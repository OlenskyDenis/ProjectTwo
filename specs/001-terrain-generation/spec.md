# Feature Specification: Procedural Terrain Generation via Perlin Noise

**Feature Branch**: `001-terrain-generation`

**Created**: 2026-08-19

**Status**: Draft

**Input**: User description: "Створи модуль під назвою Terrain, який за допомогою Шуму Перліна процедурно генеруватиме ланшафт. Переглянь вимоги з точки зору комфорту редагування та налаштувань. Користув повинен мати змогу швидкого, легкого та зрозумілого налаштування, редагування, додавання та видалення любих наявних та суміхни елементів. Наприклад, у нас на карті визначено, що генеруються біоми чи земля , пода, гори. Це повнино визначатись в окремому файлі який буде передаватись як налаштування до самого генератора з коментарями і описом. Переглянь специфікацію з точки зору подальшої інтеграції з іншими модулями по типу генератора наповнення світу, декором, будинками, мостами і тд. Переглянь специфікацію з точки зору оптимізації, так як процедурна генерація буде генерувати карту безкінечно, потрібно щоб ландшафт провантажувався плавно без фрізів (Multi-LOD стрімінг із збереженням). Переглянь специфікацію з точки зору розробника."

## Clarifications

### Session 2026-08-19

- Q: How should the Terrain module represent and output the generated landscape in the game engine? → A: Option A — Custom Procedural 3D Mesh (generates vertices, triangles, UVs, normals, and MeshCollider dynamically from heightmap data).
- Q: How should designers and developers trigger and preview terrain generation in the Unity development workflow? → A: Option A — Editor Preview + Auto-Update & Runtime API (in-Editor Inspector generation button and auto-update toggle alongside programmatic runtime APIs).
- Q: How should the terrain surface visually represent different elevation regions (such as water, sand, grass, rock, and snow)? → A: Option A — Elevation Region Coloring (configurable height thresholds with color mapping applied to the generated mesh/material).
- Q: How should the external configuration file for terrain parameters, biomes, and landscape elements be structured and managed in the project? → A: Option A — Unity ScriptableObject Assets (standalone modular configuration data assets with comprehensive tooltips, field validation ranges, dynamic add/remove/reorder lists for biomes/layers, and easy preset swapping).
- Q: How should the Terrain module expose its surface data and generation lifecycle events to downstream world-building modules (such as decor, foliage, bridge, and building generators)? → A: Option A — Decoupled ITerrainProvider Interface & Lifecycle Event (exposing spatial queries like GetHeight, GetNormal, GetSlope, GetBiomeAt with bilinear interpolation, and an OnTerrainGenerated event to trigger downstream placers).
- Q: How should the infinite chunk streaming and asynchronous loading system be architected to eliminate frame stutter during continuous landscape generation? → A: Multi-LOD Threaded Chunk Streaming with Chunk State Persistence (asynchronous background calculation, distance-based LOD mesh simplification, seamless chunk borders, chunk pooling, and persistent chunk caching/storage for visited areas).
- Q: How should the internal code architecture separate pure procedural algorithms from Unity engine dependencies (MonoBehaviour, UnityEngine.Object)? → A: Option A — Layered Clean Architecture (pure C# domain core for Perlin noise math, heightmaps, LOD geometry, and storage; lightweight Unity bridge layer for GameObjects, ScriptableObjects, and GPU mesh uploads).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Infinite Procedural Landscape & Asynchronous Chunk Streaming (Priority: P1)

As a player or world explorer, I want the terrain to generate infinitely in all directions as I move, with new chunks loading smoothly on background threads without frame drops or hitches, so that the gameplay experience remains continuous and stutter-free.

**Why this priority**: Foundational architecture for open-world infinite landscape generation. Without asynchronous chunk streaming, generating infinite geometry will cause severe main-thread freezing.

**Independent Test**: Can be tested by moving a viewer across multiple chunk coordinates at high speed and observing that chunks dynamically spawn ahead of the viewer via background tasks without dropping below 60 FPS on standard hardware.

**Acceptance Scenarios**:

1. **Given** an active viewer moving across the world, **When** entering a new chunk coordinate threshold, **Then** surrounding chunks within the configured view radius are loaded and rendered asynchronously.
2. **Given** continuous viewer movement, **When** new chunks are constructed, **Then** all noise and vertex calculations execute off the main thread, keeping per-frame main-thread overhead under 2 milliseconds.
3. **Given** chunks that fall outside the active view radius, **When** the viewer moves away, **Then** out-of-range chunk GameObjects are disabled and returned to an object pool to prevent memory leaks and garbage collection spikes.

---

### User Story 2 - Distance-Based Multi-Level of Detail (LOD) & Seamless Borders (Priority: P2)

As a player, I want close-up terrain to render at maximum detail and physics accuracy while distant terrain uses simplified mesh LODs, without any visible gaps, cracks, or seams along chunk boundaries.

**Why this priority**: Essential performance optimization ensuring high rendering frame rates and manageable vertex counts across wide viewing distances.

**Independent Test**: Can be tested by positioning the camera at varying distances from a chunk and verifying that the mesh automatically switches between LOD resolution levels, while verifying that adjacent chunks of differing LOD levels maintain perfectly sealed borders.

**Acceptance Scenarios**:

1. **Given** a chunk close to the viewer (within LOD 0 distance), **When** rendered, **Then** it generates at full vertex resolution with an active `MeshCollider`.
2. **Given** a chunk at distant ranges (LOD 1, LOD 2, LOD 3), **When** rendered, **Then** it displays simplified vertex grids with disabled colliders to minimize compute and render overhead.
3. **Given** two neighboring chunks with different LOD levels, **When** rendered side-by-side, **Then** edge vertices align seamlessly with zero visible cracks or gaps.

---

### User Story 3 - Persistent Chunk State & Cache Storage (Priority: P3)

As a player or world designer, I want previously visited or modified terrain chunks to be cached and persisted, so that returning to previous coordinates restores the exact state instantly without redundant recalculation or state loss.

**Why this priority**: Prevents redundant CPU recomputation when backtracking and enables long-term world state preservation (e.g., terrain excavations, placed structures).

**Independent Test**: Can be tested by travelling to a chunk, leaving its view radius (triggering unload and disk/memory cache serialization), returning to that chunk, and validating that it reloads from persistent cache in under 10 milliseconds with identical data.

**Acceptance Scenarios**:

1. **Given** a chunk being unloaded from memory, **When** persistence is enabled, **Then** its elevation/modification data is serialized to the chunk cache storage.
2. **Given** a viewer returning to a previously generated chunk coordinate, **When** loading the chunk, **Then** the system loads from cache rather than recomputing raw noise from scratch.

---

### User Story 4 - Modular Configuration Assets & Biome Management (Priority: P4)

As a designer, I want all terrain generation settings, noise properties, chunk dimensions, LOD distances, and biome definitions (e.g., water, land, mountains) to reside in an external, dedicated configuration asset file with rich tooltips, so that I can quickly and intuitively add, edit, reorder, delete, and swap entire terrain presets without editing code.

**Why this priority**: Maximizes designer ergonomics, workflow comfort, and project maintainability by strictly separating generation logic from design data.

**Independent Test**: Can be tested by creating two distinct configuration asset files with different biome lists and noise settings, assigning them in turn to the Terrain generator, and verifying that swapping assets instantly reconfigures the generated landscape to the new preset.

**Acceptance Scenarios**:

1. **Given** a configuration asset with editable biome layers, **When** the designer adds a new biome (e.g., "Volcano" or "Snow Peaks"), removes an existing layer, or changes height boundaries, **Then** the configuration list updates effortlessly and the generator immediately reflects the modified layers.
2. **Given** multiple saved configuration assets in the project, **When** a user drags and drops a different configuration asset into the generator reference field, **Then** the generator loads the new preset and refreshes the terrain.
3. **Given** any property in the configuration asset Inspector, **When** the designer hovers over or edits a field, **Then** descriptive tooltips and validated numeric sliders provide immediate guidance and guardrails.

---

### User Story 5 - Downstream Module Integration & Spatial Surface Queries (Priority: P5)

As a world-population system developer (e.g., vegetation spawner, village/building placer, road/bridge builder), I want the Terrain module to notify me when chunks generate and provide fast spatial queries for height, slope, and biome, so that I can automatically and accurately position objects, buildings, and structures on appropriate terrain locations.

**Why this priority**: Enables seamless modular integration across the world generation pipeline, preventing downstream systems from needing direct coupling to terrain mesh internals.

**Independent Test**: Can be tested by creating a mock downstream placement component that subscribes to chunk load events, queries height and slope at 100 arbitrary coordinates, and validates that all returned values align with the actual mesh elevation and steepness without calling private terrain internals.

**Acceptance Scenarios**:

1. **Given** a downstream placement module subscribed to the terrain generation/chunk load events, **When** a chunk finishes loading, **Then** an `OnChunkLoaded` event fires with the chunk provider context, prompting downstream placement to begin.
2. **Given** an arbitrary world (X, Z) coordinate on the terrain, **When** querying `GetHeight(x, z)` or `GetSlope(x, z)`, **Then** the system returns smoothly interpolated elevation and slope values.
3. **Given** an arbitrary world (X, Z) coordinate, **When** querying `GetBiomeAt(x, z)`, **Then** the system returns the exact biome/region definition active at that elevation.

---

### User Story 6 - Developer Ergonomics, Modularity & Test Automation (Priority: P6)

As a software engineer maintaining and extending the project, I want the core procedural algorithms (noise evaluation, heightmaps, mesh triangulation, and chunk coordinate math) isolated in pure C# domain services with zero Unity engine dependencies, so that I can write lightning-fast automated unit tests (>= 80% coverage), run safely across background threads without Unity API threading errors, and easily swap algorithms or storage providers.

**Why this priority**: Fulfills Constitution Principle I (Architectural Integrity & SOLID), Principle II (Comprehensive Testing Standards), and Principle V (Maintainability & Simplicity).

**Independent Test**: Can be tested by running isolated NUnit test suites verifying noise determinism, coordinate hashing, mesh vertex/triangle counts per LOD level, and storage serialization entirely in headless mode without initializing Unity GameObjects.

**Acceptance Scenarios**:

1. **Given** pure domain math services (`PerlinNoiseGenerator`, `HeightMapBuilder`, `TerrainMeshData`), **When** executed in unit tests, **Then** 100% of domain logic runs headlessly in under 2 seconds.
2. **Given** background worker tasks computing noise and mesh data, **When** executed across multiple threads, **Then** zero Unity main-thread cross-thread violations or exceptions occur.

---

### User Story 7 - Interactive & Dynamic Editor Preview (Priority: P7)

As a user or level editor, I want to request terrain generation on-demand in Editor mode with auto-update enabled, confirming that the landscape updates cleanly to reflect new parameters without lingering memory or orphaned visual artifacts.

**Why this priority**: Enables rapid design iteration in the editor before entering Play mode.

**Independent Test**: Can be tested by modifying configuration parameters in the Unity Inspector in Edit mode with auto-update enabled or clicking the Generate button, confirming that the preview updates cleanly.

**Acceptance Scenarios**:

1. **Given** an existing generated terrain in the Unity Editor, **When** the designer changes noise parameters with auto-update enabled or clicks "Generate", **Then** the old terrain geometry is cleanly replaced with the newly calculated landscape mesh and collider.

---

### User Story 8 - Elevation Region & Biome Visual Coloring (Priority: P8)

As a designer, I want to define distinct elevation regions and biomes (e.g., water, sand, grass, rock, snow) with configurable height thresholds and color tints so that the generated landscape has clear visual biome distinctions.

**Why this priority**: Provides essential aesthetic depth and visual differentiation across elevation levels without requiring external shader authoring.

**Independent Test**: Can be tested by configuring multiple elevation regions (e.g. 0.0-0.3 Blue/Water, 0.3-0.7 Green/Grass, 0.7-1.0 White/Snow) and verifying that the generated surface reflects the corresponding color distribution across heights.

**Acceptance Scenarios**:

1. **Given** configured elevation regions with assigned color values, **When** terrain is generated, **Then** areas corresponding to specific height intervals display their configured regional color tints.

---

### Edge Cases

- **Rapid Viewer Teleportation**: What happens when the viewer instantly teleports across large distances? The system must cancel in-flight background generation tasks for obsolete distant chunks, immediately request chunks around the new position, and purge out-of-range objects cleanly.
- **LOD Seam Cracks**: How does the system prevent visible gaps between adjacent chunks with different LOD resolutions? Edge vertex normal calculations and border stitching must ensure continuous seams.
- **Storage/Cache Corruption**: What happens if a serialized chunk cache file is corrupted or incompatible with a changed noise seed? The system must detect cache validation failure, log a warning, invalidate the obsolete cache, and recompute clean procedural data from source noise.
- **Missing Configuration Reference**: What happens when no configuration asset is assigned to the generator? The component must log an actionable warning in the Inspector/Console and refrain from throwing null reference exceptions or creating corrupt mesh state.
- **Extreme Scale or Octave Values**: What happens when noise scale approaches zero or octaves exceed limits? The configuration validation must clamp parameters to safe boundaries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Terrain module MUST partition the infinite landscape into discrete square chunks with deterministic 2D chunk coordinates `(chunkX, chunkZ)`.
- **FR-002**: The module MUST execute Perlin noise sampling, heightmap construction, and mesh vertex/triangle calculations asynchronously on background threads/tasks to prevent blocking the Unity main thread.
- **FR-003**: The module MUST support Multi-Level of Detail (Multi-LOD) mesh generation, dynamically adjusting chunk mesh resolution based on viewer distance according to configurable LOD distance thresholds.
- **FR-004**: The module MUST implement seamless border stitching to prevent visible cracks or geometry gaps between adjacent chunks and across differing LOD levels.
- **FR-005**: The module MUST use an object pool for chunk GameObjects, recycling mesh filters, renderers, and colliders upon chunk unload to eliminate garbage collection hitching.
- **FR-006**: The module MUST support persistent chunk caching and storage, serializing visited/modified chunk data to storage and reloading cached chunks upon viewer return in under 10 milliseconds.
- **FR-007**: The module MUST decouple all generation parameters, noise settings, chunk dimensions, LOD tiers, and biome/region definitions into a dedicated, reusable configuration asset file (`ScriptableObject`) passed to the generator.
- **FR-008**: The configuration asset MUST provide descriptive tooltips, inline comments, and bounded numeric ranges (`[Range]`, `[Tooltip]`) on all serialized fields for intuitive designer ergonomics.
- **FR-009**: The configuration asset MUST allow users to dynamically add, edit, reorder, and remove any number of biomes/terrain layers (e.g., deep water, shallow water, sand, grass, forest, rock, snow) with custom names, height thresholds, and color tints.
- **FR-010**: The module MUST expose an `ITerrainProvider` interface providing spatial query methods: `GetHeight(x, z)`, `GetNormal(x, z)`, `GetSlope(x, z)`, and `GetBiomeAt(x, z)` for downstream world-population modules (decor, foliage, buildings, bridges).
- **FR-011**: The module MUST dispatch lifecycle events (`OnTerrainGenerated`, `OnChunkLoaded`, `OnChunkUnloaded`) containing chunk and terrain context to notify downstream modules.
- **FR-012**: Spatial queries MUST support continuous world coordinate inputs using bilinear interpolation of height and normal data to ensure smooth object placement between discrete grid vertices.
- **FR-013**: The module MUST generate and update a physics collision mesh (`MeshCollider`) for close-proximity chunks (LOD 0) and disable physics on distant LODs to conserve memory and CPU cycles.
- **FR-014**: The module MUST provide Unity Editor tooling featuring a custom Inspector with an on-demand "Generate" button and an "Auto Update" toggle for real-time in-Editor previews when modifying parameters.
- **FR-015**: The module MUST isolate all mathematical, heightmap, LOD geometry, and storage serialization logic in pure C# domain classes with zero dependencies on `UnityEngine.Object` or `MonoBehaviour`.
- **FR-016**: The Unity integration MUST be implemented as a thin presentation/adapter layer (`MonoBehaviour`, `ScriptableObject`, `MeshFilter`, `MeshRenderer`) interacting with domain services via clean abstractions.
- **FR-017**: All core domain services MUST be covered by automated unit test suites achieving >= 80% branch coverage with deterministic test fixtures.
- **FR-018**: The module MUST gracefully handle edge cases such as missing configuration asset references, cache validation failures, or rapid viewer teleportation with safe fallbacks.

### Key Entities

- **Pure C# Domain Engine**:
  - `PerlinNoiseGenerator`: Deterministic multi-octave Perlin noise computation engine.
  - `HeightMapBuilder`: 2D height array builder applying octave sampling, normalization, and bounds clamping.
  - `TerrainMeshData`: Raw geometry container holding vertex arrays, triangle indices, UVs, and normals per LOD level.
  - `ChunkCoordinate`: Lightweight value struct representing `(chunkX, chunkZ)` with deterministic hashing and distance methods.
  - `ChunkPersistenceService (`IChunkStorage`)`: Storage interface responsible for saving, loading, and validating serialized chunk state to/from persistent cache or disk.
- **Unity Presentation & Infrastructure Layer**:
  - `TerrainChunk`: Unity GameObject wrapper managing LOD mesh instances, visibility, object pooling, and collider.
  - `TerrainGenerator`: Main controller coordinating viewer tracking, chunk lifecycle, async task dispatching, and downstream events.
  - `TerrainDataConfig`: Standalone serialized `ScriptableObject` holding dimensions, noise parameters, seed, LOD tiers, view distance, and biome definitions.
  - `TerrainRegion`: Reusable serialized data structure defining an individual landscape layer (name, height threshold, color tint).
  - `ITerrainProvider`: Decoupled public interface exposing spatial queries (`GetHeight`, `GetNormal`, `GetSlope`, `GetBiomeAt`) and chunk lifecycle events for downstream systems.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Sustained 60+ FPS (frame times < 16.6ms) during continuous viewer movement across chunk boundaries with 0 noticeable frame stalls/hitches (> 1 frame drop).
- **SC-002**: Asynchronous chunk generation executes 100% of noise and vertex array calculations on background threads, consuming < 2ms of main thread time per frame for mesh buffer upload.
- **SC-003**: Multi-LOD transitions produce 0 visible geometric cracks or seams along chunk boundaries.
- **SC-004**: Returning to previously visited chunk coordinates restores identical elevation and state from persistent cache in < 10 milliseconds.
- **SC-005**: Spatial point elevation and slope queries via `ITerrainProvider` execute in under 0.01 milliseconds (10 microseconds) per query, enabling fast placement of thousands of downstream decorative objects.
- **SC-006**: Parameter validation intercepts 100% of invalid configurations (missing asset, negative bounds, zero scale, negative octaves) with descriptive, actionable errors before computation starts.
- **SC-007**: Memory footprint remains stable across 50 consecutive chunk load/unload cycles with 0 resource leaks or memory growth due to object pooling.
- **SC-008**: Adding, editing, reordering, or removing a biome layer in the configuration asset takes fewer than 3 clicks in the Unity Inspector without writing any code.
- **SC-009**: Automated unit test suite for core domain math executes and passes in < 2 seconds in standard CI/NUnit test runners with >= 80% branch coverage.
- **SC-010**: Zero Unity main-thread API calls are made inside background task workers, completely preventing Unity thread-safety exceptions.

## Assumptions

- **Coordinate System**: The terrain is generated on a horizontal plane (X and Z coordinates) with elevation mapped along the vertical axis (Y coordinate).
- **Mesh Limits**: Chunk size (e.g. 240x240 or 120x120 vertices) is chosen to easily divide evenly across LOD steps (divisible by 2, 4, 6, 8, 10, 12).
- **Noise Algorithm**: Standard 2D Perlin noise / multi-octave fractal Brownian motion (fBm) evaluated asynchronously in pure C#.
- **Configuration Format**: Unity `ScriptableObject` is used for standalone configuration assets.
- **Persistence Storage**: Local binary/JSON file or memory cache storage using chunk coordinate hashes `(chunkX, chunkZ)` as keys.
- **Integration Target**: The Terrain module is structured as an isolated, testable component adhering to project SOLID principles and capable of integration within the project's game engine environment (Unity).
