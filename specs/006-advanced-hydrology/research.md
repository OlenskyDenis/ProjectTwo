# Research: Advanced Hydrology, Waterfall Dynamics & Continuous River Networks

## Technical Context & Decisions

### Decision 1: Waterfall Surface Conformance & Adaptive Clamping
- **Context**: Steep slopes (>25°) currently cause linear river segment chords to detach from the slope and float horizontally in mid-air (BUG-013, BUG-015).
- **Decision**: Implement adaptive vertical sub-stepping in `RiverMeshBuilder` and `HydrologyService`:
  - When slope $> 25^\circ$, subdivision interval reduces to $1.0\text{m} - 2.0\text{m}$.
  - Compute surface-aligned 3D orthogonal coordinate frame using the terrain facet normal: $\vec{b} = \text{normalize}(\vec{t} \times \vec{n}_{\text{terrain}})$, $\vec{n}_{\text{mesh}} = \text{normalize}(\vec{b} \times \vec{t})$.
  - Clamp water vertex elevation to terrain surface with fixed vertical offset $+0.15\text{m}$.
- **Rationale**: Ensures the waterfall ribbon tightly hugs arbitrary cliffs without polygon penetration or floating air gaps.
- **Alternatives Considered**:
  - *Fixed 25m steps*: Causes 50m air overhangs.
  - *Full physics particle simulation*: Too computationally expensive for real-time procedural generation.

### Decision 2: Basin Depression Filling & Saddle Point Spillover (Lake Cascades)
- **Context**: Flow routing gets trapped in micro-depressions, leaving orphan truncated rivers.
- **Decision**: Implement analytical Depression Saddle Point Extraction (Priority-Flood / Saddle Scan):
  - Identify basin perimeter and evaluate lowest rim height ($Z_{\text{saddle}}$).
  - Fill enclosed basin up to $Z_{\text{saddle}}$ generating a `LakeBasin` water polygon.
  - Spawn an overflow outflow stream at the saddle point and continue flow routing downstream.
- **Rationale**: Eliminates dead-ends, allows multi-tier cascading lakes in alpine valleys, and maintains 100% path continuity.
- **Alternatives Considered**:
  - *Random ocean raycast*: Ignores mountain terrain barriers.
  - *Arbitrary sink holes*: Visually unnatural.

### Decision 3: Hydraulic Momentum & Long-Range Continuity
- **Context**: Minor noise perturbations can deflect flow into artificial loops or early termination.
- **Decision**: Inertial velocity blending: $\vec{v}_{\text{next}} = \text{normalize}(\alpha \cdot \vec{v}_{\text{prev}} + (1 - \alpha) \cdot \vec{d}_{\text{steepest}})$, with $\alpha = 0.45$.
- **Rationale**: Allows rivers to flow across undulating plains and maintain macro direction towards the coast.

### Decision 4: Confluence (Strahler Order) & Bifurcation (Delta Branches)
- **Context**: Natural river networks widen when tributaries join and split across coastal floodplains.
- **Decision**:
  - **Confluence**: When stream $A$ (order $i$) meets stream $B$ (order $j$), downstream order is $\max(i, j) + 1$ if $i = j$, else $\max(i, j)$. Channel width scales as $W = W_0 \cdot \text{Order}^{1.35}$.
  - **Bifurcation**: In flat lowlands ($\text{Slope} < 5^\circ$, $\text{Elevation} < 15\text{m}$), allow stochastic split into 2 branches with shared water budget.
- **Rationale**: Produces realistic dendritic river basins and coastal deltas.
