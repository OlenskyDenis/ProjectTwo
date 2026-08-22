namespace ProjectTwo.Terrain.Core.Services
{
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Service for generating deterministic macro hydrology graphs, waterfalls, lake cascades, and river deltas.
    /// </summary>
    public interface IHydrologyService
    {
        /// <summary>
        /// Generates the complete, continuous river and lake graph from terrain elevation models.
        /// </summary>
        RiverGraph GenerateRiverGraph(
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            HydrologySettings settings,
            WaterSettings water,
            int worldSeed);

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
