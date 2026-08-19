# Research Findings: Procedural Terrain Generation via Perlin Noise

**Feature**: Procedural Terrain Generation via Perlin Noise (`001-terrain-generation`)  
**Status**: Complete  
**Date**: 2026-08-19

---

## 1. Procedural Perlin Noise & Fractal Brownian Motion (fBm)

### Decision
Implement a pure C# deterministic 2D Gradient Perlin Noise generator with multi-octave Fractal Brownian Motion (`fBm`).

### Rationale
- Standard `Mathf.PerlinNoise` in Unity is tightly coupled to Unity's main thread and produces periodic repetition artifacts at integer boundaries.
- A pure C# implementation has zero dependencies on `UnityEngine`, executes 100% thread-safely in parallel background worker tasks, and allows strict seed-based permutation table generation for deterministic reproducibility across platforms.

### Alternatives Considered
- **`Mathf.PerlinNoise` (Unity built-in)**: Rejected because it cannot run on background threads safely, lacks built-in multi-octave fBm calculation, and exhibits repeating artifacts at coordinate multiples.
- **FastNoise / External Native C++ Libs**: Rejected to avoid external native binary dependencies and platform compatibility friction, keeping code maintainable and self-contained in pure C# in accordance with Constitution Principle V.

---

## 2. Chunk Grid, Multi-LOD Triangulation & Seamless Seams

### Decision
Partition the infinite landscape into fixed-size grid chunks of $240 \times 240$ units (or $120 \times 120$ vertices per chunk) with integer LOD reduction steps ($LOD \in \{0, 1, 2, 3\}$ with step increments $1, 2, 4, 6$). Calculate shared edge normals or vertex margins to eliminate seam cracking between differing LOD chunks.

### Rationale
- $240$ is highly composite (evenly divisible by 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24), allowing clean vertex skipping without remainder fractions at chunk edges.
- Multi-LOD drastically reduces active vertex count for distant landscape chunks (reducing draw call vertex overhead by up to $80\%$).
- Pre-computing edge normals across chunk borders or sampling heightmaps with a 1-vertex margin ensures seamless lighting and continuous geometry across chunk boundaries.

### Alternatives Considered
- **Single monolithic mesh**: Rejected because it cannot support infinite streaming, causes huge memory spikes, and fails latency budgets.
- **Continuous dynamic mesh tessellation (GPU shaders)**: Rejected as overly complex for v1, making physics collision matching difficult; CPU mesh LODs with `MeshCollider` for LOD 0 provide superior simplicity and testability.

---

## 3. Asynchronous Threading Model & Object Pooling

### Decision
Execute noise sampling and mesh data construction on background worker threads using `System.Threading.Tasks.Task`, delivering constructed `TerrainMeshData` structs to the main thread via a thread-safe staging queue. Main thread applies vertex/index arrays to Unity `Mesh` instances during `Update` with a budget threshold (< 2ms/frame). Recycle chunk GameObjects using an Object Pool.

### Rationale
- Unity's `Mesh` API requires main-thread instantiation and buffer assignment, but $95\%$ of computation time is spent on noise sampling, elevation normalization, and vertex math.
- Offloading math to background threads keeps frame rate locked at 60+ FPS without hitching.
- Object pooling recycles `GameObject`, `MeshFilter`, `MeshRenderer`, and `MeshCollider` components when chunks exit view distance, preventing Garbage Collector pressure and memory fragmentation.

### Alternatives Considered
- **Unity Job System + Burst**: Viable, but introduces `Unity.Collections` and NativeArray dependencies into the domain layer. A pure C# async Task model provides complete domain decoupling and testability without engine runtime overhead.

---

## 4. ScriptableObject Presets & Ergonomic Editor Inspector

### Decision
Decouple all terrain parameters, noise settings, LOD distance tiers, and biome region lists into a `TerrainDataConfig` `ScriptableObject`. Provide a custom Unity Editor Inspector (`TerrainEditor`) with an on-demand "Generate" button, an "Auto Update" live-preview toggle, and reorderable biome lists with `[Tooltip]` and `[Range]` attributes.

### Rationale
- Enables designers to create, duplicate, and swap different world presets (e.g., `DesertPreset.asset`, `AlpsPreset.asset`) with zero code modifications.
- Fulfills user requirements for instant editing, visual color selection, and easy addition/removal of biome layers.

---

## 5. Spatial Queries (`ITerrainProvider`) & Bilinear Interpolation

### Decision
Expose an `ITerrainProvider` interface with $O(1)$ spatial queries (`GetHeight`, `GetNormal`, `GetSlope`, `GetBiomeAt`) using a spatial hash grid of active chunk heightmaps combined with bilinear interpolation across adjacent height samples.

### Rationale
- Downstream systems (vegetation spawners, house/road/bridge placers) can sample continuous world coordinates $(X, Z)$ and receive exact, smooth elevation and slope vectors without raycasting physics meshes.
- Bilinear interpolation avoids stair-stepping artifacts when placing props between discrete mesh vertices.
- `OnTerrainGenerated` and `OnChunkLoaded` lifecycle events decouple the terrain pipeline from downstream systems (SOLID DIP).

---

## 6. Chunk Persistence & Caching (`IChunkStorage`)

### Decision
Define an `IChunkStorage` interface with a memory-cached storage provider (`MemoryChunkStorage`) and local binary/JSON file storage provider (`FileChunkStorage`).

### Rationale
- Visited chunks and modified terrain data are cached by chunk coordinate key `(chunkX, chunkZ)`.
- Returning to previously generated areas loads instantly from cache (< 10ms) without redundant noise recalculation.
- Clean interface abstraction allows swapping storage backends (e.g., SQLite, cloud sync) in future phases without altering terrain streaming logic.
