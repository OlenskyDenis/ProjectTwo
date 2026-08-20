# Tasks: Macro-Tectonic Zoning & Hydrological River Graph System

**Branch**: `003-tectonic-zones-river-network`
**Spec**: [specs/003-tectonic-zones-river-network/spec.md](file:///e:/ProjectTwo/specs/003-tectonic-zones-river-network/spec.md)
**Plan**: [specs/003-tectonic-zones-river-network/plan.md](file:///e:/ProjectTwo/specs/003-tectonic-zones-river-network/plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Assembly definition setup and foundational enumeration models.

- [x] T001 Verify and configure `Unity.Collections`, `Unity.Mathematics`, and `Unity.Burst` package references in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/ProjectTwo.Terrain.Core.asmdef`
- [x] T002 [P] Create Tectonic and Hydrology enumeration types (`TectonicBoundaryType`, `PlateCrustType`, `RiverNodeType`) in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TectonicEnums.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/HydrologyEnums.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data models, configuration assets, and contracts required by all user stories.

**⚠️ CRITICAL**: Must complete before implementing User Stories.

- [x] T003 [P] Create `TectonicPlate` and `TectonicBoundary` pure data structs in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TectonicPlate.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TectonicBoundary.cs`
- [x] T004 [P] Create `TectonicSettings` serializable struct with validation logic in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TectonicSettings.cs`
- [x] T005 [P] Create `RiverNode`, `RiverSegment`, and `LakeBasin` pure data structs in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverNode.cs`, `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverSegment.cs`, and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/LakeBasin.cs`
- [x] T006 [P] Create `HydrologySettings` serializable struct with validation logic in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/HydrologySettings.cs`
- [x] T007 [P] Create `RiverGraph` data container with NativeArray storage and Spatial Hash Grid in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverGraph.cs`
- [x] T008 [P] Define `ITectonicService` contract interface in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Contracts/ITectonicService.cs`
- [x] T009 [P] Define `IHydrologyService` and `IRiverMeshBuilder` contract interfaces in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Contracts/IHydrologyService.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Contracts/IRiverMeshBuilder.cs`
- [x] T010 Update `ITerrainShaper` and `TerrainDataConfig` to incorporate `TectonicSettings` and `HydrologySettings` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Contracts/ITerrainShaper.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TerrainDataConfig.cs`

**Checkpoint**: Core contracts and data structures ready. User stories can proceed.

---

## Phase 3: User Story 1 - Continuous Mountain Ridges via Tectonic Boundary Zoning (Priority: P1) 🎯 MVP

**Goal**: Generate continuous, unbroken mountain chains along convergent plate boundaries using jittered Voronoi plate cells and drift vector kinematics.

**Independent Test**: Configure tectonic plate count and uplift multiplier in `TerrainConfig`, sample elevations along plate collision zones, and verify continuous ridge elevation ($>80\%$ peak height across 4+ adjacent chunks).

### Tests for User Story 1

- [x] T011 [P] [US1] Create EditMode unit tests for Voronoi cell partitioning, boundary classification, and uplift distance fields in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/TectonicServiceTests.cs`

### Implementation for User Story 1

- [x] T012 [US1] Implement `TectonicService` with deterministic jittered Voronoi cell centroids and drift velocity generation in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/TectonicService.cs`
- [x] T013 [US1] Implement boundary edge extraction and kinematic classification (Convergent, Divergent, Transform) in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/TectonicService.cs`
- [x] T014 [US1] Implement analytical boundary distance-field calculation, exponential ridge profile shaping, and noise warping in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/TectonicService.cs`
- [x] T015 [US1] Integrate tectonic uplift calculation into `ProceduralTerrainShaper.CalculateElevation` and `GenerateHeightMap` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/ProceduralTerrainShaper.cs`
- [x] T016 [US1] Create integration test verifying continuous mountain chains across adjacent chunk borders in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/Integration/TectonicRidgeContinuityTests.cs`

**Checkpoint**: User Story 1 MVP fully operational: procedural terrain generates natural continental plates and unbroken mountain ranges.

---

## Phase 4: User Story 2 - Vector River Network & Gradient-Based Pathfinding (Priority: P2)

**Goal**: Trace connected river paths from mountain catchments down to sea level, resolving sinks via Priority-Flood saddle breaching and lake basins.

**Independent Test**: Generate river graph on a test heightmap and verify 100% of paths reach ocean level with strictly non-increasing water surface elevations and valid Strahler stream orders.

### Tests for User Story 2

- [x] T017 [P] [US2] Create EditMode unit tests for river source placement, steepest descent routing, and depression resolving in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/HydrologyServiceTests.cs`

### Implementation for User Story 2

- [x] T018 [US2] Implement high-elevation mountain source candidate selection in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [x] T019 [US2] Implement gradient-based steepest descent path tracing with harmonic meandering deflection in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [x] T020 [US2] Implement Priority-Flood saddle breach carving and `LakeBasin` spillover generation for depression resolution in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [x] T021 [US2] Implement tributary confluence merging and Strahler stream ordering propagation in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`

**Checkpoint**: User Story 2 operational: full topological river network generated across the world without dead-ends.

---

## Phase 5: User Story 3 - Continuous Hydraulic Carving & Riverbed Shaping (Priority: P3)

**Goal**: Carve V-shaped and alluvial river channels into chunk heightmaps and build procedural river ribbon water meshes with flow UV coordinates.

**Independent Test**: Generate chunks intersecting river splines, verify seamless heightmap carving across chunk borders, and confirm valid procedural water mesh geometry with flow UVs.

### Tests for User Story 3

- [x] T022 [P] [US3] Create EditMode unit tests for chunk river carving and water ribbon mesh generation in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/RiverMeshBuilderTests.cs`

### Implementation for User Story 3

- [x] T023 [US3] Implement Spatial Hash Grid registration and $O(1)$ query evaluation for river segments in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverGraph.cs`
- [x] T024 [US3] Implement `SampleRiverCarve` in `HydrologyService.cs` with variable channel width, depth, and bank cross-sections, and connect to `ProceduralTerrainShaper.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/ProceduralTerrainShaper.cs`
- [x] T025 [P] [US3] Create `RiverWaterMeshData` container in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverWaterMeshData.cs`
- [x] T026 [US3] Implement `RiverMeshBuilder` service generating sloped water surface ribbons with flow UVs in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs`
- [x] T027 [US3] Update `TerrainChunkView` to instantiate and render procedural river water ribbon meshes in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainChunkView.cs`
- [x] T028 [US3] Create integration test verifying 0 vertex seams and continuous riverbed depth across chunk boundaries in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/Integration/RiverChunkSeamTests.cs`

**Checkpoint**: User Story 3 operational: 3D carved river channels and sloped water ribbons seamlessly rendered across terrain chunks.

---

## Phase 6: User Story 4 - Tectonic & Hydrology Inspector Configuration and Visual Debugging (Priority: P4)

**Goal**: Provide intuitive Inspector controls in `TerrainConfig` and Scene View Gizmo overlays for plate boundaries, drift vectors, and river network splines.

**Independent Test**: Open `TerrainConfig` in Unity Editor, toggle Tectonic and River gizmo modes, adjust sliders, and verify live preview updates in Scene View.

### Implementation for User Story 4

- [x] T029 [P] [US4] Implement `TectonicDebugGizmo` for Scene View visualization of plate polygons and drift vectors in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/Debug/TectonicDebugGizmo.cs`
- [x] T030 [P] [US4] Implement `RiverGraphDebugGizmo` for Scene View visualization of river nodes, splines, and lake basins in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/Debug/RiverGraphDebugGizmo.cs`
- [x] T031 [US4] Extend `TerrainConfigEditor` with dedicated Tectonics and Hydrology foldouts, preset options, and sanity validation in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainConfigEditor.cs`

**Checkpoint**: User Story 4 operational: designers can configure, inspect, and visually debug tectonic plates and river graphs directly in Unity Editor.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Performance optimization, Burst compilation passes, and full end-to-end validation.

- [x] T032 [P] Optimize math routines with `Unity.Burst` and allocation-free structs in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/`
- [x] T033 Execute all automated EditMode and Integration test suites
- [x] T034 Run interactive editor validation per `specs/003-tectonic-zones-river-network/quickstart.md`

---

## Dependencies & Execution Order

```text
Phase 1 (Setup)
   │
   ▼
Phase 2 (Foundational Models & Contracts)
   │
   ├────────────────────────┐
   ▼                        ▼
Phase 3 (US1 - Tectonics) Phase 4 (US2 - River Routing)
   │                        │
   └───────────┬────────────┘
               ▼
Phase 5 (US3 - Carving & Water Meshes)
               │
               ▼
Phase 6 (US4 - Inspector UI & Gizmos)
               │
               ▼
Phase 7 (Polish & Verification)
```
