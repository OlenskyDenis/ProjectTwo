namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Service responsible for building compound 2D heightmaps via injected ITerrainShaper.
    /// Pure C# domain service adhering strictly to SOLID (SRP, DIP) and Constitution Principle VI.
    /// </summary>
    public class HeightMapBuilder
    {
        private readonly ITerrainShaper _terrainShaper;

        public HeightMapBuilder(ITerrainShaper terrainShaper)
        {
            _terrainShaper = terrainShaper ?? throw new ArgumentNullException(nameof(terrainShaper));
        }

        public HeightMap GenerateCompoundHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            in TerrainShaperContext context)
        {
            int vertexCount = resolution + 1;
            float[,] buffer = new float[vertexCount, vertexCount];

            _terrainShaper.GenerateHeightMap(
                startX,
                startZ,
                size,
                resolution,
                in context,
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
