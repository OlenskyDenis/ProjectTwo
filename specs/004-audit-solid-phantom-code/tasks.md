# Tasks: Codebase SOLID Audit and Phantom Code Elimination

**Branch**: `004-audit-solid-phantom-code` | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify baseline compilation and initialize test harness

- [X] T001 Verify baseline project compilation via `dotnet build ProjectTwoUnity/ProjectTwoUnity.slnx`
- [X] T002 Verify baseline unit and integration test suite execution via `dotnet test ProjectTwoUnity/ProjectTwo.Terrain.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish contract reflection test infrastructure to guard against architectural drift

- [X] T003 [P] Create `ContractReflectionTests.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/ContractReflectionTests.cs` to assert zero stale overloads on `ITerrainShaper` and single pipeline governance

**Checkpoint**: Foundation ready — contract tests established and ready to detect pipeline deviations.

---

## Phase 3: User Story 1 - Comprehensive SOLID Principles Compliance Audit (Priority: P1) 🎯 MVP

**Goal**: Audit domain contracts and services for adherence to SOLID and Constitution Principle VI

**Independent Test**: `ContractReflectionTests` executes and flags non-compliant stale overloads in `ProjectTwo.Terrain.Core.Contracts`

- [X] T004 [US1] Run `ContractReflectionTests` to catalog all stale overloads and architectural violations in `ITerrainShaper` and `HeightMapBuilder`
- [X] T005 [P] [US1] Audit dependency inversion and interface segregation in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HeightMapBuilder.cs`

**Checkpoint**: User Story 1 complete — full audit and automated CI reflection tests operational.

---

## Phase 4: User Story 2 - Detection & Inventory of Phantom Code (Priority: P2)

**Goal**: Detect and map all dead methods, unused overloads, and orphaned calculation pathways

**Independent Test**: All legacy call sites calling 8-parameter or 11-parameter overloads are inventoried across tests and runtime

- [X] T006 [P] [US2] Map all stale callers in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/ProceduralTerrainShaperTests.cs`
- [X] T007 [P] [US2] Map all stale callers in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/HeightMapBuilderTests.cs`
- [X] T008 [US2] Verify end-to-end parameter routing from `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Config/TerrainDataConfig.cs` down to `TerrainGenerator.cs`

**Checkpoint**: User Story 2 complete — full inventory of phantom elements and legacy call sites compiled.

---

## Phase 5: User Story 3 - Full-Cycle Remediation & Safe Cleanup (Priority: P3)

**Goal**: Eliminate confirmed phantom code, refactor for pure SOLID compliance, and verify 0% regressions

**Independent Test**: `dotnet test ProjectTwoUnity/ProjectTwo.Terrain.Tests.csproj` passes 100% with ContractReflectionTests green and 0 compiler warnings

- [X] T009 [P] [US3] Remove prohibited stale overloads from `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Contracts/ITerrainShaper.cs`
- [X] T010 [US3] Refactor `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/ProceduralTerrainShaper.cs` to eliminate legacy overloads and maintain single authoritative pipeline
- [X] T011 [US3] Refactor `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HeightMapBuilder.cs` for pure dependency injection (remove hardcoded `new` and remove `_noiseGenerator` bypass)
- [X] T012 [US3] Update test cases in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/ProceduralTerrainShaperTests.cs` to use authoritative 12-parameter pipeline
- [X] T013 [US3] Update test cases in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/HeightMapBuilderTests.cs` to test authoritative compound pipeline
- [X] T014 [US3] Ensure `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainGenerator.cs` initializes `HeightMapBuilder` with explicit dependency injection

**Checkpoint**: User Story 3 complete — all phantom code eliminated, SOLID principles satisfied, contracts frozen.

---

## Phase 6: Polish & Cross-Cutting Verification

**Purpose**: Final quality gate and regression validation

- [X] T015 Run full test suite via `dotnet test ProjectTwoUnity/ProjectTwo.Terrain.Tests.csproj` and ensure 100% pass rate
- [X] T016 Build full solution via `dotnet build ProjectTwoUnity/ProjectTwoUnity.slnx` and ensure 0 warnings and 0 errors
- [X] T017 Validate all scenarios in `specs/004-audit-solid-phantom-code/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — establishes test fixture.
- **User Stories (Phases 3-5)**: Sequentially executed: US1 (Audit & Guard) → US2 (Inventory) → US3 (Remediation & Refactoring).
- **Polish (Phase 6)**: Final validation after US3 completion.

### Parallel Opportunities
- T003, T005, T006, T007, T009 can be analyzed in parallel.
- Test updates T012 and T013 can proceed in parallel once contracts are updated.

---

## Implementation Strategy

### MVP First (User Story 1 & 2)
1. Complete Setup and Contract Reflection Tests.
2. Complete audit and catalog of violations.

### Full-Cycle Delivery (User Story 3)
1. Refactor `ITerrainShaper`, `ProceduralTerrainShaper`, `HeightMapBuilder`.
2. Update tests and verify 0% regressions with green contract tests.

---

## Phase 7: Convergence

**Purpose**: Address gaps identified during convergence assessment against Constitution Principle IV and Editor Lifecycle governance

- [X] T018 [P] Configure `HideFlags.DontSave` and `OnDisable` / cleanup logic in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Pooling/ChunkObjectPool.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainGenerator.cs` to prevent orphaned scene chunk accumulation in EditMode per Constitution IV (partial)
- [X] T019 Add unit test in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/DomainModelTests.cs` verifying chunk object pool assigns `HideFlags.DontSave` to prevent scene serialization pollution per Constitution IV (missing)

---

## Phase 8: Convergence

**Purpose**: Eliminate Hierarchy visibility of pooled chunks, resolve magenta river shader error, and fix vertical river spline geometry spikes

- [X] T020 [P] Upgrade `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Pooling/ChunkObjectPool.cs` to apply `HideFlags.HideAndDontSave` and avoid EditMode pre-warming (`capacity = 0` when `!Application.isPlaying`) per Constitution IV (partial)
- [X] T021 [P] Fix river material shader resolution in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainChunkView.cs` with robust multi-pipeline shader fallback (URP/Lit, Standard, Unlit, VertexColor) to eliminate magenta error rendering per Constitution III (partial)
- [X] T022 Fix river waypoint node elevation tracking in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs` to ensure river water ribbon strictly adheres to terrain height per Constitution I & VI (partial)
- [X] T023 Update unit tests in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/DomainModelTests.cs` and `RiverMeshBuilderTests.cs` to assert `HideAndDontSave` pool flags and planar river mesh bounds per Constitution II (missing)

---

## Phase 9: Convergence

**Purpose**: Restore full QA/Developer Hierarchy Observability (Principle V) by using transparent `HideFlags.DontSave` and enforcing physical teardown on PlayMode exit

- [X] T024 [P] Refactor `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Pooling/ChunkObjectPool.cs` to use transparent `HideFlags.DontSave` (visible in Hierarchy during PlayMode, no stealth masking) and strictly enforce 0 capacity in EditMode per Constitution IV & V (contradicts)
- [X] T025 [P] Strengthen `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainGenerator.cs` lifecycle teardown to physically destroy all pooled and active `TerrainChunk` GameObjects upon `OnDisable` and PlayMode termination per Constitution IV (partial)
- [X] T026 Update unit tests in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/DomainModelTests.cs` to assert `HideFlags.DontSave` and record BUG-005/BUG-006 resolutions in `docs/troubleshooting-knowledge-base.md` per Constitution II & V (missing)

---

## Phase 10: Convergence

**Purpose**: Eliminate Inspector UI thread freeze ("Hold on" modal dialog) on tab switching and slider adjustments per Constitution Principles III & IV

- [X] T027 [P] Move `DrawNavigationTabs()` outside `EditorGUI.BeginChangeCheck()` in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs` so tab navigation never triggers 3D terrain regeneration per Constitution III & IV (contradicts)
- [X] T028 [P] Optimize Editor preview generation in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainGenerator.cs` and `TerrainDataConfigEditor.cs` to bypass heavy synchronous collider baking in EditMode live updates per Constitution IV (partial)
- [X] T029 Document BUG-012 (Inspector Tab UI Freeze & Synchronous Main Thread Regeneration) in `docs/troubleshooting-knowledge-base.md` per Constitution V (missing)

---

## Phase 11: Convergence

**Purpose**: Eliminate floating sky river geometry (Sky Aqueducts) and resolve magenta water shader across all Unity render pipelines

- [X] T030 [P] Fix river waypoint elevation conformation in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs` and `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs` so water ribbon strictly conforms to local carved terrain elevation per Constitution I & VI (contradicts)
- [X] T031 [P] Implement robust multi-pipeline water material creation in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainChunkView.cs` with verified transparent water shader fallback to permanently eliminate magenta error rendering per Constitution III (partial)
- [X] T032 Update unit tests in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/RiverMeshBuilderTests.cs` and document BUG-013 in `docs/troubleshooting-knowledge-base.md` per Constitution II & V (missing)

---

## Phase 12: Convergence

**Purpose**: Eliminate quadratic height multiplier inflation in hydrology elevation sampling so rivers rest directly on terrain surface

- [X] T033 [P] Remove quadratic `HeightMultiplier` multiplication in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs` `SampleElevation` so river mesh coordinates strictly match terrain mesh elevations in world meters per Constitution I & VI (contradicts)
- [X] T034 Update unit tests in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/RiverMeshBuilderTests.cs` to assert river vertex elevations never exceed single-multiplier world terrain height per Constitution II (missing)
- [X] T035 Document BUG-014 (Quadratic Height Multiplier Inflation in Hydrology Elevation Sampling) in `docs/troubleshooting-knowledge-base.md` per Constitution V (missing)

---

## Phase 13: Convergence

**Purpose**: Implement adaptive slope step sizing, dynamic stream widths, and terrain-clinging waterfall geometry for natural mountain streams and cascades

- [X] T036 [P] Refactor `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/HydrologyService.cs` with adaptive slope step sizing (shorter steps on steep cliffs) and stream order-based dynamic widths per Constitution I & III (partial)
- [X] T037 [P] Enhance `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Services/RiverMeshBuilder.cs` with slope-aligned lateral vertices and surface-clinging elevation profiling for waterfalls per Constitution I & III (contradicts)
- [X] T038 Update unit tests in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/RiverMeshBuilderTests.cs` and document BUG-015 in `docs/troubleshooting-knowledge-base.md` per Constitution II & V (missing)














