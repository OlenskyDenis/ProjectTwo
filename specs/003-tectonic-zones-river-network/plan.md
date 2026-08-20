# Implementation Plan: Macro-Tectonic Zoning & Hydrological River Graph System

**Branch**: `003-tectonic-zones-river-network` | **Date**: 2026-08-20 | **Spec**: [specs/003-tectonic-zones-river-network/spec.md](file:///e:/ProjectTwo/specs/003-tectonic-zones-river-network/spec.md)

**Input**: Feature specification from `/specs/003-tectonic-zones-river-network/spec.md`

## Summary

Implement a high-performance macro-tectonic zoning and hydrological river graph pipeline for procedural world generation. The system generates global crustal plates using jittered Voronoi/Delaunay partitioning with drift vectors to form continuous mountain chains along convergent boundaries, and integrates an acyclic vector river graph with priority-flood depression resolving, Strahler stream ordering, hydraulic heightmap carving, and procedural river ribbon water meshes. All core sampling algorithms are designed for zero-allocation C# Job System / Burst execution to guarantee 60+ FPS without chunk streaming lag.

## Technical Context

**Language/Version**: C# 9.0+ / Unity 2022.3 LTS (.NET Standard 2.1)

**Primary Dependencies**: Unity Engine, `Unity.Collections` (NativeArray, NativeParallelMultiHashMap), `Unity.Mathematics`, `Unity.Burst`

**Storage**: In-memory `RiverGraph` with Spatial Hash Grid index + ScriptableObject `TerrainDataConfig` / `TectonicSettings` / `HydrologySettings` assets

**Testing**: Unity Test Framework (NUnit EditMode & PlayMode unit and integration tests)

**Target Platform**: Windows / macOS / Linux / Consoles (Standalone runtime and Unity Editor)

**Project Type**: Procedural World Generation Subsystem & Game Architecture

**Performance Goals**: 
- Tectonic & River Graph macro generation for 2km x 2km $< 500$ms.
- Per-chunk heightmap & river carving sampling $< 0.5$ms.
- Zero GC allocations during chunk streaming ($0$ B/frame).
- Main-thread frame budget: 60+ FPS during continuous streaming and live preview dragging.

**Constraints**:
- Seam-free chunk alignment (0 vertex cracks or elevation gaps across borders).
- Pure mathematical determinism from seed.
- Clean Architecture separation (Core Models/Contracts/Services vs. Presentation/Components vs. Editor).

**Scale/Scope**:
- Supports worlds with up to 64 macro tectonic plates and hundreds of interconnected river tributaries.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Principle I: Architectural Integrity & SOLID Design**: Services (`TectonicService`, `HydrologyService`, `RiverMeshBuilder`) implement fine-grained contracts (`ITectonicService`, `IHydrologyService`, `IRiverMeshBuilder`). High-level terrain shapers depend on abstractions via Dependency Injection.
- [x] **Principle II: Comprehensive Testing Standards & Test-First**: Unit tests covering Voronoi cell continuity, convergent mountain uplift, river steepest descent routing, and seam-free chunk borders. Minimum 80% coverage on core domain services.
- [x] **Principle III: User Experience Consistency & Accessibility**: Ergonomic foldouts in `TerrainConfig` with live parameter validation, tooltips, and Scene View Gizmo debug overlays.
- [x] **Principle IV: Performance & Resource Efficiency**: Pure structs and NativeArray / Spatial Hash Grid structures optimized for Unity Job System / Burst with zero GC allocs in sampling loops.
- [x] **Principle V: Maintainability, Simplicity & Observability**: Clear data contracts, self-documenting naming, and structured debug logging.

## Project Structure

### Documentation (this feature)

```text
specs/003-tectonic-zones-river-network/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── ITectonicService.md
│   └── IHydrologyService.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - to be created)
```

### Source Code Layout

```text
ProjectTwoUnity/Assets/Scripts/Terrain/
├── Core/
│   ├── Contracts/
│   │   ├── ITectonicService.cs
│   │   ├── IHydrologyService.cs
│   │   ├── IRiverMeshBuilder.cs
│   │   └── ITerrainShaper.cs (updated with Tectonic & River parameters)
│   ├── Models/
│   │   ├── TectonicSettings.cs
│   │   ├── TectonicPlate.cs
│   │   ├── TectonicBoundary.cs
│   │   ├── HydrologySettings.cs
│   │   ├── RiverNode.cs
│   │   ├── RiverSegment.cs
│   │   ├── RiverGraph.cs
│   │   ├── LakeBasin.cs
│   │   └── RiverWaterMeshData.cs
│   └── Services/
│       ├── TectonicService.cs
│       ├── HydrologyService.cs
│       ├── RiverMeshBuilder.cs
│       └── ProceduralTerrainShaper.cs (updated)
├── Presentation/
│   └── Components/
│       ├── TerrainChunkView.cs (updated to bind river meshes)
│       └── RiverChunkView.cs
├── Editor/
│   ├── TerrainConfigEditor.cs (updated with Tectonic & Hydrology tabs)
│   └── Debug/
│       ├── TectonicDebugGizmo.cs
│       └── RiverGraphDebugGizmo.cs
└── Tests/
    ├── EditMode/
    │   ├── TectonicServiceTests.cs
    │   ├── HydrologyServiceTests.cs
    │   └── RiverMeshBuilderTests.cs
    └── Integration/
        └── TectonicHydrologyChunkIntegrationTests.cs
```

**Structure Decision**: Clean Architecture with modular asmdef separation (`ProjectTwo.Terrain.Core`, `ProjectTwo.Terrain.Presentation`, `ProjectTwo.Terrain.Editor`, `ProjectTwo.Terrain.Tests`).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *None* | Architecture strictly adheres to existing repository conventions and constitution | N/A |
