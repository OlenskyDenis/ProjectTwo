namespace ProjectTwo.Terrain.Core.Contracts
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Builder service for procedural river water surface ribbons, cliff-conforming waterfalls, and lake meshes.
    /// </summary>
    public interface IRiverMeshBuilder
    {
        /// <summary>
        /// Builds water surface meshes for river segments intersecting a specific chunk, tightly conforming to terrain geometry.
        /// </summary>
        RiverWaterMeshData BuildChunkRiverMesh(
            ChunkCoordinate coordinate,
            float chunkSize,
            RiverGraph riverGraph,
            HydrologySettings settings,
            WaterSettings water,
            ITerrainShaper terrainProvider,
            in TerrainShaperContext context);
    }
}
