# Feature Specification: TerrainConfig Interface & Generation Capabilities Expansion

**Feature Branch**: `002-terrain-config-ui-expansion`

**Created**: 2026-08-20

**Status**: Draft

**Input**: User description: "Створимо зруний інтерфейс для TerrainConfig та розширимо моживості налаштування і генерації"

## Clarifications

### Session 2026-08-20

- Q: Як система генерації повинна розділяти глобальні макро-структури (високі гірські хребти, рівнини, океанічні западини), щоб додавання великих гір не спотворювало рівнинні ділянки світу? → A: Двошаровий підхід (Macro/Continent Mask) для незалежного масштабування амплітуди гірських і рівнинних масивів.
- Q: Як гейм-дизайнер повинен налаштовувати та призначати текстури для різних шарів/біомів ландшафту (трава, скелі, пісок, сніг) в інтерфейсі TerrainConfig? → A: Гібридні біомні шари (A + C): підтримка слотів текстур (Albedo/Normal, тайлінг, змішування за висотою та крутизною схилів) з можливістю перевизначення окремим користувацьким матеріалом для біому.
- Q: Яким чином система повинна реалізувати великі річки, озера та моря/океани, щоб водні басейни не руйнували навколишній процедурний світ і не утворювали артефактів на межах чанків? → A: Глобальний рівень води (Sea Level) з автоматичним заглибленням океанського дна + процедурна маска річкових русел (River Carve Mask) для безшовного прорізання русел.
- Q: Як гейм-дизайнеру має надаватися швидкий візуальний зворотний зв'язок (Live Preview) під час редагування параметрів шуму, річок та текстур у вікні Inspector? → A: Пряма онлайн-генерація 3D-сцени (Live 3D Scene View updates) без 2D мінімапи, з оптимізованим дебаунсингом обчислень та візуалізацією сітки/LOD.
- Q: Як система повинна управляти чергою генерації та фоновими задачами під час швидкого переміщення повзунків у редакторі, щоб гарантувати відсутність підвисань Unity Editor та витоків пам'яті? → A: Поєднання A + C: автоматичне скасування застарілих фонових задач через CancellationToken, дебаунсинг (100–150мс), генерація швидкого низькополігонального драфт-прев'ю під час безперервного перетягування повзунків з наступним повним розрахунком після фіксації значення.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Intuitive & Visual Inspector UI for Terrain Configuration (Priority: P1)

As a game designer or world builder, I want an ergonomic, well-structured, and visual inspector for `TerrainConfig` so that I can rapidly tweak, organize, and preview terrain parameters without dealing with cluttered, raw numeric fields or invalid setting combinations.

**Why this priority**: Directly solves the usability bottleneck for configuring procedural terrain. Providing intuitive visual controls (such as collapsible category sections, live min/max validation sliders, interactive biome gradient previews, and immediate visual hints) dramatically speeds up iteration time and prevents misconfigurations.

**Independent Test**: Can be fully tested in the editor by selecting a `TerrainConfig` asset, modifying noise parameters, biomes, and mesh resolution settings through organized tabbed/foldout groups, observing instant visual feedback and sanity validations, and confirming valid saved configuration states.

**Acceptance Scenarios**:

1. **Given** a user opens a `TerrainConfig` asset in the Inspector, **When** viewing the interface, **Then** parameters are organized into logical, collapsible categories (e.g., Grid & Resolution, Noise & Elevation Shaping, Biomes & Surface Regions, LOD & Streaming, Generation Presets) with clear tooltips and validation badges.
2. **Given** a user adjusts numeric parameters (such as chunk resolution or LOD distance steps), **When** invalid or non-seamless multiples are entered, **Then** the interface provides real-time validation warnings and auto-snap guidance without breaking runtime chunk alignment.
3. **Given** a user configures elevation biomes, **When** adjusting height thresholds, **Then** the user sees a visual gradient / layered height bar reflecting biome color distributions from lowest to highest elevations.

---

### User Story 2 - Expanded Terrain Generation Features & Noise Shaping (Priority: P2)

As a level designer, I want expanded procedural generation capabilities (such as multi-type noise algorithms, height redistribution curves, domain warping/falloff, and seed randomizers) so that I can generate diverse, natural landscapes ranging from rugged mountains and rolling hills to terraced plateaus and island archipelagos.

**Why this priority**: Enhances procedural generation expressiveness beyond basic Perlin noise, allowing creators to produce varied geographic topographies and richer world environments.

**Independent Test**: Can be tested independently by selecting different noise modes (e.g., standard fBm, Ridged/Mountainous, Cellular/Voronoi) and height curve remap profiles, regenerating terrain chunks, and verifying distinct geological formations.

**Acceptance Scenarios**:

1. **Given** a terrain configuration, **When** switching the noise algorithm mode (e.g., Perlin fractal, Ridged multifractal for mountain crests, Billow/Turbulent for soft rolling hills), **Then** the terrain generator produces distinct topographies corresponding to the selected mathematical model.
2. **Given** terrain generation settings, **When** applying a non-linear elevation curve (e.g., custom animation curve or power-law redistribution), **Then** terrain heights are non-linearly remapped (e.g., flattening valleys, steepening mountain peaks, or creating mesa terraces).
3. **Given** terrain generation settings, **When** enabling boundary falloff / island masking, **Then** elevation gradually falls to water level near world or map borders.
4. **Given** a terrain configuration, **When** clicking a "Randomize Seed" button or entering custom coordinate offsets, **Then** a new deterministic world layout is generated immediately while preserving all other noise parameters.

---

### User Story 3 - Preset Management & Instant Live Preview (Priority: P3)

As a designer, I want to save, load, and switch between pre-configured terrain environment presets (e.g., "Alpine Mountains", "Desert Dunes", "Rolling Plains", "Island Archipelago") and toggle responsive real-time generation previews so that I can quickly prototype and compare world styles.

**Why this priority**: Enhances creator productivity by allowing quick experimentation with pre-built terrain styles and immediate visual evaluation without manual scene setup.

**Independent Test**: Can be tested by selecting different built-in or custom presets from a dropdown, observing complete parameter re-population, and seeing live updates in the Scene/Game view.

**Acceptance Scenarios**:

1. **Given** the `TerrainConfig` inspector, **When** a user selects a preset from a template library (e.g., "Mountains", "Plains", "Islands", "Canyons"), **Then** all noise, height, and biome parameters update to match the selected template.
2. **Given** a customized terrain configuration, **When** the user clicks "Save as Preset", **Then** the current settings are exported as a reusable preset asset.
3. **Given** live edit mode is active, **When** any slider or parameter is adjusted in the inspector, **Then** the terrain mesh and heightmap preview regenerate responsively within acceptable latency budgets.

---

### Edge Cases

- **Extreme Parameter Ranges**: How does the system handle extreme noise scale (e.g., 0.0001 or 5000), excessive octaves (> 10), or negative height multipliers? (The UI and validator must clamp and enforce safe bounds preventing mathematical singularities or frame freezes).
- **Overlapping or Inverted Biome Thresholds**: What happens when biome height thresholds are configured in reverse order or with equal values? (The interface must automatically sort thresholds or highlight collisions with corrective hints).
- **Missing Material / Texture References**: How does the terrain view behave when materials or biome textures are unassigned? (A fallback default shader/material is rendered with a non-blocking warning).
- **Rapid Preset Switching / Live Updates**: What happens when presets are rapidly cycled while asynchronous chunk background tasks are computing? (In-flight tasks must be cleanly cancelled or superseded by latest generation requests without data corruption or memory leaks).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an enhanced, categorized inspector UI for terrain configuration with collapsible sections (Grid & Chunk Metrics, Noise Shaping & Algorithms, Height Redistribution, Biome / Region Layers, LOD & Caching, Preset Library).
- **FR-002**: System MUST validate terrain resolution and chunk dimensions in real-time, enforcing mathematical compatibility with seamless LOD subdivisions (steps 1, 2, 4, 6) and displaying clear status indicators.
- **FR-003**: System MUST provide an interactive visual biome/region editor with live color/threshold gradient preview, draggable reordering, and automated threshold sorting.
- **FR-004**: System MUST support multiple procedural noise synthesis modes, including standard fractal Perlin, Ridged multifractal (for sharp peaks/ridges), and Billow/turbulent noise.
- **FR-005**: System MUST support non-linear height redistribution using customizable elevation curves and power remapping (e.g., valley flattening, terrace steps, peak amplification).
- **FR-006**: System MUST support optional boundary falloff / island shaping masks to gracefully blend terrain edges towards baseline or water levels.
- **FR-007**: System MUST provide quick utility actions within the inspector, including one-click seed randomization, parameter reset, and validation/snapping shortcuts.
- **FR-008**: System MUST support preset templates (e.g., built-in archetypes like Alpine Peaks, Rolling Grasslands, Archipelago, Desert Plateaus) with one-click applying, exporting, and saving.
- **FR-009**: System MUST support responsive live preview in Edit and Play modes, automatically throttling and debouncing regeneration triggers to maintain smooth editor interaction.
- **FR-010**: System MUST provide a Macro Continent / Mountain Mask layer allowing designers to modulate noise amplitude and height multipliers per region, isolating high mountain peaks from low flat valleys.
- **FR-011**: System MUST provide hybrid biome texturing: individual biome layers in the Inspector support slots for textures (Albedo, Normal maps, Tiling scale, height/slope blend softness for automatic triplanar/slope rock transitions) with an optional override slot for custom Unity Materials.
- **FR-012**: System MUST support a configurable Water / Sea Level baseline and procedural river carving mask (River Carve Settings) that smoothly depresses heightmaps along procedural drainage paths down to sea level across chunk boundaries without vertex tearing or border seams.
- **FR-013**: System MUST enforce cooperative cancellation (using `CancellationTokenSource`) and debounced scheduling (100–150ms) for all asynchronous terrain generation tasks, instantly dropping obsolete requests when new configuration parameters arrive.
- **FR-014**: System MUST utilize a progressive preview pipeline during active interactive dragging (generating immediate low-resolution draft meshes during continuous slider manipulation and triggering full-fidelity mesh generation upon input settlement).
- **FR-015**: System MUST enforce strict resource lifecycle cleanup (destroying temporary meshes and materials explicitly via `DestroyImmediate` in Edit mode and recycling data arrays via pooling) to prevent memory leaks during long-running editor sessions.

### Key Entities *(include if feature involves data)*

- **TerrainDataConfig**: ScriptableObject configuration asset aggregating grid dimensions, noise synthesis settings, macro continent masks, elevation curves, biome layer definitions, water/river parameters, LOD tiers, and visual styling properties.
- **NoiseSettings**: Value model capturing noise algorithm mode (Perlin, Ridged, Billow), seed, scale, octaves, persistence, lacunarity, height multiplier, coordinate offsets, and domain warping parameters.
- **MacroMaskSettings**: Configuration entity defining low-frequency continental / regional noise masks to blend and isolate mountainous terrain from flat plains.
- **WaterSettings**: Configuration entity specifying global sea level elevation, ocean basin depth, underwater slope blending, and optional water surface plane references.
- **RiverCarveSettings**: Configuration entity defining procedural river channel paths, carve depth, riverbed width, and transition softness.
- **HeightRemapCurve**: Configuration entity specifying elevation scaling, power exponents, and curve interpolation profiles for non-linear vertical shaping.
- **FalloffSettings**: Configuration entity defining edge damping modes (e.g., Circular, Square, None) with customizable falloff radius and curve sharpness.
- **TerrainPreset**: Reusable preset definition containing complete parameter sets for distinct biome archetypes (e.g., Mountains, Archipelago, Hills, Canyons).
- **TerrainRegion / BiomeLayer**: Entity specifying elevation threshold, color tint, Albedo/Normal texture assignments, UV tiling/offset, blend transition softness, slope steepness thresholds, and optional custom material override.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Designers can configure and validate a complete procedural terrain preset in under 2 minutes through the enhanced inspector interface.
- **SC-002**: 100% of generated chunk configurations satisfy seamless LOD divisibility rules, eliminating border cracking and vertex gap artifacts across all supported chunk sizes.
- **SC-003**: Live editor parameter adjustments provide visible preview feedback within 150ms for local preview chunks without editor UI freezing or frame dropping.
- **SC-004**: The terrain generator supports at least 3 distinct procedural topography styles (e.g., Ridged mountains, rolling plains, archipelagos) with distinct visual and geometric characteristics.
- **SC-005**: Presets can be switched and applied with a single click, completely updating scene terrain generation state deterministically.
- **SC-006**: Continuous slider dragging over 60 seconds generates zero memory growth / leaks (0 uncollected orphaned meshes or unmanaged buffers) and maintains >= 60 FPS in Editor Scene View.

## Assumptions

- Target environment is Unity Editor (2022 LTS or newer) and runtime gameplay.
- Single-chunk and multi-chunk streaming generators both utilize the centralized `TerrainDataConfig` asset structure.
- The procedural height generation will continue to compute deterministically on background worker threads using pure mathematical functions.
- Preset templates are stored as asset files or serializable presets within the project.
- Advanced features like GPU compute shader generation or real-time thermal hydraulic erosion can be integrated modularly as future extensions without altering the core configuration schema.
