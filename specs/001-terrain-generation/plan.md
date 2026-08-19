# Implementation Plan: Procedural Terrain Generation via Perlin Noise

**Branch**: `001-terrain-generation` | **Date**: 2026-08-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [spec.md](spec.md)

---

## Summary

Implement an infinite procedural terrain generation module (`Terrain`) in Unity 6 using deterministic multi-octave Perlin noise (Fractal Brownian Motion / fBm). The module is built using a Layered Clean Architecture:
1. **Pure C# Domain Engine**: Zero-dependency mathematical and data processing layer (`PerlinNoiseGenerator`, `HeightMapBuilder`, `TerrainMeshBuilder`, `MemoryChunkStorage`) enabling 100% background thread execution and rapid headless unit testing.
2. **Multi-LOD Asynchronous Streaming Engine**: Seamless, stutter-free chunk streaming around an active viewer with distance-based mesh LOD reduction, edge normal stitching, and GameObject pooling.
3. **Ergonomic ScriptableObject Presets & Unity Editor Tooling**: Standalone `TerrainDataConfig` asset with tooltips, range sliders, expandable biome lists, on-demand generation, and live auto-updating.
4. **Decoupled Downstream Integration (`ITerrainProvider`)**: High-performance spatial elevation/slope queries ($< 0.01\text{ ms}$) with bilinear interpolation and lifecycle events (`OnChunkLoaded`, `OnTerrainGenerated`) for prop, foliage, building, and bridge placers.
5. **Persistent Chunk Caching (`IChunkStorage`)**: Caching visited chunk data for rapid retrieval ($< 10\text{ ms}$) upon backtracking.

---

## Technical Context

**Language/Version**: C# 9.0 / .NET Standard 2.1 (Unity 6 / 6000.5.5f1)  
**Primary Dependencies**: Unity Engine (Universal Render Pipeline 17.5.0), Unity Test Framework 1.7.0  
**Storage**: Memory cache with optional binary/JSON serialization for chunk state persistence  
**Testing**: Unity Test Framework (NUnit EditMode unit tests and PlayMode integration tests)  
**Target Platform**: Standalone PC / Desktop / Console (Windows / Mac / Linux)  
**Project Type**: Unity 3D Game / Subsystem Module  
**Performance Goals**: Locked 60+ FPS during infinite chunk streaming; background task calculation with $< 2\text{ ms}$ main thread upload budget; $< 0.01\text{ ms}$ spatial query latency  
**Constraints**: Zero main thread frame stalls; zero GC allocations during steady-state chunk streaming (object pooling); zero Unity API dependencies in domain math  
**Scale/Scope**: Infinite chunk grid space ($240\text{ m} \times 240\text{ m}$ chunk units with multi-LOD tiers $0..3$)

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **Principle I: Architectural Integrity & SOLID Design** — **PASS**
  - SRP: Pure C# domain handles math/mesh structures; Unity layer handles GameObjects/Inspectors.
  - OCP: `INoiseGenerator` and `IChunkStorage` allow adding Simplex/Voronoi noise or SQLite storage without modifying core streaming.
  - DIP: Downstream modules depend on `ITerrainProvider`, not concrete MonoBehaviour classes.
- **Principle II: Comprehensive Testing Standards & Test-First** — **PASS**
  - Core domain logic is covered by isolated NUnit EditMode tests targeting $\ge 80\%$ branch coverage.
- **Principle III: User Experience Consistency & Accessibility** — **PASS**
  - Rich Unity Inspector tooling with tooltips, range validation sliders, auto-update toggle, and clear feedback.
- **Principle IV: Performance & Resource Efficiency** — **PASS**
  - Background task calculation, chunk object pooling (zero steady-state GC), Multi-LOD vertex reduction, and bilinear spatial lookup.
- **Principle V: Maintainability, Simplicity & Observability** — **PASS**
  - Clean C# project layout, self-documenting code, structured events.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-terrain-generation/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Phase 0 technical research & trade-offs
├── data-model.md        # Phase 1 data model & entity diagrams
├── quickstart.md        # Phase 1 validation & testing guide
├── contracts/           # Phase 1 public interface contracts
│   ├── ITerrainProvider.md
│   ├── IChunkStorage.md
│   └── INoiseGenerator.md
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
│   │   ├── IChunkStorage.cs
│   │   └── INoiseGenerator.cs
│   ├── Models/
│   │   ├── NoiseSettings.cs
│   │   ├── ChunkCoordinate.cs
│   │   ├── HeightMap.cs
│   │   ├── TerrainMeshData.cs
│   │   ├── LODInfo.cs
│   │   └── TerrainRegion.cs
│   └── Services/
│       ├── PerlinNoiseGenerator.cs
│       ├── HeightMapBuilder.cs
│       ├── TerrainMeshBuilder.cs
│       └── MemoryChunkStorage.cs
├── Presentation/
│   ├── Config/
│   │   └── TerrainDataConfig.cs
│   ├── Components/
│   │   ├── TerrainGenerator.cs
│   │   └── TerrainChunkView.cs
│   └── Pooling/
│       └── ChunkObjectPool.cs
├── Editor/
│   ├── TerrainGeneratorEditor.cs
│   └── TerrainDataConfigEditor.cs
└── Tests/
    ├── EditMode/
    │   ├── PerlinNoiseGeneratorTests.cs
    │   ├── HeightMapBuilderTests.cs
    │   ├── TerrainMeshBuilderTests.cs
    │   └── ChunkStorageTests.cs
    └── PlayMode/
        ├── InfiniteStreamingTests.cs
        └── TerrainProviderIntegrationTests.cs
```

---

## Phase Breakdown

### Phase 0: Research & Architecture (Complete)
- Evaluated pure C# deterministic Perlin noise math vs Unity `Mathf.PerlinNoise`.
- Designed $240\text{ m}$ chunk grid with divisible LOD steps ($1, 2, 4, 6$) and seamless edge stitching.
- Designed Task-based background multi-threading with main-thread staging queues and object pooling.
- Established `ITerrainProvider` and `IChunkStorage` contract abstractions.

### Phase 1: Design, Contracts & Data Model (Complete)
- Produced [data-model.md](data-model.md) defining all domain models and ScriptableObject schemas.
- Defined public interfaces in [contracts/](contracts/): `ITerrainProvider`, `IChunkStorage`, `INoiseGenerator`.
- Produced [quickstart.md](quickstart.md) for EditMode and PlayMode validation workflows.

### Phase 2: Tasks & Implementation Planning (Next Phase: `/speckit.tasks`)
- Task breakdown ordered by strict dependencies:
  1. Pure C# Domain models and tests.
  2. Perlin Noise generator and heightmap synthesis with determinism tests.
  3. Mesh builder and Multi-LOD triangulation with seamless edge stitching.
  4. Memory and file chunk storage services.
  5. ScriptableObject configuration presets and Editor inspectors.
  6. Chunk pooling and async streaming coordinator (`TerrainGenerator`).
  7. Downstream spatial queries (`ITerrainProvider`) and integration verification.
