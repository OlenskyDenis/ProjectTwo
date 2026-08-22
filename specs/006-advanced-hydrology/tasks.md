# Tasks: Advanced Hydrology, Waterfall Dynamics & Continuous River Networks

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Data models and configuration parameters for advanced hydrology

- [X] T001 [P] Update domain models `RiverNodeType`, `RiverNode`, `RiverSegment`, and `LakeBasin` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/RiverGraph.cs`
- [X] T002 [P] Add waterfall, momentum, lake cascade, and delta parameters to `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/HydrologySettings.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interfaces and test harnesses for hydrology and river mesh builder

- [X] T003 [P] Declare `IHydrologyService` and `IRiverMeshBuilder` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/`
- [X] T004 [P] Create unit test fixture `AdvancedHydrologyTests.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/AdvancedHydrologyTests.cs`

---

## Phase 3: User Story 1 - Continuous Mountain Headwaters & Waterfall Clamping (Priority: P1) 🎯 MVP

**Goal**: Mountain headwaters flow continuously as cliff-conforming waterfalls without floating in mid-air or breaking into orphan segments

**Independent Test**: Generate alpine terrain and verify that all mountain streams cling tightly to cliffs (<=0.25m distance) and have zero orphan/floating single-quad segments

- [X] T005 [US1] Implement 3D surface-aligned binormal frame calculation and terrain surface vertex clamping in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs`
- [X] T006 [US1] Implement adaptive waterfall stepping (1–2m vertical steps on slopes >25°) in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [X] T007 [P] [US1] Implement dynamic headwater width scaling (starting at 1.5m and widening with flow accumulation) in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs`
- [X] T008 [US1] Implement river network validation and dead-end pruning to eliminate all disconnected orphan segments in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`

---

## Phase 4: User Story 2 - Lake Cascades, Basin Spillover & Long-Range Continuity (Priority: P1)

**Goal**: Water accumulates in mountain basins, spills over lowest saddle rims into cascading lake chains, and flows long distances via hydraulic momentum

**Independent Test**: Generate multi-basin highland terrain and verify that enclosed depressions form lakes with overflow channels draining sequentially into lower lakes

- [X] T009 [US2] Implement analytical saddle point extraction for basin depression filling and spillover outflow channels in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [X] T010 [US2] Implement multi-tier lake cascade linkage connecting highland lake basins through spillway rapids into shared drainage trunks in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [X] T011 [P] [US2] Implement hydraulic momentum and directional look-ahead velocity blending in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`

---

## Phase 5: User Story 3 - Tributary Confluence, River Bifurcation & Deltas (Priority: P2)

**Goal**: Tributaries merge to widen rivers according to Strahler orders, and lowland streams bifurcate into braided channels and coastal deltas

**Independent Test**: Verify channel width increases proportionally at merge junctions and splits into multiple branches on flat lowlands (slope < 5°) before reaching sea level

- [X] T012 [US3] Implement tributary confluence resolution with Strahler stream order calculation and channel widening in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [X] T013 [P] [US3] Implement lowland river bifurcation and coastal delta branching in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs`
- [X] T014 [US3] Add inspector slider controls for waterfall step size, lake spillover, momentum, and delta chance in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs`

---

## Phase 6: Polish & Verification

**Purpose**: Cross-cutting verification, integration tests, and performance validation

- [X] T015 [P] Create integration test suite `WaterfallAndLakeCascadeIntegrationTests.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/Integration/WaterfallAndLakeCascadeIntegrationTests.cs`
- [X] T016 Execute quickstart validation scenarios and document results in `walkthrough.md`

---

## Phase 7: Convergence

**Purpose**: Remediate mid-air floating river quads and ensure end-to-end parameter forwarding

- [X] T017 [CRITICAL] [US1] Forward `_terrainShaper`, `NoiseSettings`, and `TectonicSettings` from `TerrainGenerator.cs` to `RiverMeshBuilder.BuildChunkRiverMesh` per Constitution Principle VI (contradicts)
- [X] T018 [CRITICAL] [US1] Sample authoritative terrain elevation $h(x, z)$ at every spline subdivision step in `RiverMeshBuilder.cs` to clamp vertex elevation strictly within 0.15m of ground surface per FR-002, SC-002 (partial)
- [X] T019 [HIGH] [US1] Calculate adaptive quadratic Bezier control point height conforming to terrain saddle contours in `HydrologyService.cs` per FR-001, FR-002 (partial)
