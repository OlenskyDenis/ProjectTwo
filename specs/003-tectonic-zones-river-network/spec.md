# Feature Specification: Macro-Tectonic Zoning & Hydrological River Graph System

**Feature Branch**: `003-tectonic-zones-river-network`

**Created**: 2026-08-20

**Status**: Draft

**Input**: User description: "Створимо систему глобального макро-зонування на основі тектонічних ліній для формування суцільних гірських хребтів та інтегруй векторний граф річок, який за градієнтом висот і гідрологічною ерозією прорізатиме неперервні русла від витоків до низовин."

## Clarifications

### Session 2026-08-20

- Q: Яким математичним підходом система повинна генерувати та представляти глобальні тектонічні плити для швидкого обчислення висот у чанках? → A: Процедурна клітинна діаграма Вороного/Делоне (Jittered Voronoi/Delaunay Cells) у світовому просторі з векторами дрейфу та безперервними полями відстаней до меж, оптимізована для максимальної продуктивності (нульові алокації, сумісність з Unity C# Job System / Burst, без фрізів і лагів).
- Q: Як векторний граф річок повинен формувати та відображати водну поверхню на похилих ділянках ландшафту (від гірських витоків до впадіння в океан)? → A: Гібридне гідравлічне прорізання русла в ландшафті + процедурна генерація стрічкових мешів водної поверхні (River Mesh Ribbons) уздовж сегментів графу з реалістичними геоморфологічними профілями (V-подібні русла у верхів'ях, алювіальні меандри в долинах, UV-потік за вектором течії) та безшовним злиттям з глобальним океаном на рівні Sea Level.
- Q: Яким чином гідрологічний алгоритм повинен вирішувати проблему локальних западин (замкнених котловин / pits / sinks), де градієнт висоти блокує стік води до океану? → A: Геоморфологічний гібрид: невеликі бар'єри та сідловини прорізаються ерозійним каналом (Carve Breach Channel), а глибокі/просторі котловини утворюють процедурні озера (Lake Basins) з автоматичним розрахунком рівня дзеркала води (Spillover elevation) та витоком річки з точки переливу до низовин.
- Q: Як векторний граф річок повинен генеруватися та кешуватися для забезпечення стабільного стрімінгу чанків без просідання FPS? → A: Ієрархічні макро-басейни (Macro Watershed Regions) з просторовим індексом: річковий граф генерується для макро-секторів у фонових C# Jobs/Burst і зберігається у просторовому хеш-індексі (Spatial Hash Grid) для миттєвої (<0.5 мс) вибірки кривих під час генерації окремих чанків без блокування кадрової частоти.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuous Mountain Ridges via Tectonic Boundary Zoning (Priority: P1)

As a level designer and world builder, I want a macro-zoning system based on global tectonic plates and fault lines so that procedural terrain naturally forms massive, continuous mountain chains along convergent boundaries, rift valleys along divergent zones, and stable plateaus/lowlands across continental interiors.

**Why this priority**: Solves the fundamental problem of incoherent, fragmented mountain peaks created by naive per-pixel noise. By anchoring terrain uplift to continuous structural fault lines, the generator produces geologically plausible continents and unbroken mountain ranges.

**Independent Test**: Can be tested independently by configuring tectonic plate seeds, plate count, and boundary parameters, generating macro terrain heightmaps, and verifying that continuous mountain chains align along convergent plate edges across multiple chunks while interior plate areas remain smooth lowlands/plains.

**Acceptance Scenarios**:

1. **Given** a world configuration with defined tectonic plates, **When** the macro terrain is generated, **Then** continuous mountain ridges are formed along convergent plate boundaries with realistic elevation uplift that falls off smoothly into adjacent continental plates.
2. **Given** divergent or transform plate boundaries, **When** macro zoning is computed, **Then** divergent boundaries produce sunken rift valleys/trenches and transform boundaries produce sheared lateral hills without creating jarring elevation cliffs.
3. **Given** adjacent terrain chunks spanning a tectonic boundary, **When** chunks are evaluated independently or streamed asynchronously, **Then** mountain heights and boundary distances match seamlessly across chunk borders with zero vertex cracks or elevation mismatches.

---

### User Story 2 - Vector River Network & Gradient-Based Pathfinding (Priority: P2)

As a game designer, I want a vector river graph system that generates connected waterways starting from high-elevation mountain springs, following natural height gradients, joining at confluences, and flowing continuously into lakes or ocean basins.

**Why this priority**: Water is the defining feature of realistic geographic landscapes. A vector graph structure ensures global topological continuity and connectivity, preventing isolated puddles or cut-off river segments.

**Independent Test**: Can be tested independently by initiating river path generation on a generated macro heightmap, verifying that river nodes trace steepest descent trajectories down to sea level, join into tributaries, and form an acyclic drainage graph.

**Acceptance Scenarios**:

1. **Given** terrain with high mountain ridges and low ocean basins, **When** river generation is triggered, **Then** river sources spawn in high-elevation catchment zones and trace continuous downstream paths following elevation gradients to ocean level.
2. **Given** multiple converging river paths, **When** paths intersect within a proximity threshold, **Then** they merge into a single tributary with combined flow accumulation and increased channel capacity.
3. **Given** local elevation depressions (pits) along the path, **When** the river reaches the depression, **Then** the system resolves the depression (via pit breach routing or lake basin creation) ensuring the river never gets stuck in an unresolved infinite loop or dead-end.

---

### User Story 3 - Continuous Hydraulic Carving & Riverbed Shaping (Priority: P3)

As a player and world explorer, I want procedural rivers to carve realistic, smoothly graded channels into the terrain geometry, varying from steep V-shaped mountain canyons to wide, gently sloping lowland riverbeds.

**Why this priority**: Connects the mathematical vector river graph to the visual 3D mesh, ensuring riverbeds look organically carved into the earth rather than floating or clipping through hills.

**Independent Test**: Can be tested by generating chunks intersecting river graph segments, measuring vertex displacement along the river centerlines, and verifying smooth cross-sectional profiles and continuous downstream gradient without backward-sloping waterbeds.

**Acceptance Scenarios**:

1. **Given** a chunk intersecting one or more river graph segments, **When** the chunk heightmap is sampled, **Then** hydraulic carving depresses terrain vertices along the river spline based on distance-to-curve, flow rate, and stream order.
2. **Given** high-elevation river segments vs. low-elevation river segments, **When** carving is applied, **Then** upper reaches form narrow, steep V-shaped ravines while lower reaches form wider, U-shaped alluvial valleys.
3. **Given** a river crossing chunk boundaries, **When** adjacent chunks compute their local carved meshes, **Then** riverbed depth, width, and bank transitions align continuously across chunk edges without seam artifacts.

---

### User Story 4 - Tectonic & Hydrology Inspector Configuration and Visual Debugging (Priority: P4)

As a designer, I want intuitive configuration controls in the `TerrainConfig` inspector and editor Scene View gizmos for tectonic plates and river networks so that I can visualize plate boundaries, drift vectors, catchment basins, and river trees directly in the Unity Editor.

**Why this priority**: Enables rapid tuning of complex global macro parameters by providing immediate visual insight into the underlying mathematical graphs.

**Independent Test**: Can be tested by opening `TerrainConfig`, enabling Tectonic and Hydrology visual overlays in Scene View, adjusting plate counts and river density sliders, and observing interactive gizmo updates.

**Acceptance Scenarios**:

1. **Given** the `TerrainConfig` inspector, **When** the user navigates to the Tectonics section, **Then** controls for plate count, plate jitter, ridge uplift height, boundary width, and noise warping are available with real-time validation.
2. **Given** the Hydrology section, **When** the user adjusts river density, carve depth, and flow accumulation thresholds, **Then** Scene View debug gizmos display the updated vector river network and drainage paths.
3. **Given** live preview mode, **When** tectonic or river parameters change, **Then** terrain chunks update deterministically and responsively.

---

### Edge Cases

- **Local Elevation Depressions (Sinks/Pits)**: What happens when a river flow path enters an enclosed crater or valley where all neighboring points are higher? (The system employs depression filling / topological breach routing or terminates into an endorheic lake basin at water equilibrium level).
- **Ridge-Crossing Conflicts**: What happens when a river path encounters an opposing tectonic ridge or steep barrier? (Rivers follow lowest saddles / passes, or carve narrow gorge cuts if no downhill bypass exists within search radius).
- **Chunk Seam Discontinuities**: What happens when a river spline crosses obliquely through chunk corners or boundaries? (Distance fields and spline influence functions are evaluated analytically in world coordinates, guaranteeing identical height deductions on both sides of chunk borders).
- **Dense Tributary Clumping**: What happens when dozens of river sources spawn in close proximity? (The system enforces minimum distance between sources and merges nearby parallel streams using stream order filtering).
- **Flat Plains & Low Gradient Ambiguity**: What happens when a river reaches an expansive, flat plain where the height gradient is nearly zero ($\nabla h \approx 0$)? (The system uses directional momentum, Voronoi flow field guidance, or subtle meandering noise to guide the river smoothly to the nearest ocean boundary).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate a global macro-tectonic partition dividing the world into discrete crustal plates with assigned centroids, plate types (continental vs. oceanic), and motion/drift vectors.
- **FR-002**: System MUST identify and classify plate boundaries into convergent (collision/subduction), divergent (rifting/trench), and transform (strike-slip) boundary types based on relative plate motion vectors.
- **FR-003**: System MUST compute continuous mountain ridge uplift along convergent tectonic lines, modulating elevation via distance-field falloff, ridge sharpness curves, and structural noise distortion.
- **FR-004**: System MUST generate a globally connected vector river graph composed of nodes (springs, confluences, lake inlets/outlets, ocean mouths) and spline/polyline edges.
- **FR-005**: System MUST compute river source placement in high-elevation catchment areas and calculate downstream trajectories based on steepest descent height gradients.
- **FR-006**: System MUST calculate hydrological flow accumulation and stream order (e.g., Strahler or Shreve ordering) along the river network, propagating discharge volumes downstream.
- **FR-007**: System MUST apply continuous hydraulic erosion carving to terrain heightmaps, dynamically adjusting riverbed width, bank slope, and channel depth proportionally to stream order and flow accumulation.
- **FR-008**: System MUST resolve topological sinks and local minima using depression filling, saddle breaching, or inland lake basin generation, ensuring no river dead-ends above base level.
- **FR-009**: System MUST guarantee mathematical continuity and seam-free alignment of tectonic uplift and river carving across all chunk boundaries and LOD tiers.
- **FR-010**: System MUST expose configurable tectonic parameters (plate count, boundary width, mountain uplift multiplier, rift depth, fault distortion) and hydrology parameters (source density, carve depth curve, channel width scale, meander intensity) in `TerrainDataConfig` / sub-settings.
- **FR-011**: System MUST provide visual debug rendering in the Unity Editor (Scene View gizmos / overlay passes) displaying plate polygons, motion vectors, fault lines, river graph nodes, and flow vectors.
- **FR-012**: System MUST perform tectonic evaluation and river graph sampling asynchronously on background worker threads without triggering main thread frame hitches or garbage collection spikes.
- **FR-013**: System MUST generate procedural river water surface ribbons (sloped procedural water mesh segments) with flow UV coordinates along active river splines, smoothly transitioning into ocean water planes at Sea Level.
- **FR-014**: System MUST partition world hydrology into Macro-Watershed Regions and index river segments in a Spatial Hash Grid, ensuring fast spatial distance queries (<0.5ms per chunk sampling).
- **FR-015**: System MUST compute tectonic Voronoi cell boundaries and boundary distance fields using allocation-free structs and Unity C# Job System / Burst compilation to eliminate main-thread stuttering.

### Key Entities *(include if feature involves data)*

- **TectonicPlate**: Data structure representing a continental or oceanic crustal plate with centroid position, boundary polygon, plate type, and drift velocity vector.
- **TectonicBoundary**: Data structure representing an edge/fault line between adjacent plates, including boundary classification (convergent, divergent, transform), relative collision velocity, influence radius, and uplift profile.
- **TectonicSettings**: Configuration model defining macro plate generation parameters (seed, plate count, boundary width, mountain height multiplier, rift depression, and domain warping).
- **RiverNode**: Data structure representing a topological point in the river graph (source, junction/confluence, lake boundary, or terminal ocean mouth) with world position, elevation, and connectivity links.
- **RiverSegment**: Data structure representing a continuous river channel edge between two `RiverNode`s, containing parametric curve/spline points, flow accumulation, channel width, carve depth, and stream order.
- **RiverGraph**: Global container encapsulating the complete network of river nodes and segments, supporting spatial index queries (Spatial Hash Grid) for fast local chunk sampling.
- **RiverWaterMeshGenerator**: Procedural mesh component generating sloped water surface ribbons with flow UVs along river splines.
- **LakeBasin**: Data structure representing a depression lake basin with boundary polygon, water spillover elevation, and outlet river node reference.
- **HydrologySettings**: Configuration model defining hydrological parameters (source elevation threshold, source density, flow accumulation multipliers, channel carve curves, valley width, and lake formation rules).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mountain ranges generated along convergent tectonic boundaries form continuous, unbroken chains spanning at least 4 contiguous chunks without arbitrary gaps or disconnected single peaks.
- **SC-002**: 100% of generated river trajectories originating at valid sources reach an ocean or designated lake basin without encountering unresolved dead-ends or backward-sloping (uphill) riverbed artifacts.
- **SC-003**: Terrain chunk meshes across river channels and tectonic fault lines exhibit 0 visible cracks, 0 T-junction tears, and 0 height discrepancies along all chunk boundaries.
- **SC-004**: Global tectonic and river graph generation for a 2km x 2km world macro region completes in under 500ms during initial generation on standard target hardware.
- **SC-005**: Sampling tectonic height modifiers and river carving for an individual chunk heightmap adds less than 15ms to chunk generation time on background worker threads.
- **SC-006**: Designers can adjust tectonic plate count and river density parameters in the Editor and receive updated visual feedback in Scene View within 200ms in live preview mode.

## Assumptions

- Tectonic macro-zoning and river network graphs are deterministic based on the global world seed and configuration parameters.
- The river graph is generated at a macro-scale resolution and sampled continuously with mathematical falloff during local chunk heightmap synthesis.
- Water surface rendering for rivers and lakes will seamlessly connect with the global ocean water level defined in `WaterSettings`.
- Advanced dynamic fluid simulations (e.g., real-time shallow water equations) are out of scope for v1; the system focuses on deterministic procedural terrain carving and static/spline-based hydrological channels.
