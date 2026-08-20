# Phase 0 Research: Macro-Tectonic Zoning & Hydrological River Graph System

**Feature**: `003-tectonic-zones-river-network`
**Date**: 2026-08-20
**Target**: Unity 2022+ / C# Job System & Burst / Clean Architecture

---

## 1. Tectonic Plate Partitioning & Boundary Morphometry

### Decision: Jittered Voronoi/Delaunay Cells with Drift Velocity Vectors
- **Selected Mechanism**: Deterministic cell partitioning using jittered 2D grid Voronoi seeds. Each plate $P_i$ has a centroid $C_i$, a drift vector $\vec{v}_i = (\cos \theta_i, \sin \theta_i) \cdot s_i$, and crust type (continental vs. oceanic).
- **Boundary Classification**:
  - For two adjacent plates $P_a$ and $P_b$ sharing edge $E_{ab}$ with normal $\vec{n}_{ab}$ (pointing from $a$ to $b$):
    - Relative motion: $\vec{v}_{\text{rel}} = \vec{v}_a - \vec{v}_b$
    - Normal convergence rate: $c_n = \vec{v}_{\text{rel}} \cdot \vec{n}_{ab}$
    - Tangential shear rate: $c_t = \|\vec{v}_{\text{rel}} \times \vec{n}_{ab}\|$
  - Classification:
    - **Convergent (Collision / Subduction)**: $c_n > \epsilon_{\text{conv}}$ (creates massive mountain chains and orogenic belts).
    - **Divergent (Rift / Spreading)**: $c_n < -\epsilon_{\text{div}}$ (creates sunken rift valleys and oceanic spreading ridges).
    - **Transform (Strike-Slip)**: $|c_n| \le \epsilon$ and $c_t > \epsilon_{\text{trans}}$ (creates lateral shear ridges and fault valleys).
- **Uplift Profile Formula**:
  - Distance field $d(x, z)$ to nearest boundary line segment.
  - Profile curve: $U(d) = H_{\text{max}} \cdot \exp\left(-\left(\frac{d}{\sigma}\right)^2\right) \cdot \text{NoiseWarp}(x, z)$ for sharp mountain crests.
- **Alternatives Considered**:
  - *Full physical lithosphere simulation*: Rejected due to high computational overhead unsuitable for interactive editor previews and runtime generation.
  - *Static bitmap tectonics*: Rejected due to fixed resolution, memory footprint, and inability to support infinite worlds.

---

## 2. Hydrological River Graph Routing & Flow Accumulation

### Decision: Gradient-Based Steepest Descent with Strahler Stream Ordering
- **Selected Mechanism**:
  - River source nodes spawn in high-elevation catchments (where tectonic uplift + mountain noise $> H_{\text{source}}$).
  - Downstream path tracing: At node position $\vec{p}_k$, the next step moves along steepest descent gradient $-\nabla h(\vec{p}_k)$ with step size $\Delta s$, blended with subtle meandering harmonic noise perpendicular to flow.
  - Confluences: When two river paths approach within distance $R_{\text{merge}}$, they merge into a single child node.
  - Flow Accumulation & Stream Ordering:
    - Strahler stream order $S$: Leaf streams have $S=1$. When two streams of order $i$ meet, child stream has order $i+1$; if different orders meet, child takes $\max(S_1, S_2)$.
    - Riverbed width $W(S) = W_0 \cdot S^\alpha$ and carve depth $D(S) = D_0 \cdot \sqrt{S}$.
- **Alternatives Considered**:
  - *Naive per-pixel hydraulic particle simulation*: Particle erosion across full 3D chunk grids is too slow for real-time chunk streaming.
  - *Straight Voronoi river edges*: Unnatural geometric angularity lacking organic river meandering.

---

## 3. Depression Resolution (Pits / Sinks) & Lake Basins

### Decision: Priority-Flood Saddle Breaching & Spillover Lake Basins
- **Selected Mechanism**:
  - When steepest descent encounters a local minimum $h(\vec{p}) \le h(\vec{p}_{\text{neighbors}})$:
    - **Shallow depression (barrier height $< H_{\text{lake\_threshold}}$)**: Path traces the lowest saddle point on the depression perimeter and carves an incision channel through the barrier (Breach Carving).
    - **Deep depression (barrier height $\ge H_{\text{lake\_threshold}}$)**: Forms a `LakeBasin`. The water surface rises to the spillover elevation $h_{\text{spill}}$. An outlet river node is instantiated at the spillover point, continuing downstream flow to ocean level.
- **Alternatives Considered**:
  - *Blind depression filling*: Completely flattens terrain, destroying crater/valley geometry.
  - *Dead-end termination*: Leaves unfinished rivers floating high in mountains.

---

## 4. Spatial Indexing & Chunk Streaming Architecture

### Decision: Spatial Hash Grid with NativeArray Job System Integration
- **Selected Mechanism**:
  - World hydrology is divided into Macro-Watershed Sectors ($1024 \times 1024$ m).
  - River segments are registered in a 2D Spatial Hash Grid (cell size $= 128$ m).
  - When an individual chunk ($32 \times 32$ or $64 \times 64$ m) is sampled in worker threads:
    - Queries only the $2 \times 2$ or $3 \times 3$ relevant hash cells.
    - Samples distance to quadratic Bézier spline segments analytically.
    - Zero GC allocations during heightmap generation loop.
- **Alternatives Considered**:
  - *Global KD-Tree / BVH with class pointers*: Incompatible with Burst compiler and causes GC memory fragmentation during streaming.

---

## 5. Procedural River Water Surface Mesh Generation

### Decision: Sloped River Ribbons with Flow UVs
- **Selected Mechanism**:
  - Procedural triangle strip extruded along river spline centerlines.
  - Water surface elevation set to riverbed base + water depth.
  - UV coordinates: $U \in [0, 1]$ across river width, $V$ mapped to cumulative spline distance (for continuous scrolling water shader).
  - Seamless junction at $h \le \text{SeaLevel}$ with ocean plane.
