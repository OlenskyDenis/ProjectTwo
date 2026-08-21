# Feature Specification: Terrain Material Generation Module

**Feature Branch**: `005-terrain-material-generation`

**Created**: 2026-08-21

**Status**: Draft

**Input**: User description: "Створимо окремий модуль який відповідатиме за генерацію матеріалів для teratin"

## Clarifications

### Session 2026-08-21
- Q: Which material rendering technique should the material generation module primarily generate and manage for terrain surfaces? → A: Hybrid: Vertex Color + Biome Tint baseline with extensible profile support for procedural textures.
- Q: How should visual material profiles (palettes, textures, shaders, water parameters) be authored and stored? → A: ScriptableObject Asset Profiles (`TerrainVisualProfileSO`, `WaterVisualProfileSO`) as modular reusable assets.
- Q: How should edits to material profile parameters in the Unity Inspector be reflected on visible terrain chunks during Play Mode and Editor Preview? → A: Real-time reactive updates: shader/color property tweaks immediately update shared materials across active chunks without re-generating chunk meshes.
- Q: How should procedural texture authoring and PNG baking be exposed to users? → A: Dedicated interactive Procedural Texture Baker Editor Window with live 2D preview + quick 1-click default texture package baking directly in the TerrainConfig inspector.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dedicated Terrain & Water Material Generation (Priority: P1)

As a world designer and graphics developer, I want a dedicated material generation system that produces and configures visual materials (surface terrain, biome colorations, and water features) from high-level visual settings, so that rendering setup is separated from mesh generation and object pooling logic.

**Why this priority**: Core architectural goal. Currently, material instantiation and fallback shader resolution are entangled across presentation views, pools, and domain models. Separating material creation into its own dedicated module establishes clean modularity and single responsibility.

**Independent Test**: Can be tested independently by supplying a terrain visual configuration to the material generator and verifying that ready-to-render material assets with correct properties (vertex colors, tint, textures, water properties) are produced and assigned without relying on scattered inline fallbacks.

**Acceptance Scenarios**:

1. **Given** a terrain configuration specifying vertex-color shading and river parameters, **When** the material generator is requested to produce materials, **Then** it generates valid material instances configured with the specified color palettes, shaders, and surface parameters.
2. **Given** missing or invalid custom material assignments, **When** the material generation is invoked, **Then** the generator provides predictable, validated default fallback materials without crashing or allocating duplicate redundant runtime instances.
3. **Given** a request to generate multiple terrain chunks, **When** chunks share identical material characteristics, **Then** the material generator provides shared material instances rather than creating redundant unique material clones per chunk.

---

### User Story 2 - Configurable Multi-Layer & Biome Material Profiles (Priority: P2)

As a technical artist, I want to define reusable visual material profiles (such as biome tints, procedural textures, slope thresholds, and water surface styles) through modular ScriptableObject assets (`TerrainVisualProfileSO`, `WaterVisualProfileSO`), so that I can easily swap and reuse visual presets across different terrain configurations without touching core geometry algorithms.

**Why this priority**: Enables rapid artistic iteration and visual richness across diverse landscape types (e.g. mountains, plains, rivers, water bodies) while adhering to the Open/Closed Principle.

**Independent Test**: Can be tested by creating two distinct visual profile assets (e.g., Alpine vs Desert) and swapping the active profile asset on the terrain configuration, verifying that terrain visual properties update consistently.

**Acceptance Scenarios**:

1. **Given** distinct biome parameters in a `TerrainVisualProfileSO` (elevation gradients, moisture, slope), **When** generating the terrain material set, **Then** the material parameters correctly map to visual surface bands and shading rules.
2. **Given** an updated material profile asset assigned in editor preview or runtime, **When** the profile is applied, **Then** all active visual chunk renderers update their material bindings consistently without leaking old material instances.
3. **Given** active terrain chunks rendered in the scene, **When** an artist tweaks color gradients or shader properties in the inspector, **Then** changes reflect in real-time across all active chunks without requiring mesh recalculation.

---

### User Story 3 - Decoupled Core Domain & Material Abstraction (Priority: P3)

As a systems architect, I want the domain calculations (heightmaps, rivers, erosion, biome data) to remain completely agnostic of engine-specific material instances, referencing only lightweight abstract material descriptors, so that domain models satisfy Dependency Inversion and Single Responsibility.

**Why this priority**: Eliminates engine/rendering coupling inside domain models (`TerrainRegion`, `WaterSettings`), preventing rendering leaks in background computation threads or headless server environments.

**Independent Test**: Can be tested by running pure domain terrain calculations and serialization in an isolated test harness without instantiating any graphics pipeline materials.

**Acceptance Scenarios**:

1. **Given** domain terrain models and region descriptors, **When** running calculation or export workflows, **Then** domain data models use abstract visual descriptors without referencing engine-specific material objects.
2. **Given** a rendering coordinator receiving calculated chunk data and visual descriptors, **When** preparing chunk presentation, **Then** the dedicated material generation module translates domain visual descriptors into concrete renderable materials.

---

### Edge Cases

- What happens when a configured shader or material asset is missing or corrupted at runtime? The system MUST fall back to a safe, built-in fallback material and log a diagnostic warning.
- What happens when hundreds of chunks stream in and out simultaneously? The material generator MUST reuse shared material instances to avoid unbounded GPU material allocation and memory leaks.
- How does the system handle material parameter changes at runtime? The system MUST update shared instances or batch-apply updates to active renderers without breaking mesh pool reuse.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a dedicated material generation and management module responsible for creating, configuring, and caching all terrain and water surface materials.
- **FR-002**: System MUST separate material creation and parameter binding from chunk mesh construction, chunk object pooling, and domain data generation.
- **FR-003**: System MUST support a hybrid material generation model: providing a lightweight Vertex Color + Biome Tint baseline while allowing extensible profile support for procedural textures and triplanar shaders.
- **FR-004**: System MUST support generating specialized water and river surface materials with configurable color, opacity, and wave/flow properties.
- **FR-005**: System MUST maintain a caching and sharing mechanism so identical visual configurations reuse shared material instances instead of duplicating instances per chunk.
- **FR-006**: System MUST provide robust fallback material resolution when user-supplied materials or custom shaders are null, invalid, or unsupported on the target platform.
- **FR-007**: System MUST decouple domain models (such as `TerrainRegion` and `WaterSettings`) from concrete engine material instances, using abstract visual descriptors in core calculations.
- **FR-008**: System MUST author and store visual material presets as modular ScriptableObject asset profiles (`TerrainVisualProfileSO`, `WaterVisualProfileSO`).
- **FR-009**: System MUST support real-time reactive updates when profile parameters are modified in the inspector during Play Mode and Editor preview without regenerating chunk meshes.
- **FR-010**: System MUST enforce proper lifecycle management (instantiation, reuse, and disposal) for generated materials to prevent memory leaks in both editor and play mode.
- **FR-011**: System MUST provide an interactive `ProceduralTextureBakerWindow` allowing users to tune procedural noise parameters, view real-time seamless tiling previews, and bake/export texture pairs (`Albedo` + `Normal Map`) to `.png` files in `Assets/Textures/Terrain/`.
- **FR-012**: System MUST provide 1-click procedural texture generation and auto-assignment within the `TerrainConfig` inspector.

### Key Entities *(include if feature involves data)*

- **TerrainVisualProfileSO**: Modular ScriptableObject defining visual styling properties, color palettes, texture references, and surface shading rules for terrain.
- **WaterVisualProfileSO**: Modular ScriptableObject defining water surface appearance, flow speed, shallow/deep color gradients, and transparency.
- **MaterialDescriptor**: Lightweight identifier or descriptor linking domain regions/biomes to visual material definitions without coupling domain logic to graphics objects.
- **MaterialRegistry / Cache**: Centralized registry that holds and serves shared material instances, resolving descriptors into concrete renderable materials.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of material creation and fallback resolution logic is centralized within the dedicated material module, removing all ad-hoc `new Material()` calls from chunk pooling and view components.
- **SC-002**: 0 direct references to engine-specific material instances remain in core domain models (`TerrainRegion`, `WaterSettings`).
- **SC-003**: In a streaming world with 100+ active terrain chunks using standard profiles, the number of distinct material instances allocated does not exceed the number of configured unique visual profiles.
- **SC-004**: Material generation and retrieval adds less than 1ms total overhead during chunk initialization.
- **SC-005**: All unit and integration tests verifying material generation, fallback behavior, caching, and profile updates pass with 100% success rate.

## Assumptions

- Standard terrain rendering utilizes vertex colors, procedural shader attributes, or standard PBR texture blending supported by the rendering pipeline.
- Material caching is scoped per terrain generation session or visual profile configuration.
- Editor workflows and runtime generation pipelines share the same material generation contracts to maintain visual parity.
