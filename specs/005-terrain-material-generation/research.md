# Research & Technical Decisions: Terrain Material Generation Module

**Feature**: `005-terrain-material-generation`
**Date**: 2026-08-21
**Status**: Completed

## 1. Material Rendering Architecture & Pipeline Selection

### Decision
Implement a modular **Material Generation & Provider Service** in the Presentation layer (`ProjectTwo.Terrain.Runtime.Materials`) with a hybrid rendering strategy:
1. **Primary Baseline**: High-performance Vertex Color & Biome Gradient shading with support for `TerrainVertexColor.shader` and custom URP terrain shaders.
2. **Extensible Texture Layer**: Support for procedural texture array / triplanar blending parameters mapped through visual profiles.
3. **Water / River Shading**: Dedicated water material generation supporting flow speed, shallow/deep tint gradients, foam thresholds, and transparency via `WaterSimple.shader`.

### Rationale
- Decouples material generation and shader property binding from chunk mesh construction and pooling.
- Retains compatibility with current vertex-colored terrain meshes while paving the way for multi-texture biomes.
- Conforms to SOLID (Single Responsibility & Open/Closed Principle).

### Alternatives Considered
- *Single Monolithic Terrain Shader with hardcoded properties*: Rejected because it prevents artistic customization across varied biomes (Alpine, Desert, Volcanic) and couples presentation to shader specifics.
- *Per-Chunk Material Instances (`new Material() per chunk`)*: Rejected because allocating materials per chunk causes massive draw call batching degradation, GPU state changes, and memory leaks.

---

## 2. Visual Profile Configuration Architecture

### Decision
Author and store visual settings as modular Unity `ScriptableObject` assets:
- `TerrainVisualProfileSO`: Holds base terrain shader references, color palettes, elevation band thresholds, texture arrays, and surface parameters.
- `WaterVisualProfileSO`: Holds water/river shader references, surface colors (deep/shallow), flow velocities, normal map scale, and transparency.
- Reference these profiles inside `TerrainDataConfig` while allowing runtime/editor overrides.

### Rationale
- Technical artists can build a project library of interchangeable visual styles (e.g. Alpine, Desert, Autumn, Arctic) and swap them instantly.
- ScriptableObjects integrate seamlessly with Unity serialization, version control, and custom inspector workflows.

### Alternatives Considered
- *JSON/YAML Text Serialization*: Rejected due to lack of native Unity object reference serialization (e.g. `Shader`, `Texture2D`) and poor inspector UX for artists.
- *Embedding all settings directly into TerrainDataConfig*: Rejected because presets cannot be shared or swapped across different terrain configs.

---

## 3. Material Caching, Lifecycle & Memory Management

### Decision
Implement `TerrainMaterialCache` implementing `IMaterialCache` and `IDisposable`:
- Computes deterministic hash keys based on profile ID, shader reference, and configuration variant.
- Serves shared `sharedMaterial` instances to chunk renderers (`TerrainChunkView` and `RiverWaterMeshView`).
- Manages explicit disposal and cleanup during terrain generator destruction, scene unloads, or profile replacement in editor mode to prevent native memory leaks (`DestroyImmediate` in editor, `Destroy` in play mode).

### Rationale
- With 100+ active streaming chunks, only 1-2 shared material instances exist in memory.
- Completely eliminates memory leaks caused by untracked `new Material()` instances.

### Alternatives Considered
- *Static material fields in TerrainChunkView*: Rejected because static instances cannot react to multiple active terrain instances with different profiles, cannot be cleanly disposed on domain reload, and violate test isolation.

---

## 4. Real-Time Reactive Updates in Inspector

### Decision
- Profile ScriptableObjects implement an event/callback `OnProfileChanged` triggered via `OnValidate()` in the Unity Editor or runtime property setters.
- The `TerrainMaterialService` subscribes to these events and immediately updates properties on active cached shared materials (e.g., `SetColor`, `SetFloat`, `SetTexture`).
- Because chunks share these material instances, all active terrain chunks reflect visual changes immediately without recalculating mesh geometry or pool objects.

### Rationale
- Delivers instantaneous visual feedback for artists and level designers.
- Zero CPU mesh recalculation cost during color/gradient tweaking.

---

## 5. Domain Clean Architecture & Decoupling

### Decision
- Remove direct `UnityEngine.Material` fields from `TerrainRegion` and `WaterSettings` in `ProjectTwo.Terrain.Core`.
- Replace them with lightweight string/identifier descriptors (e.g. `MaterialKey` / `VisualProfileId`).
- `ProjectTwo.Terrain.Core` maintains 100% engine-rendering independence, allowing pure C# domain computations in background jobs, headless server tests, and CLI tooling without graphics dependencies.

### Rationale
- Strictly aligns with Constitution Principle I (SOLID & Clean Architecture) and Principle VI (Single Source of Truth).
