namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

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
    }
}
