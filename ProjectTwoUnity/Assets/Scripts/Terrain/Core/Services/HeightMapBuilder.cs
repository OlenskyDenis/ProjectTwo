namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Service responsible for building 2D heightmaps using an injected ITerrainShaper or INoiseGenerator.
    /// Pure C# domain service.
    /// </summary>
    public class HeightMapBuilder
    {
        private readonly ITerrainShaper _terrainShaper;
        private readonly INoiseGenerator _noiseGenerator;

        public HeightMapBuilder(ITerrainShaper terrainShaper)
        {
            _terrainShaper = terrainShaper ?? throw new ArgumentNullException(nameof(terrainShaper));
            _noiseGenerator = new PerlinNoiseGenerator();
        }

        public HeightMapBuilder(INoiseGenerator noiseGenerator)
        {
            _noiseGenerator = noiseGenerator ?? throw new ArgumentNullException(nameof(noiseGenerator));
            _terrainShaper = new ProceduralTerrainShaper();
        }

        public HeightMap GenerateHeightMap(int mapWidth, int mapHeight, NoiseSettings settings, ChunkCoordinate chunkCoord)
        {
            return _noiseGenerator.GenerateHeightMap(mapWidth, mapHeight, settings, chunkCoord);
        }

        public HeightMap GenerateCompoundHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            NoiseSettings noise,
            MacroMaskSettings macro,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            FalloffSettings falloff)
        {
            int vertexCount = resolution + 1;
            float[,] buffer = new float[vertexCount, vertexCount];

            _terrainShaper.GenerateHeightMap(
                startX,
                startZ,
                size,
                resolution,
                noise,
                macro,
                heightCurve,
                water,
                river,
                falloff,
                buffer);

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int y = 0; y < vertexCount; y++)
            {
                for (int x = 0; x < vertexCount; x++)
                {
                    float val = buffer[x, y];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
            }

            return new HeightMap(buffer, min, max);
        }
    }
}
