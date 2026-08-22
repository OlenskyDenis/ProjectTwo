namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe service for evaluating global tectonic macro-plates and boundary uplift.
    /// </summary>
    public interface ITectonicService
    {
        /// <summary>
        /// Generates tectonic plates and structural boundary segments for a given configuration.
        /// </summary>
        void GenerateTectonicPartition(
            TectonicSettings settings,
            out TectonicPlate[] plates,
            out TectonicBoundary[] boundaries);

        /// <summary>
        /// Calculates the compound tectonic elevation modifier (uplift or rift depression) at world coordinates (x, z).
        /// </summary>
        float SampleTectonicUplift(
            float worldX,
            float worldZ,
            TectonicSettings settings,
            TectonicBoundary[] boundaries);
    }
}
