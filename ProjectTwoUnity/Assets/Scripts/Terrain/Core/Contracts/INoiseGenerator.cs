namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# interface for procedural noise sampling algorithms.
    /// </summary>
    public interface INoiseGenerator
    {
        /// <summary>
        /// Generates a complete 2D heightmap matrix for a chunk coordinate based on provided noise settings.
        /// </summary>
        HeightMap GenerateHeightMap(int mapWidth, int mapHeight, NoiseSettings settings, ChunkCoordinate chunkCoord);

        /// <summary>
        /// Samples single-point 2D noise value at specific world coordinates.
        /// </summary>
        float SampleNoise(float x, float y, NoiseSettings settings);
    }
}
