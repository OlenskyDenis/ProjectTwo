# Implementation Plan: TerrainConfig Interface & Generation Capabilities Expansion

**Branch**: `002-terrain-config-ui-expansion` | **Date**: 2026-08-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [spec.md](spec.md)

---

## Summary

Expand the procedural terrain generation suite and Editor UX for `TerrainDataConfig` in Unity:
1. **Multi-Type Noise & Compound Terrain Shaper (`ProceduralTerrainShaper`)**: Support Ridged Multifractal, Billow, Macro Continent/Mountain masks, Non-linear elevation curves, Procedural River carving masks, and Global Sea Level depth floors.
2. **Ergonomic, Categorized Inspector UI (`TerrainDataConfigEditor`)**: Collapsible foldouts, seamless multiple-of-12 validation badges, interactive biome gradient previews, and instant utility buttons (Seed Randomizer, Snap).
3. **Direct 3D Scene View Live Preview with Draft-Mode & Cancellation**: Progressive low-res preview during active slider manipulation, debounced scheduling ($120\text{ ms}$), instant `CancellationTokenSource` task cancellation, and explicit `DestroyImmediate` resource cleanup to guarantee 0 memory leaks and $\ge 60\text{ FPS}$ UI responsiveness.
4. **Hybrid Biome Texturing & Slope Blending**: Layered biome definitions supporting Albedo/Normal textures, UV tiling, slope angle thresholds (for cliff/rock blending), and custom Unity Material overrides.
5. **Archetype Presets Library**: Built-in environment presets (`AlpineMountains`, `RollingPlains`, `Archipelago`, `DesertCanyons`) with 1-click loading, exporting, and runtime serialization.

---

## Technical Context

**Language/Version**: C# 9.0 / .NET Standard 2.1 (Unity 6 / 6000.5.5f1)  
**Primary Dependencies**: Unity Engine (Universal Render Pipeline 17.5.0), Unity Test Framework 1.7.0  
**Storage**: ScriptableObject configuration assets and preset files (`.asset`)  
**Testing**: Unity Test Framework (NUnit EditMode unit tests in `ProjectTwo.Terrain.Tests`)  
**Target Platform**: Standalone PC / Desktop (Windows / Mac / Linux)  
**Project Type**: Unity 3D Game / Subsystem Module  
**Performance Goals**: Live Editor preview updates within $< 150\text{ ms}$; steady $\ge 60\text{ FPS}$ during continuous slider manipulation; zero memory leaks or orphaned meshes during long edit sessions  
**Constraints**: Pure C# thread-safe mathematical core; strict divisibility by 12 on chunk sizes/resolutions for zero mesh cracking; zero heap allocations in inner elevation evaluation loops  
**Scale/Scope**: Infinite chunk grid space with full LOD streaming compatibility

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **Principle I: Architectural Integrity & SOLID Design** — **PASS**
  - SRP: `ProceduralTerrainShaper` calculates elevation math; `TerrainDataConfigEditor` renders GUI; `TerrainGenerator` manages chunk views.
  - OCP: `ITerrainShaper` abstraction allows plugging in erosion algorithms or custom noise without modifying streaming pipelines.
  - DIP: Core generators depend on `ITerrainShaper` and `INoiseGenerator` abstractions.
- **Principle II: Comprehensive Testing Standards & Test-First** — **PASS**
  - Pure C# mathematical services tested via NUnit EditMode tests targeting $\ge 80\%$ branch coverage.
- **Principle III: User Experience Consistency & Accessibility** — **PASS**
  - Clean, organized inspector with instant visual feedback, tooltips, validation indicators, and preset archetypes.
- **Principle IV: Performance & Resource Efficiency** — **PASS**
  - `CancellationTokenSource` drops stale generation tasks; debouncing prevents UI hitching; progressive draft preview eliminates main-thread stalls; explicit mesh destruction prevents memory leaks.
- **Principle V: Maintainability, Simplicity & Observability** — **PASS**
  - Clear data structures, self-documenting code, zero unnecessary external dependencies.

---

## Project Structure

### Documentation (this feature)

```text
specs/002-terrain-config-ui-expansion/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Phase 0 technical research & trade-offs
├── data-model.md        # Phase 1 data model & entity diagrams
├── quickstart.md        # Phase 1 validation & testing guide
├── contracts/           # Phase 1 public interface contracts
│   ├── ITerrainShaper.md
│   └── ITerrainPresetService.md
├── checklists/
│   └── requirements.md  # Quality validation checklist
└── tasks.md             # Phase 2 task decomposition (/speckit-tasks output)
```

### Source Code (Unity project structure)

```text
ProjectTwoUnity/Assets/Scripts/Terrain/
├── Core/
│   ├── Contracts/
│   │   ├── ITerrainProvider.cs
│   │   ├── ITerrainShaper.cs        # [NEW] Shaper contract
│   │   ├── INoiseGenerator.cs
│   │   └── IChunkStorage.cs
│   ├── Models/
│   │   ├── NoiseSettings.cs         # [MODIFY] Added NoiseType (Perlin, Ridged, Billow)
│   │   ├── MacroMaskSettings.cs     # [NEW] Macro continent mask model
│   │   ├── WaterSettings.cs         # [NEW] Sea level & water basins model
│   │   ├── RiverSettings.cs         # [NEW] Procedural river carve model
│   │   ├── FalloffSettings.cs       # [NEW] Island / boundary falloff model
│   │   ├── HeightCurveSettings.cs   # [NEW] Elevation remap curve model
│   │   ├── BiomeLayer.cs            # [MODIFY] Added textures, tiling, slope threshold
│   │   └── TerrainPreset.cs         # [NEW] Preset asset model
│   └── Services/
│       ├── ProceduralTerrainShaper.cs # [NEW] Core multi-algorithm elevation service
│       ├── HeightMapBuilder.cs      # [MODIFY] Uses ITerrainShaper
│       └── PerlinNoiseGenerator.cs
├── Editor/
│   ├── TerrainDataConfigEditor.cs   # [MODIFY] Enhanced categorized foldouts, presets, snapping
│   └── TerrainGeneratorEditor.cs    # [MODIFY] Live preview draft-mode & debouncing
├── Presentation/
│   ├── Config/
│   │   └── TerrainDataConfig.cs     # [MODIFY] Integrated macro, water, river, curve, biomes
│   └── Components/
│       └── TerrainGenerator.cs      # [MODIFY] CancellationToken task cancellation & draft mode
└── Tests/
    └── EditMode/
        ├── ProceduralTerrainShaperTests.cs # [NEW] Unit tests for noise types, rivers, macro masks
        └── TerrainConfigValidationTests.cs # [NEW] Unit tests for config snapping & presets
```

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| None | All additions follow standard SOLID architecture and existing C# assembly layouts | N/A |
