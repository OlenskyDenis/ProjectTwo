# Tasks: Terrain Material Generation Module

**Feature**: `005-terrain-material-generation`
**Date**: 2026-08-21
**Status**: Completed

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish folder layout and shared data structures

- [X] T001 Create material module folder structure in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/`
- [X] T002 [P] Define `MaterialDescriptor` struct in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/MaterialDescriptor.cs`

---

## Phase 2: Foundational (Core Contracts & Cache Infrastructure)

**Purpose**: Core caching and service interfaces that block all user stories

**⚠️ CRITICAL**: Must complete before proceeding to user stories

- [X] T003 [P] Implement `IMaterialCache` contract and `TerrainMaterialCache` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/TerrainMaterialCache.cs`
- [X] T004 [P] Define `ITerrainMaterialService` interface in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/ITerrainMaterialService.cs`
- [X] T005 Create unit test suite for cache lifecycle and disposal in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/TerrainMaterialCacheTests.cs`

**Checkpoint**: Foundation ready — user story implementation can now proceed.

---

## Phase 3: User Story 1 - Dedicated Terrain & Water Material Generation (Priority: P1) 🎯 MVP

**Goal**: Provide centralized material creation, fallback resolution, and chunk renderer assignment, removing all scattered `new Material()` calls.

**Independent Test**: Supply a visual configuration to `TerrainMaterialService` and verify that valid, shared terrain and water materials are assigned to chunk renderers without duplicate allocations.

### Tests for User Story 1
- [X] T006 [P] [US1] Unit test for terrain & water material factory generation in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/TerrainMaterialServiceTests.cs`

### Implementation for User Story 1
- [X] T007 [US1] Implement `TerrainMaterialService` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/TerrainMaterialService.cs` with fallback shader resolution and property binding for terrain and water
- [X] T008 [US1] Refactor `ChunkObjectPool` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Pooling/ChunkObjectPool.cs` to receive material from `ITerrainMaterialService` instead of instantiating internal fallback materials
- [X] T009 [US1] Refactor `TerrainChunkView` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainChunkView.cs` to eliminate static `_cachedDefaultMaterial` / `_cachedDefaultRiverMaterial` and receive materials directly from the service
- [X] T010 [US1] Update `TerrainGenerator` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Components/TerrainGenerator.cs` to instantiate and lifecycle-manage `ITerrainMaterialService` and pass materials to chunk pool and chunk views

**Checkpoint**: User Story 1 complete and independently testable as an MVP.

---

## Phase 4: User Story 2 - Configurable Multi-Layer & Biome Material Profiles (Priority: P2)

**Goal**: Enable technical artists to author modular `ScriptableObject` profiles (`TerrainVisualProfileSO`, `WaterVisualProfileSO`) with live reactive inspector updates without rebuilding chunk geometry.

**Independent Test**: Create two distinct visual profiles (e.g., Alpine vs Desert), swap them in inspector, and verify that material properties update in real-time across active chunk renderers.

### Tests for User Story 2
- [X] T011 [P] [US2] Unit test for profile updates and live material property modification in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/TerrainVisualProfileTests.cs`

### Implementation for User Story 2
- [X] T012 [P] [US2] Create `BiomeVisualBand` struct and `TerrainVisualProfileSO` ScriptableObject in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Config/TerrainVisualProfileSO.cs`
- [X] T013 [P] [US2] Create `WaterVisualProfileSO` ScriptableObject in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Config/WaterVisualProfileSO.cs`
- [X] T014 [US2] Update `TerrainDataConfig` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Config/TerrainDataConfig.cs` and `TerrainDataConfigEditor` in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs` to reference `TerrainVisualProfileSO` and `WaterVisualProfileSO`
- [X] T015 [US2] Implement `OnProfileChanged` event propagation and `UpdateTerrainMaterialProperties` / `UpdateWaterMaterialProperties` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/TerrainMaterialService.cs`

**Checkpoint**: User Stories 1 and 2 both fully functional.

---

## Phase 5: User Story 3 - Decoupled Core Domain & Material Abstraction (Priority: P3)

**Goal**: Decouple domain models (`TerrainRegion`, `WaterSettings`) from engine `UnityEngine.Material` references, satisfying Clean Architecture and Constitution Principle I.

**Independent Test**: Execute pure C# domain tests and reflection checks confirming zero `UnityEngine.Material` references in `ProjectTwo.Terrain.Core`.

### Tests for User Story 3
- [X] T016 [P] [US3] Add architectural decoupling unit test in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/DomainDecouplingTests.cs` validating zero `UnityEngine.Material` references in `ProjectTwo.Terrain.Core`

### Implementation for User Story 3
- [X] T017 [P] [US3] Refactor `TerrainRegion` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/TerrainRegion.cs` to remove `UnityEngine.Material` field and replace with `MaterialDescriptor`
- [X] T018 [P] [US3] Refactor `WaterSettings` in `ProjectTwoUnity/Assets/Scripts/Terrain/Core/Models/WaterSettings.cs` to remove `UnityEngine.Material` field and replace with `MaterialDescriptor`

**Checkpoint**: All user stories complete and decoupled.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and zero-stale method cleanup

- [X] T019 [P] Update XML documentation across all newly added material interfaces and classes
- [X] T020 Run full test suite and validate quickstart scenarios in `specs/005-terrain-material-generation/quickstart.md`
- [X] T021 Clean up any obsolete methods or deprecated material fields across the solution to satisfy Constitution Principle VI

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: Completed.
- **Foundational (Phase 2)**: Completed.
- **User Stories (Phases 3–5)**: Completed:
  - **US1 (P1)**: Core material service and view integration.
  - **US2 (P2)**: Extends US1 with ScriptableObject profile assets and live property updates.
  - **US3 (P3)**: Decouples Core models from Unity engine material types.
- **Polish (Phase 6)**: Completed.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete Phase 1 (Setup) and Phase 2 (Foundational).
2. Implement Phase 3 (User Story 1: `TerrainMaterialService`, `ChunkObjectPool`, `TerrainChunkView`).
3. Run tests in `TerrainMaterialServiceTests.cs`.
4. Validate that materials render properly on chunks with zero `new Material()` in pooling code.

### Incremental Delivery
1. Foundation & MVP: Centralized material factory & caching.
2. US2 Delivery: Modular ScriptableObject profiles & live inspector updates.
3. US3 Delivery: Clean Architecture core domain decoupling.
4. Polish & Verification: Complete regression tests & cleanup.

---

## Phase 7: Convergence

**Purpose**: Resolve gap findings between specification, actual shaders, and default asset setups

- [X] T022 [US1] Correct default shader name constants in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/TerrainMaterialService.cs` from `ProjectTwo/TerrainVertexColor` to `ProjectTwo/Terrain/VertexColorLit` and `ProjectTwo/WaterSimple` to `ProjectTwo/Terrain/WaterSimple` per FR-003, FR-006, US1/AC2 (partial)
- [X] T023 [P] [US2] Create default `DefaultTerrainVisualProfile.asset` and `DefaultWaterVisualProfile.asset` in `ProjectTwoUnity/Assets/Settings/` for 1-click inspector assignment per FR-008, US2/AC1 (missing)

---

## Phase 8: Convergence

**Purpose**: Resolve UI inspector tab locking, live season updates, and shader texture/tint parameter binding

- [X] T024 [US2] Add scene regeneration trigger `RegenerateActiveSceneTerrain()` and serialized property synchronization on season button clicks in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs` per FR-008, US2/AC3 (partial)
- [X] T025 [US2] Fix IMGUI layout synchronization and tab toolbar state to prevent inspector locking in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs` per Constitution III, FR-008 (contradicts)
- [X] T026 [P] [US1] Add `_BaseColor` property, CBuffer entry, and fragment multiplication to `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Shaders/TerrainVertexColor.shader` for dynamic global tint support per FR-003, US1/AC1 (partial)
- [X] T027 [P] [US2] Implement `TerrainTriplanar.shader` (`ProjectTwo/Terrain/TriplanarLit`) in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Shaders/TerrainTriplanar.shader` supporting height/slope texture layer blending for `TerrainVisualProfileSO` per FR-003, US2/AC1 (partial)

---

## Phase 9: Convergence

**Purpose**: Implement procedural texture generator engine, interactive editor PNG baker, and default texture bundle bindings

- [X] T028 [P] [US2] Implement `ProceduralTextureGenerator.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/ProceduralTextureGenerator.cs` supporting seamless Perlin, Voronoi/cellular noise, multi-octave surface patterns, and Normal Map generation per FR-003, US2/AC1 (missing)
- [X] T029 [US2] Create unit test suite in `ProjectTwoUnity/Assets/Scripts/Terrain/Tests/EditMode/ProceduralTextureGeneratorTests.cs` validating seamless wrap continuity and normal map formatting per FR-003, US2/AC1 (missing)
- [X] T030 [US2] Implement `ProceduralTextureBakerWindow.cs` in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/ProceduralTextureBakerWindow.cs` with live interactive preview, preset templates (Grass, Rock, Sand, Snow), and 1-click PNG asset export per FR-008, US2/AC2 (missing)
- [X] T031 [P] [US2] Bake standard procedural PNG textures into `ProjectTwoUnity/Assets/Textures/Terrain/` and bind them to `DefaultTerrainVisualProfile.asset` / `TerrainDataConfig.asset` with `TerrainTriplanar.shader` per FR-008, US2/AC1 (missing)

---

## Phase 10: Convergence

**Purpose**: Auto-populate TerrainConfig.Regions layer slots with baked textures on first startup or bake

- [X] T032 [US2] Extend `AssignTexturesToDefaultProfile()` in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/ProceduralTextureBakerWindow.cs` to auto-assign baked textures into `TerrainConfig.asset`'s `Regions` array (`AlbedoTexture` / `NormalMap`) per FR-012, US2/AC1 (partial)

---

## Phase 11: Convergence

**Purpose**: Preserve assigned textures and auto-bind baked textures when switching seasonal color palettes

- [X] T033 [US2] Update `ApplySeasonBiome()` in `ProjectTwoUnity/Assets/Scripts/Terrain/Editor/TerrainDataConfigEditor.cs` to retain existing texture bindings or re-bind baked textures by layer semantics when applying season presets per FR-008, US2/AC1 (partial)

---

## Phase 12: Convergence

**Purpose**: Bind triplanar textures in TerrainMaterialService and assign TerrainTriplanar shader to visual profile

- [X] T034 [P] [US2] Extend `ApplyTerrainProperties()` in `ProjectTwoUnity/Assets/Scripts/Terrain/Presentation/Materials/TerrainMaterialService.cs` to pass `_FlatTex`, `_FlatNormal`, `_SlopeTex`, `_SlopeNormal`, `_PeakTex`, `_PeakNormal`, tiling and thresholds from `profile.BiomeBands` per FR-003, US2/AC1 (missing)
- [X] T035 [US2] Configure `DefaultTerrainVisualProfile.asset` to use `TerrainTriplanar.shader` (`ProjectTwo/Terrain/TriplanarLit`) with `EnableTriplanarBlending = true` per FR-008, US2/AC1 (partial)






