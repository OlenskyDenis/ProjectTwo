# Phase 1 Data Model: Macro-Tectonic Zoning & Hydrological River Graph System

**Feature**: `003-tectonic-zones-river-network`
**Date**: 2026-08-20

---

## 1. Tectonic Entities

### `TectonicPlate` (Struct / Pure Data Model)
Represents an individual macro crustal plate.
- `int Id`: Unique plate identifier.
- `Vector2 Centroid`: World-space coordinate center of the plate.
- `Vector2 DriftVelocity`: Direction and speed of plate motion ($\text{m/s}$ or relative scale).
- `PlateCrustType CrustType`: Enum (`Continental`, `Oceanic`).
- `float BaseElevation`: Baseline elevation offset for this crust type.

### `TectonicBoundary` (Struct / Pure Data Model)
Represents a structural fault/contact edge between two plates.
- `int PlateAId`: First adjacent plate.
- `int PlateBId`: Second adjacent plate.
- `Vector2 StartPoint`: Line segment start in world coordinates.
- `Vector2 EndPoint`: Line segment end in world coordinates.
- `TectonicBoundaryType BoundaryType`: Enum (`Convergent`, `Divergent`, `Transform`).
- `float CollisionIntensity`: Relative convergence or shear magnitude.
- `float InfluenceRadius`: Width of the orogenic mountain belt or rift zone.
- `float MaxUplift`: Peak elevation gain along the ridge crest.

### `TectonicSettings` (Serializable Value Object / Sub-Settings)
Configuration model embedded in `TerrainDataConfig`.
- `bool Enabled`: Toggle tectonic macro-shaping.
- `int Seed`: Randomization seed for plate distributions.
- `int PlateCount`: Number of tectonic cells in macro area (e.g., 8 to 64).
- `float PlateScale`: Spatial scale of tectonic plates (e.g., 500m to 5000m).
- `float MountainUplift`: Maximum height multiplier for convergent mountain ridges.
- `float RiftDepth`: Maximum depression depth for divergent boundaries.
- `float BoundaryInfluenceWidth`: Width of transition zone from fault to plains.
- `float RidgeSharpness`: Power curve exponent for knife-edge mountain crests.
- `float FaultNoiseWarp`: Amplitude of domain warping along fault lines.

---

## 2. Hydrological River Graph Entities

### `RiverNode` (Struct / Pure Data Model)
A point in the hydrological network graph.
- `int Id`: Unique node identifier.
- `Vector3 Position`: World coordinates $(x, y, z)$ of the node.
- `RiverNodeType NodeType`: Enum (`Source`, `Confluence`, `LakeInlet`, `LakeOutlet`, `OceanMouth`).
- `float Elevation`: Terrain surface elevation at this point.
- `float FlowAccumulation`: Total upstream catchment area contributing discharge.
- `int StreamOrder`: Strahler stream order rank ($1 \dots N$).

### `RiverSegment` (Struct / Pure Data Model)
A continuous river channel edge connecting two nodes.
- `int Id`: Segment identifier.
- `int StartNodeId`: Upstream node index.
- `int EndNodeId`: Downstream node index.
- `Vector3 ControlPoint`: Intermediate Bézier tangent control point for curved flow.
- `float Length`: 3D arc length of the segment.
- `float ChannelWidth`: World-unit width of the carved riverbed.
- `float CarveDepth`: Maximum incision depth into terrain heightmap.
- `int StreamOrder`: Strahler stream order.
- `float AverageSlope`: Hydraulic gradient $\Delta h / L$.

### `LakeBasin` (Struct / Pure Data Model)
Represents an enclosed depression basin holding a lake.
- `int Id`: Unique lake identifier.
- `Vector3 Center`: Approximate center coordinate of the lake.
- `float WaterElevation`: Surface spillover elevation.
- `float Area`: Basin surface area.
- `int OutletNodeId`: River node where water spills over downstream.

### `RiverGraph` (Domain Aggregate Service / Data Container)
The container for the connected hydrological network.
- `NativeArray<RiverNode> Nodes`: Contiguous array of nodes for Burst jobs.
- `NativeArray<RiverSegment> Segments`: Contiguous array of river segments.
- `NativeParallelMultiHashMap<int, int> SpatialGrid`: Spatial Hash Grid mapping 2D cell hashes to active segment indices.
- `NativeArray<LakeBasin> Lakes`: List of generated lake basins.

### `HydrologySettings` (Serializable Value Object / Sub-Settings)
Configuration model embedded in `TerrainDataConfig`.
- `bool Enabled`: Toggle hydrological river network & carving.
- `int Seed`: Randomization seed for river network routing.
- `int SourceCount`: Target number of river springs spawned in mountains.
- `float MinSourceElevationRatio`: Minimum relative elevation threshold for river sources.
- `float BaseRiverWidth`: Baseline width of first-order mountain brooks.
- `float WidthGrowthRate`: Width multiplier per Strahler stream order.
- `float BaseCarveDepth`: Incision depth of primary channels.
- `float BankSmoothness`: Softness of riverbank transition slopes.
- `float MeanderIntensity`: Frequency and amplitude of lateral river curves.
- `float LakeMinDepthThreshold`: Minimum depression depth required to instantiate a lake basin.

---

## 3. Presentation & Mesh Model

### `RiverWaterMeshData` (Pure Data Structure)
Contains procedural geometry for river surface ribbons.
- `Vector3[] Vertices`: Left and right bank vertices along the river spline.
- `Vector3[] Normals`: Upward-facing surface normals.
- `Vector2[] UVs`: $U \in [0, 1]$ across width, $V$ mapped to downstream distance.
- `int[] Triangles`: Index buffer for triangle strip.
- `Color32[] Colors`: Flow direction and foam mask encoded in vertex colors.
