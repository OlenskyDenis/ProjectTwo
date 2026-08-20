# Technical Research & Architecture Decisions: TerrainConfig Interface & Generation Expansion

**Feature Branch**: `002-terrain-config-ui-expansion`  
**Date**: 2026-08-20  
**Spec**: [spec.md](spec.md)

---

## 1. Noise Shaping & Topography Algorithms

### Decision: Multi-Algorithm Procedural Shaper (`ProceduralTerrainShaper`) with Pure C# Execution
- **Selected Approach**: Implement an extensible shaper service that combines:
  1. **Noise Types**: Standard Fractal Perlin (fBm), Ridged Multifractal (inverted absolute noise for sharp crests), and Billow (absolute noise for rounded hills/clouds).
  2. **Macro Continent / Mountain Mask**: Secondary low-frequency noise layer acting as a regional multiplier $[0..1]$ to scale height and octave roughness, ensuring high mountain ranges are isolated from flat plains.
  3. **Non-Linear Elevation Curves**: Polynomial / power-law redistribution and piecewise Hermite curves to flatten valleys or terraced mesas without expensive external locks.
  4. **Sea Level & River Carving**: Configurable sea level floor ($y = \text{SeaLevel}$) plus procedural river carving masks (narrow inverted ridge noise / sinusoidal channel functions) that depress terrain heights toward water level seamlessly across chunk boundaries.
  5. **Boundary Falloff**: Vector math falloff functions (Circular radial distance, Rounded square) applied cleanly at domain edges.
- **Rationale**: Keeps all noise evaluation 100% thread-safe and deterministic on background worker threads without Unity Engine main-thread API locks.
- **Alternatives Considered**:
  - *Unity Compute Shaders*: High throughput for batch textures, but requires GPU dispatch, asynchronous readbacks to CPU for physics colliders, and platform-dependent HLSL. Rejected in favor of pure C# thread-pool math with SIMD readiness.
  - *Single Monolithic Noise Function*: Too rigid for diverse biomes and causes high mountains to distort flat lowlands.

---

## 2. Editor Ergonomics, Live Preview & Cancellation Pipeline

### Decision: `CancellationTokenSource` Debounced Preview with Draft-Mode LOD Switching
- **Selected Approach**:
  1. **Custom Inspector Layout**: Built with clean, collapsible category foldouts (`Chunk Grid`, `Macro & Continents`, `Noise & Elevation`, `Rivers & Water`, `Biome Textures`, `LOD & Streaming`, `Presets & Utilities`) using custom IMGUI styling.
  2. **Cooperative Cancellation**: When an inspector property changes, any in-flight background generation task's `CancellationTokenSource` is cancelled immediately (`cts.Cancel()`), preventing obsolete task accumulation.
  3. **Debounced Scheduling**: A timer delay ($120\text{ ms}$) buffers rapid slider events.
  4. **Draft-Mode Preview**: While continuous slider dragging is active, the preview generates a low-resolution mesh (LOD step 4/6); upon slider release or settlement ($> 150\text{ ms}$ idle), a full LOD-0 mesh is generated.
  5. **Resource Lifecycle Safety**: In Edit Mode, any replaced preview meshes are immediately destroyed via `UnityEngine.Object.DestroyImmediate` to eliminate memory leaks and orphaned mesh assets.
- **Rationale**: Guarantees $\ge 60\text{ FPS}$ in Editor Scene View without UI hitching or background thread exhaustion.
- **Alternatives Considered**:
  - *Immediate Synchronous Generation on GUI Change*: Freezes the editor GUI on high-resolution chunks ($240 \times 240$ vertices).
  - *Detached 2D Mini-Map Window*: Adds visual clutter; user explicitly requested direct 3D Scene View generation.

---

## 3. Hybrid Biome Texturing & Shader Architecture

### Decision: Multi-Layer Material Property Pipeline with Height & Slope Blending
- **Selected Approach**:
  1. **Hybrid Biome Layer Data**: Each `BiomeLayer` ScriptableObject / struct stores `HeightThreshold`, `SlopeThreshold` (for steep cliffs/rocks), `AlbedoTexture`, `NormalMap`, `Tiling`, `BlendSoftness`, `ColorTint`, and an optional `CustomMaterial` override.
  2. **Texture Array / Shader Uniforms**: In default mode, biome textures and color/slope parameters are passed to a Universal Render Pipeline (URP) Triplanar/Slope Blending Shader via `MaterialPropertyBlock` or Texture Arrays.
  3. **Sub-Material Support**: If a biome specifies a custom material override (e.g., animated water, ice, lava), chunks intersecting that primary biome can assign that material directly.
- **Rationale**: Provides game designers with complete creative freedom—from simple color tints to realistic PBR textures with triplanar projection on sheer cliffs.
- **Alternatives Considered**:
  - *Fixed 4-Splatmap Texturing*: Limits worlds to only 4 textures globally.
  - *Per-Vertex Color Only*: Too low fidelity for modern gameplay visuals.

---

## 4. Preset Management Architecture

### Decision: ScriptableObject Archetype Presets & Inspector Serialization
- **Selected Approach**:
  - Built-in presets (`AlpineMountains`, `RollingPlains`, `Archipelago`, `DesertCanyons`) stored in `Assets/Presets/Terrain/`.
  - Inspector contains a preset selector dropdown, a "Load Preset" button, a "Save Current as Preset" exporter, and a "Randomize Seed" generator.
- **Rationale**: Allows rapid level design iterations and non-destructive prototyping.
