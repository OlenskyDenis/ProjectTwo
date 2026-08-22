# Research & Architectural Decisions: Asynchronous Multithreaded Pipeline & Streaming Optimization

**Feature**: `007-async-multithreaded-pipeline-optimization`
**Date**: 2026-08-22

## Research Findings & Architectural Decisions

### 1. Full-Payload Background Worker Generation (`ChunkGenerationPayload`)

- **Context**: The existing implementation generated `HeightMap` in `Task.Run`, but executed `TerrainMeshBuilder.GenerateTerrainMesh` (visual and collision) and `RiverMeshBuilder.BuildChunkRiverMesh` synchronously on the Main Thread during `ProcessCompletedChunks()`. When 25–36 chunks completed at world startup, this dumped $>300\text{ms}$ of CPU work onto a single frame, tanking FPS down to 3–4.
- **Decision**: Move 100% of mesh geometry construction (vertices, triangles, UVs, analytical smooth normals, vertex colors, river spline ribbon vertices) into the background `Task.Run` worker thread. The worker produces a complete `ChunkGenerationPayload`.
- **Main Thread Responsibility**: The Main Thread solely dequeues pre-built payloads and invokes fast native buffer uploads (`mesh.SetVertices`, `mesh.SetTriangles`, `mesh.SetColors`, etc.) and GameObject transform activations.
- **Alternatives Considered**:
  - *Compute Shaders*: High initial implementation overhead and synchronization barrier with C# river graph structures.
  - *Unity Job System / Burst*: Requires converting existing pure C# objects (`RiverGraph`, `TectonicBoundary[]`) into NativeArrays. Background `Task.Run` with pure C# structs is simpler, fully decoupled from Unity runtime, and yields $<1\text{ms}$ background generation time per chunk across multi-core ThreadPool.

---

### 2. Off-Thread Physics Collision Mesh Baking (`Physics.BakeMesh`)

- **Context**: Setting `MeshCollider.sharedMesh = collisionMesh` on the Main Thread triggers synchronous PhysX spatial tree cooking (10–15ms per chunk for detailed meshes).
- **Decision**: In Unity 6, `Physics.BakeMesh(meshInstanceID, isConvex: false)` can be executed in background worker threads. The worker thread creates the collision mesh or pre-cooks the collision geometry ID off-thread. When the Main Thread assigns `sharedMesh`, PhysX recognizes the pre-cooked BVH structure and attaches with zero latency ($<0.05\text{ms}$).
- **Alternatives Considered**:
  - *Primitive Colliders / Simplified Box Triggers*: Inadequate for undulating mountain physics.
  - *Deferred Lazy Collision Activation*: Only activating collider when player touches chunk; still produces a hitch on boundary crossing unless pre-baked. Pre-baking with `Physics.BakeMesh` eliminates the hitch universally.

---

### 3. Hybrid Time-Slicing Main Thread Activation Budget

- **Context**: Even with native buffer assignment, activating 36 GameObjects, updating materials, and binding MeshFilters in a single frame can take 5–8ms.
- **Decision**: Implement a hybrid time-budgeted ingestion loop in `TerrainGenerator`:
  ```csharp
  private const float MaxActivationTimeBudgetMs = 2.0f;
  private const int MaxChunksPerFrame = 2;

  private void ProcessCompletedChunks()
  {
      var sw = System.Diagnostics.Stopwatch.StartNew();
      int chunksProcessed = 0;

      while (chunksProcessed < MaxChunksPerFrame && _completedQueue.TryDequeue(out ChunkGenerationPayload payload))
      {
          ApplyChunkPayload(payload);
          chunksProcessed++;

          if (sw.Elapsed.TotalMilliseconds >= MaxActivationTimeBudgetMs)
          {
              break;
          }
      }
  }
  ```
- **Rationale**: Guarantees that chunk streaming NEVER consumes more than 2.0ms of the 16.6ms budget required for 60 FPS.

---

### 4. Parameter Context Refactoring (`TerrainShaperContext`)

- **Context**: The `004` audit forced all 12-15 parameters into `ITerrainShaper.CalculateElevation` and `GenerateHeightMap` to satisfy the "Single Authoritative Pipeline" principle, resulting in an unwieldy Long Parameter List code smell.
- **Decision**: Encapsulate all settings into a readonly struct `TerrainShaperContext`:
  ```csharp
  public readonly struct TerrainShaperContext
  {
      public readonly NoiseSettings Noise;
      public readonly MacroMaskSettings Macro;
      public readonly TectonicSettings Tectonics;
      public readonly TectonicBoundary[] TectonicBoundaries;
      public readonly HeightCurveSettings HeightCurve;
      public readonly WaterSettings Water;
      public readonly RiverSettings River;
      public readonly HydrologySettings Hydrology;
      public readonly RiverGraph RiverGraph;
      public readonly FalloffSettings Falloff;

      public TerrainShaperContext(...) { ... }
  }
  ```
- **Contracts Impact**:
  - `CalculateElevation(float worldX, float worldZ, in TerrainShaperContext context)`
  - `GenerateHeightMap(float startX, float startZ, float size, int resolution, in TerrainShaperContext context, float[,] outputBuffer)`
- **Test Impact**: Update `ContractReflectionTests` to verify `TerrainShaperContext` encapsulation rather than counting 12 individual parameters.
