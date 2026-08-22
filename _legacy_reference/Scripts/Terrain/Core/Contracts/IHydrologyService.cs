namespace ProjectTwo.Terrain.Core.Contracts
{
    using System.Collections.Generic;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe service for generating deterministic macro hydrology graphs, waterfalls, lake cascades, and river deltas.
    /// </summary>
    public interface IHydrologyService
    {
        /// <summary>
        /// Generates a connected river graph with flow accumulation, cliff-conforming waterfalls, lake cascades, and deltas.
        /// </summary>
        RiverGraph GenerateRiverGraph(
            HydrologySettings settings,
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water);

        /// <summary>
        /// Samples hydraulic river carving displacement at world coordinates (x, z).
        /// Returns depth in world units to subtract from terrain elevation.
        /// </summary>
        float SampleRiverCarve(
            float worldX,
            float worldZ,
            RiverGraph riverGraph,
            HydrologySettings settings);

        /// <summary>
        /// Identifies enclosed basins and extracts saddle spillover points for cascading lakes.
        /// </summary>
        IReadOnlyList<LakeBasin> ExtractLakeBasins(
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            float searchRadius);

        /// <summary>
        /// Calculates the adaptive waterfall sampling step based on terrain slope angle.
        /// </summary>
        float GetAdaptiveStepSize(float slopeAngleDegrees, float baseStepSize);
    }
}
