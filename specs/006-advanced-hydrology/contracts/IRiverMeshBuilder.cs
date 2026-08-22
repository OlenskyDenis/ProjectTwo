namespace ProjectTwo.Terrain.Core.Services
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Service for extruding smooth, cliff-conforming water meshes from river segments within local chunk bounds.
    /// </summary>
    public interface IRiverMeshBuilder
    {
        /// <summary>
        /// Generates renderable water mesh geometry for river segments intersecting a specific chunk.
        /// </summary>
        RiverWaterMeshData BuildChunkRiverMesh(
            RiverGraph riverGraph,
            Vector3 chunkOrigin,
            float chunkSize,
            ITerrainShaper terrainProvider = null);
    }
}
