namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Service responsible for building 2D heightmaps using an injected INoiseGenerator.
    /// Pure C# domain service.
    /// </summary>
    public class HeightMapBuilder
    {
        private readonly INoiseGenerator _noiseGenerator;

        public HeightMapBuilder(INoiseGenerator noiseGenerator)
        {
            _noiseGenerator = noiseGenerator ?? throw new ArgumentNullException(nameof(noiseGenerator));
        }

        public HeightMap GenerateHeightMap(int mapWidth, int mapHeight, NoiseSettings settings, ChunkCoordinate chunkCoord)
        {
            return _noiseGenerator.GenerateHeightMap(mapWidth, mapHeight, settings, chunkCoord);
        }
    }
}
