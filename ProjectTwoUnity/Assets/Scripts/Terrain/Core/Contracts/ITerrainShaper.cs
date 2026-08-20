namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe mathematical service calculating compound procedural elevation and heightmaps.
    /// Incorporates noise types, macro continental masks, river carving, water basins, and elevation curves.
    /// </summary>
    public interface ITerrainShaper
    {
        /// <summary>
        /// Calculates the final composite world elevation at 2D world coordinates (x, z).
        /// </summary>
        float CalculateElevation(
            float worldX,
            float worldZ,
            NoiseSettings noise,
            MacroMaskSettings macro,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            FalloffSettings falloff);

        /// <summary>
        /// Populates a 2D float array with compound elevations for a chunk bounding box.
        /// </summary>
        void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            NoiseSettings noise,
            MacroMaskSettings macro,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            FalloffSettings falloff,
            float[,] outputBuffer);
    }
}
