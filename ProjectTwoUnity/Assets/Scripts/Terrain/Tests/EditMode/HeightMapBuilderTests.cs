namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class HeightMapBuilderTests
    {
        private HeightMapBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new HeightMapBuilder(new PerlinNoiseGenerator());
        }

        [Test]
        public void GenerateHeightMap_ProducesCorrectDimensions()
        {
            int width = 32;
            int height = 32;
            NoiseSettings settings = NoiseSettings.Default;
            ChunkCoordinate coord = new ChunkCoordinate(0, 0);

            HeightMap map = _builder.GenerateHeightMap(width, height, settings, coord);

            Assert.IsNotNull(map);
            Assert.AreEqual(width, map.Width);
            Assert.AreEqual(height, map.Height);
        }

        [Test]
        public void GenerateHeightMap_IsDeterministic_AcrossMultipleCalls()
        {
            int width = 16;
            int height = 16;
            NoiseSettings settings = NoiseSettings.Default;
            settings.Seed = 777;
            ChunkCoordinate coord = new ChunkCoordinate(2, 3);

            HeightMap map1 = _builder.GenerateHeightMap(width, height, settings, coord);
            HeightMap map2 = _builder.GenerateHeightMap(width, height, settings, coord);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Assert.AreEqual(map1.Values[x, y], map2.Values[x, y], 1e-6f, $"Height values at [{x},{y}] must match across deterministic runs.");
                }
            }
        }

        [Test]
        public void InterpolateValue_ReturnsSmoothValueInsideBounds()
        {
            int width = 10;
            int height = 10;
            NoiseSettings settings = NoiseSettings.Default;
            ChunkCoordinate coord = new ChunkCoordinate(0, 0);

            HeightMap map = _builder.GenerateHeightMap(width, height, settings, coord);

            float interpolated = map.InterpolateValue(0.5f, 0.5f);
            Assert.GreaterOrEqual(interpolated, 0f);
            Assert.LessOrEqual(interpolated, 1f);
        }
    }
}
