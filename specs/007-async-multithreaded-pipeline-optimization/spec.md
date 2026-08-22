# Feature Specification: Asynchronous Multithreaded Pipeline & Streaming Optimization

**Feature Branch**: `007-async-multithreaded-pipeline-optimization`

**Created**: 2026-08-22

**Status**: Draft

**Input**: User description: "007-async-multithreaded-pipeline-optimization: усунення фризів і падіння FPS до 3-4 при завантаженні та генерації чанків, повне винесення генерації візуальних та річкових сіток у фонові потоки, асинхронне запікання колізій Physics.BakeMesh, бюджетування часу кадру (Time-Slicing) та рефакторинг антипатерну довгих списків параметрів (12-15 параметрів) у TerrainShaperContext."

## Clarifications

### Session 2026-08-22

- Q: Яка стратегія бюджетування кадру (Time-Slicing) на головному потоці? → A: Option C: Гібридний підхід (жорсткий бюджет часу $\le 2.0\text{мс}$ через `Stopwatch` або максимум 2 чанки за кадр).
- Q: Яка стратегія генерації колізій (PhysX Mesh Collider)? → A: Option A: Фонове запікання `Physics.BakeMesh` у `Task.Run` для всіх активних `LOD 0` чанків для досягнення 0 мс затримок на головному потоці.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Zero-Hitch Chunk Streaming & Time-Sliced Frame Activation (Priority: P1)

As a player navigating the infinite terrain or entering the world for the first time, I want all terrain chunk streaming and instantiation to occur smoothly without perceptible frame drops or stuttering, so that the game maintains a fluid 60+ FPS experience at all times.

**Why this priority**: Severe frame drops (down to 3-4 FPS) break immersion and make the game unplayable during world initialization or fast movement. Maintaining consistent frame pacing is critical before adding further environmental density (grass, foliage, props).

**Independent Test**: Can be tested by spawning a 36-chunk world at origin and verifying via automated performance profiling that the Main Thread execution budget per frame does not exceed 2.0ms, with zero hitching below 60 FPS.

**Acceptance Scenarios**:

1. **Given** 36 chunks completing background calculation in the same frame, **When** the presentation manager receives the completed results, **Then** it applies at most 2 chunks or up to a 2.0ms time budget per frame, deferring remaining chunks to subsequent frames.
2. **Given** continuous camera motion across chunk boundaries, **When** new chunks enter view distance, **Then** framerate remains stable above 60 FPS without periodic micro-stutters.

---

### User Story 2 - Off-Thread Visual & Hydrological Geometry Generation (Priority: P2)

As a system architect, I want all vertex calculations, analytical smooth normals, LOD downsampling, vertex colors, and spline-based river water ribbon meshes to be computed entirely inside background worker threads (`Task.Run`), so that the Main Thread is responsible solely for binding native GPU buffers and activating GameObjects.

**Why this priority**: Calculating 14,641 vertices and analytical normal differentials per chunk on the Main Thread creates massive CPU bottlenecks ($>15\text{ms}$ per chunk). Moving this to worker threads fully utilizes multi-core hardware.

**Independent Test**: Can be validated by executing chunk generation tasks in isolation in EditMode/PlayMode tests and verifying that both `TerrainMeshData` and `RiverWaterMeshData` are fully populated and non-null before ever reaching the Main Thread queue.

**Acceptance Scenarios**:

1. **Given** a chunk coordinate and generation request, **When** the background task runs, **Then** it computes the `HeightMap`, visual `TerrainMeshData` (with skirts), collision `TerrainMeshData` (without skirts), and `RiverWaterMeshData` off the Main Thread.
2. **Given** the completed generation payload, **When** received by the Main Thread, **Then** the Main Thread only assigns the pre-calculated vertex and triangle buffers directly to `Mesh` instances.

---

### User Story 3 - Asynchronous Physics Collision Mesh Baking (Priority: P3)

As a player interacting with physical terrain collisions, I want the physics collision mesh (PhysX BVH tree) to be pre-baked off the Main Thread, so that assigning the collision mesh to `MeshCollider` on the Main Thread takes 0.05ms instead of 10ms.

**Why this priority**: Synchronous PhysX collision mesh cooking on the Main Thread is the primary culprit behind massive frame spikes when new collision-enabled LOD 0 chunks spawn.

**Independent Test**: Can be tested by invoking asynchronous collision baking off-thread and measuring the main-thread duration of `MeshCollider.sharedMesh = bakedMesh` to be $<0.2\text{ms}$.

**Acceptance Scenarios**:

1. **Given** a collision `TerrainMeshData` generated on a worker thread, **When** preparing collision data for LOD 0 chunks, **Then** the system pre-cooks the collision geometry off-thread using asynchronous physics baking (`Physics.BakeMesh`) before passing it to the Main Thread.
2. **Given** the pre-baked collision data on the Main Thread, **When** assigned to `MeshCollider.sharedMesh`, **Then** no synchronous PhysX cooking hitch occurs.

---

### User Story 4 - Encapsulated Domain Parameter Context Refactoring (Priority: P4)

As a developer and code maintainer, I want all mathematical elevation and heightmap calculations to consume a cohesive, strongly-typed `TerrainShaperContext` instead of 12-15 loose method parameters, so that the domain interfaces adhere to clean architectural standards, remain easily extensible, and eliminate parameter list code smells.

**Why this priority**: Methods with 12-15 arguments violate clean code standards, make adding new environmental layers (e.g. moisture, temperature, erosion) brittle, and obscure data dependencies.

**Independent Test**: Can be validated via Contract Reflection Tests verifying that `ITerrainShaper.CalculateElevation` and `GenerateHeightMap` accept `in TerrainShaperContext` without breaking existing calculation determinism.

**Acceptance Scenarios**:

1. **Given** the domain interfaces in `ProjectTwo.Terrain.Core.Contracts`, **When** evaluated, **Then** `ITerrainShaper` methods accept a single `in TerrainShaperContext` parameter encapsulating all noise, tectonic, hydrology, falloff, and curve settings.
2. **Given** identical seed and configuration inputs, **When** calculated using the new context struct, **Then** elevation values match previously verified mathematical outputs with 100% precision.

---

### Edge Cases

- **Rapid Camera Teleportation**: What happens when the viewer moves faster than background tasks complete? (Tasks must be promptly aborted via `CancellationToken` and unused payloads purged from the activation queue without allocating memory).
- **Zero-River Chunks**: How does the off-thread pipeline handle chunks with no intersecting river segments? (Must return a lightweight empty mesh descriptor without allocating vertex arrays).
- **EditMode Preview Manipulation**: How does time-slicing behave in the Unity Editor Inspector when sliders are dragged? (Draft preview mode should bypass time-slicing for instant single-chunk feedback).

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST execute 100% of heightmap evaluation, visual terrain mesh generation, and river ribbon mesh generation off the Main Thread within background worker tasks.
- **FR-002**: The presentation system MUST implement a hybrid frame budget mechanism (Time-Slicing) on the Main Thread that limits chunk activation processing to $\le 2.0\text{ms}$ or a maximum of 2 chunks per frame.
- **FR-003**: The background generation payload (`ChunkGenerationPayload`) MUST contain pre-built `TerrainMeshData` (visual and collision) and `RiverWaterMeshData` ready for immediate native mesh assignment.
- **FR-004**: The system MUST execute asynchronous physics collision mesh baking (`Physics.BakeMesh`) in background worker tasks for all LOD 0 chunks to eliminate Main Thread PhysX cooking latency.
- **FR-005**: The system MUST encapsulate domain settings into a readonly struct `TerrainShaperContext` replacing the 12-15 loose parameter signatures in `ITerrainShaper` and `HeightMapBuilder`.
- **FR-006**: The system MUST update the `ContractReflectionTests` to enforce the `TerrainShaperContext` architectural standard across all core domain contracts.
- **FR-007**: The system MUST preserve 100% mathematical determinism, ensure seamless chunk boundary stitching (X and Z axes), and maintain lighting normal continuity.
- **FR-008**: The cancellation protocol (`CancellationTokenSource`) MUST instantly terminate background mesh calculation tasks upon chunk unload or world regeneration.
- **FR-009**: Memory allocations on the Main Thread during chunk streaming MUST be minimized via object pooling and reusable buffers.

### Key Entities

- **`TerrainShaperContext`**: Readonly data struct encapsulating `NoiseSettings`, `MacroMaskSettings`, `TectonicSettings`, `TectonicBoundary[]`, `HeightCurveSettings`, `WaterSettings`, `RiverSettings`, `HydrologySettings`, `RiverGraph`, and `FalloffSettings`.
- **`ChunkGenerationPayload`**: Asynchronous transfer payload containing `ChunkCoordinate`, `HeightMap`, visual `TerrainMeshData`, collision `TerrainMeshData`, and `RiverWaterMeshData`.
- **`TimeSlicedChunkActivator`**: Budget controller managing the Main Thread ingestion queue and enforcing the $\le 2.0\text{ms}$ / max 2 chunks frame budget.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Main Thread chunk processing time during continuous streaming MUST NOT exceed $2.0\text{ms}$ in any single frame.
- **SC-002**: Framerate during initial 36-chunk world generation MUST remain stable above 60 FPS (zero drop to 3-4 FPS).
- **SC-003**: 100% of mesh geometry construction (vertices, triangles, UVs, normals, vertex colors) executes on background threads.
- **SC-004**: Synchronous PhysX collision cooking on the Main Thread is reduced from $>10\text{ms}$ to $<0.2\text{ms}$ per chunk.
- **SC-005**: 100% of existing unit, integration, and contract tests pass with zero regression in visual terrain appearance or boundary continuity.
- **SC-006**: `ITerrainShaper` interface signature complexity reduced from 12-15 loose parameters to a clean 3-parameter signature using `TerrainShaperContext`.

---

## Assumptions

- Target platform is Unity 6 (URP) on modern desktop multi-core CPUs (minimum 4 physical cores).
- PhysX mesh baking off-thread is supported in Unity 6 via `Physics.BakeMesh`.
- The existing `ChunkObjectPool` will continue to manage `TerrainChunkView` GameObject lifecycles without incurring garbage collection spikes.
