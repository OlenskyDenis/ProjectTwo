namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System;
    using NUnit.Framework;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class HeightMapBuilderTests
    {
        private HeightMapBuilder _builder;
        private ITerrainShaper _shaper;

        [SetUp]
        public void SetUp()
        {
            _shaper = new ProceduralTerrainShaper();
            _builder = new HeightMapBuilder(_shaper);
        }

        [Test]
        public void Constructor_ThrowsOnNullShaper()
        {
            Assert.Throws<ArgumentNullException>(() => new HeightMapBuilder(null));
        }

        [Test]
        public void GenerateCompoundHeightMap_ProducesCorrectDimensions()
        {
            int resolution = 32;
            float size = 240f;
            var ctx = TerrainShaperContext.CreateDefault();

            HeightMap map = _builder.GenerateCompoundHeightMap(
                0f,
                0f,
                size,
                resolution,
                in ctx);

            Assert.IsNotNull(map);
            Assert.AreEqual(resolution + 1, map.Width);
            Assert.AreEqual(resolution + 1, map.Height);
            Assert.IsTrue(map.MinValue <= map.MaxValue);
        }

        [Test]
        public void GenerateCompoundHeightMap_IsDeterministic_AcrossMultipleCalls()
        {
            int resolution = 16;
            float size = 240f;
            NoiseSettings noise = NoiseSettings.Default;
            noise.Seed = 777;

            var ctx = new TerrainShaperContext(
                noise,
                MacroMaskSettings.Default,
                TectonicSettings.Default,
                null,
                HeightCurveSettings.Default,
                WaterSettings.Default,
                RiverSettings.Default,
                HydrologySettings.Default,
                RiverGraph.Empty,
                FalloffSettings.Default);

            HeightMap map1 = _builder.GenerateCompoundHeightMap(
                100f,
                200f,
                size,
                resolution,
                in ctx);

            HeightMap map2 = _builder.GenerateCompoundHeightMap(
                100f,
                200f,
                size,
                resolution,
                in ctx);

            for (int x = 0; x < resolution + 1; x++)
            {
                for (int y = 0; y < resolution + 1; y++)
                {
                    Assert.AreEqual(map1.Values[x, y], map2.Values[x, y], 1e-6f, $"Height values at [{x},{y}] must match across deterministic runs.");
                }
            }
        }

        [Test]
        public void InterpolateValue_ReturnsSmoothValueInsideBounds()
        {
            int resolution = 12;
            float size = 240f;
            var ctx = TerrainShaperContext.CreateDefault();

            HeightMap map = _builder.GenerateCompoundHeightMap(
                0f,
                0f,
                size,
                resolution,
                in ctx);

            float interpolated = map.InterpolateValue(0.5f, 0.5f);
            Assert.IsFalse(float.IsNaN(interpolated));
            Assert.GreaterOrEqual(interpolated, map.MinValue);
            Assert.LessOrEqual(interpolated, map.MaxValue);
        }
    }
}
