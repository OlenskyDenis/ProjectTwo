namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe mathematical service calculating compound procedural elevation and heightmaps.
    /// Incorporates noise types, macro continental masks, tectonics, river carving, water basins, and elevation curves.
    /// Strictly adheres to Constitution Principle I &amp; VI (Clean Parameter Encapsulation &amp; Single Authoritative Pipeline).
    /// </summary>
    public interface ITerrainShaper
    {
        /// <summary>
        /// Calculates the final composite world elevation at (worldX, worldZ) incorporating global tectonics and river network graph.
        /// </summary>
        /// <param name="worldX">World space X coordinate in meters.</param>
        /// <param name="worldZ">World space Z coordinate in meters.</param>
        /// <param name="context">Encapsulated domain parameter context.</param>
        /// <returns>World elevation in meters.</returns>
        float CalculateElevation(
            float worldX,
            float worldZ,
            in TerrainShaperContext context);

        /// <summary>
        /// Populates a 2D float array with compound normalized elevations [0..1] incorporating global tectonics and river network graph.
        /// </summary>
        /// <param name="startX">World space origin X in meters.</param>
        /// <param name="startZ">World space origin Z in meters.</param>
        /// <param name="size">Spatial width/depth of the chunk in meters.</param>
        /// <param name="resolution">Number of quad segments along one dimension.</param>
        /// <param name="context">Encapsulated domain parameter context.</param>
        /// <param name="outputBuffer">Pre-allocated 2D array [res+1, res+1] for normalized elevations.</param>
        void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            in TerrainShaperContext context,
            float[,] outputBuffer);
    }
}
