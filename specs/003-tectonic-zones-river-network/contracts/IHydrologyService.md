# Contract: IHydrologyService & IRiverMeshBuilder

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;
    using Unity.Collections;
    using UnityEngine;

    /// <summary>
    /// Thread-safe service for generating vector river networks and hydraulic terrain carving.
    /// </summary>
    public interface IHydrologyService
    {
        /// <summary>
        /// Generates a connected river graph with flow accumulation and depression routing.
        /// </summary>
        RiverGraph GenerateRiverGraph(
            HydrologySettings settings,
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            Allocator allocator);

        /// <summary>
        /// Samples hydraulic river carving displacement at world coordinates (x, z).
        /// Returns depth in world units to subtract from terrain elevation.
        /// </summary>
        float SampleRiverCarve(
            float worldX,
            float worldZ,
            in RiverGraph riverGraph,
            HydrologySettings settings);
    }

    /// <summary>
    /// Builder service for procedural river water surface ribbons and lake meshes.
    /// </summary>
    public interface IRiverMeshBuilder
    {
        /// <summary>
        /// Builds water surface meshes for river segments intersecting a specific chunk.
        /// </summary>
        RiverWaterMeshData BuildChunkRiverMesh(
            ChunkCoordinate coordinate,
            float chunkSize,
            in RiverGraph riverGraph,
            HydrologySettings settings,
            WaterSettings water);
    }
}
```
