namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

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
            RiverGraph riverGraph,
            HydrologySettings settings,
            WaterSettings water);
    }
}
