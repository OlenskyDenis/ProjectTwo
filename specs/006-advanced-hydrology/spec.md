# Feature Specification: Advanced Hydrology, Waterfall Dynamics & Continuous River Networks

**Feature Branch**: `006-advanced-hydrology`

**Created**: 2026-08-21

**Status**: Draft

**Input**: User description: "Створити систему неперервної гідрології з каскадами озер, водоспадами на схилах, злиттям приток та розгалуженням річок на рукави"

## Clarifications

### Session 2026-08-21
- Q: Which algorithmic approach should be used for basin depression filling and spillover point extraction for lake cascades? → A: Analytical saddle point extraction (Saddle Point Spillover) to deterministically find basin overflow saddles and spawn outflow channels.
- Q: How should river bifurcation and delta formation rules be triggered? → A: Hybrid bifurcation triggered by low gradient terrain (Slope < 5°) forming braided river valleys and coastal deltas at low elevation.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuous Mountain Headwaters & Waterfall Clamping (Priority: P1)

As an environment designer and player exploring procedural terrain, I want mountain streams to flow continuously down steep cliffs as tightly contoured waterfalls without floating in mid-air or breaking into orphan segments, so that mountain landscapes look realistic and visually cohesive.

**Why this priority**: Core visual and physical requirement. Eliminates the critical bug where disconnected flat water rectangles hover in the air on steep slopes (BUG-013, BUG-015).

**Independent Test**: Can be tested by generating mountain terrain with sources above snowlines and verifying that all headwaters flow seamlessly down slopes to the base with 100% surface conformance and zero disconnected floating mesh segments.

**Acceptance Scenarios**:

1. **Given** a river source originating on a mountain peak or snowline, **When** tracing the water path down a steep cliff (>25°), **Then** the waterfall mesh is generated with high-frequency adaptive stepping (1–2m intervals) tightly hugging the cliff geometry.
2. **Given** a mountain stream starting at high elevation, **When** the headwater begins, **Then** its initial channel width starts narrow (1.0–2.0m) and scales smoothly with flow distance.
3. **Given** any generated river node, **When** validating the river network, **Then** zero orphan or single-quad river segments exist disconnected from a valid endpoint (lake, downstream river, or ocean).

---

### User Story 2 - Lake Cascades, Basin Spillover & Long-Range Continuity (Priority: P1)

As a player traveling across vast biomes, I want water to accumulate in mountain basins to form lakes that spill over their lowest rims into cascading streams that travel long distances without getting stuck, so that water bodies feel natural and connected across chunk boundaries.

**Why this priority**: Prevents premature river termination in local terrain depressions and creates beautiful multi-tier lake chains in valleys.

**Independent Test**: Can be tested by generating multi-basin highland terrain and verifying that water pools in depressions up to the lowest rim threshold and continues flowing downstream as an overflow river.

**Acceptance Scenarios**:

1. **Given** a river flow entering an enclosed depression or mountain hollow, **When** water elevation reaches the basin rim, **Then** a lake water plane is generated and an overflow outlet is spawned at the lowest saddle point.
2. **Given** multiple small lakes formed in adjacent highland basins, **When** calculating regional drainage, **Then** upper lakes drain into lower lakes via connecting rapids/spillways, creating a continuous lake cascade.
3. **Given** a river traveling through flat plains or rolling hills, **When** encountering minor terrain ripples, **Then** hydraulic momentum and look-ahead flow inertia guide the river past local micro-obstructions toward the ocean.

---

### User Story 3 - Tributary Confluence, River Bifurcation & Deltas (Priority: P2)

As a world explorer, I want multiple mountain tributaries to merge together into wider main rivers as they flow through valleys, and large rivers to branch into braided channels or coastal deltas, so that river systems exhibit natural hydrological scaling.

**Why this priority**: Enhances visual diversity and realism across macro landscapes, transitioning from small alpine brooks to massive lowland river deltas.

**Independent Test**: Can be tested by generating multi-source terrain and verifying that channel width and carve depth increase after tributary merge junctions (Strahler order), and wide rivers split into multiple braided branches near flat coastal areas.

**Acceptance Scenarios**:

1. **Given** two or more tributaries converging at a junction, **When** merging into a shared channel, **Then** the downstream river stream order increases, doubling its flow accumulation and widening its channel proportionally.
2. **Given** a high-order river flowing through flat lowland or coastal floodplain, **When** entering low gradient terrain, **Then** the main channel can bifurcate into secondary braided branches that re-converge or empty into the ocean as a multi-channel delta.

---

### Edge Cases

- **What happens when a river source is enclosed in a caldera with no lower outlet?** The basin fills to the natural spillover saddle height; if completely enclosed with no exit, it forms a terminal endorheic lake at minimum local saddle elevation.
- **What happens when a waterfall descends a near-vertical 90° cliff?** The mesh extrusion algorithm calculates 3D surface-aligned binormals using the cliff facet normal rather than world up-vector, preventing lateral ribbon flipping or inverted triangles.
- **What happens across chunk borders when rivers cross streaming boundaries?** River nodes and spline parameters are deterministic and macro-cached globally, ensuring bit-exact alignment across independently streamed chunk meshes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST guarantee end-to-end path continuity for all generated river streams, eliminating orphan, single-segment, or abruptly truncated water geometry.
- **FR-002**: System MUST adaptively sample waterfall segments on steep slopes (slope > 25°) at 1.0–2.0m vertical intervals, conforming water mesh vertices directly to terrain surface height and normals.
- **FR-003**: System MUST calculate dynamic river channel width starting from narrow headwaters (1.0–2.0m) and expanding progressively based on flow distance and accumulated tributary volume.
- **FR-004**: System MUST implement basin pit-filling and spillover detection, allowing rivers entering local depressions to form lakes with outflow channels originating at the lowest saddle rim.
- **FR-005**: System MUST support multi-tier lake cascades where proximate highland lakes connect sequentially through spillway channels into a shared drainage trunk.
- **FR-006**: System MUST incorporate hydraulic momentum and directional look-ahead in flow routing, ensuring rivers maintain long-distance continuity through undulating terrain towards sea level.
- **FR-007**: System MUST support tributary confluence according to Strahler stream ordering, scaling channel width, depth carving, and water volume at intersection nodes.
- **FR-008**: System MUST support river bifurcation and coastal delta formation in low-gradient floodplains, allowing streams to split into multiple interconnected branches.
- **FR-009**: System MUST preserve deterministic global hydrology generation across chunk streaming boundaries without seam artifacts.
- **FR-010**: System MUST expose configurable hydrology parameters in `HydrologySettings` (waterfall step size, momentum factor, lake spillover threshold, confluence width multiplier, delta branching chance).

### Key Entities

- **RiverNode**: Point in the global river graph storing 3D position, node type (Source, Waterfall, Rapids, LakeInflow, LakeOutflow, Confluence, Bifurcation, DeltaMouth), elevation, stream order, and flow accumulation.
- **RiverSegment**: Bezier curve segment connecting two nodes with start/end widths, velocity vector, and curvature control points.
- **LakeBasin**: Water body entity defined by bounding perimeter, surface water level, center position, volume capacity, inflow connections, and spillover outflow node.
- **HydrologySettings**: Scriptable/serializable configuration holding simulation rules for source spawning, waterfall thresholds, momentum, lake cascades, and delta generation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of generated river streams terminate in a valid water body (lake basin or ocean mouth) with 0 floating orphan segments.
- **SC-002**: Water mesh distance to terrain surface on waterfalls and steep cliffs does not exceed 0.25m across all slope angles (0° to 85°).
- **SC-003**: River network generation for standard macro maps (100+ river segments, 10+ lakes) executes in less than 25ms on CPU.
- **SC-004**: Stream width expands smoothly from headwaters (1.5m ± 0.5m) to major river valleys (15m–30m) without abrupt polygon stepping.
- **SC-005**: All unit tests for waterfall clamping, lake spillover, tributary merging, and river bifurcation pass with 100% success rate.

## Assumptions

- Water surface rendering uses `WaterSimple.shader` with support for flow vectors and depth fading.
- Global river graph generation runs deterministically prior to chunk mesh construction.
- Chunk streaming coordinates reference global world-space coordinates for river slicing.
