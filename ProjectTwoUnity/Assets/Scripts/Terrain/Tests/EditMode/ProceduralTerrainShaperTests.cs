namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class ProceduralTerrainShaperTests
    {
        private ProceduralTerrainShaper _shaper;

        [SetUp]
        public void SetUp()
        {
            _shaper = new ProceduralTerrainShaper();
        }

        [Test]
        public void CalculateElevation_DeterministicForSameCoordinates()
        {
            var ctx = TerrainShaperContext.CreateDefault();

            float h1 = _shaper.CalculateElevation(123.45f, 678.90f, in ctx);
            float h2 = _shaper.CalculateElevation(123.45f, 678.90f, in ctx);

            Assert.AreEqual(h1, h2, 0.00001f);
        }

        [Test]
        public void CalculateElevation_SupportsAllNoiseTypes()
        {
            var perlin = NoiseSettings.Default;
            perlin.Type = NoiseType.PerlinFbm;

            var ridged = NoiseSettings.Default;
            ridged.Type = NoiseType.RidgedMultifractal;

            var billow = NoiseSettings.Default;
            billow.Type = NoiseType.Billow;

            var ctxPerlin = new TerrainShaperContext(perlin, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);
            var ctxRidged = new TerrainShaperContext(ridged, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);
            var ctxBillow = new TerrainShaperContext(billow, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);

            float hPerlin = _shaper.CalculateElevation(50f, 50f, in ctxPerlin);
            float hRidged = _shaper.CalculateElevation(50f, 50f, in ctxRidged);
            float hBillow = _shaper.CalculateElevation(50f, 50f, in ctxBillow);

            Assert.IsFalse(float.IsNaN(hPerlin));
            Assert.IsFalse(float.IsNaN(hRidged));
            Assert.IsFalse(float.IsNaN(hBillow));
        }

        [Test]
        public void CalculateElevation_MacroMaskAmplifiesMountains()
        {
            var macroDisabled = MacroMaskSettings.Default;
            macroDisabled.Enabled = false;

            var macroEnabled = MacroMaskSettings.Default;
            macroEnabled.Enabled = true;
            macroEnabled.MountainAmplification = 3.0f;
            macroEnabled.ValleyDamping = 0.1f;

            var ctxBase = new TerrainShaperContext(NoiseSettings.Default, macroDisabled, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);
            var ctxMacro = new TerrainShaperContext(NoiseSettings.Default, macroEnabled, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);

            float hBase = _shaper.CalculateElevation(100f, 100f, in ctxBase);
            float hMacro = _shaper.CalculateElevation(100f, 100f, in ctxMacro);

            Assert.IsFalse(float.IsNaN(hMacro));
            Assert.AreNotEqual(hBase, hMacro);
        }

        [Test]
        public void CalculateElevation_CircularFalloffDampsEdgeElevation()
        {
            var falloff = FalloffSettings.Default;
            falloff.Mode = FalloffMode.Circular;
            falloff.FalloffStartRadius = 200f;
            falloff.FalloffEndRadius = 500f;
            falloff.PowerExponent = 2.0f;

            var ctx = new TerrainShaperContext(NoiseSettings.Default, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, falloff);

            float hCenter = _shaper.CalculateElevation(0f, 0f, in ctx);
            float hOuter = _shaper.CalculateElevation(350f, 0f, in ctx);

            Assert.LessOrEqual(hOuter, hCenter);
        }

        [Test]
        public void CalculateElevation_RiverCarvingDepressesTerrain()
        {
            var riverDisabled = RiverSettings.Default;
            riverDisabled.Enabled = false;

            var riverEnabled = RiverSettings.Default;
            riverEnabled.Enabled = true;
            riverEnabled.CarveDepth = 30f;
            riverEnabled.RiverbedWidth = 50f;

            var ctxNoRiver = new TerrainShaperContext(NoiseSettings.Default, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, riverDisabled, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);
            var ctxWithRiver = new TerrainShaperContext(NoiseSettings.Default, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, WaterSettings.Default, riverEnabled, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);

            float hNoRiver = _shaper.CalculateElevation(25f, 25f, in ctxNoRiver);
            float hWithRiver = _shaper.CalculateElevation(25f, 25f, in ctxWithRiver);

            Assert.LessOrEqual(hWithRiver, hNoRiver + 0.001f);
        }

        [Test]
        public void CalculateElevation_SeaLevelClampsOceanFloor()
        {
            var water = WaterSettings.Default;
            water.Enabled = true;
            water.SeaLevel = 50f;
            water.OceanFloorDepth = 15f;

            var noise = NoiseSettings.Default;
            noise.HeightMultiplier = 10f; // very low terrain

            var ctx = new TerrainShaperContext(noise, MacroMaskSettings.Default, TectonicSettings.Default, null, HeightCurveSettings.Default, water, RiverSettings.Default, HydrologySettings.Default, RiverGraph.Empty, FalloffSettings.Default);

            float h = _shaper.CalculateElevation(0f, 0f, in ctx);

            Assert.GreaterOrEqual(h, water.SeaLevel - water.OceanFloorDepth - 0.001f);
            Assert.LessOrEqual(h, water.SeaLevel + 10f);
        }

        [Test]
        public void GenerateHeightMap_ProducesValidNormalizedArray()
        {
            int resolution = 24;
            float size = 120f;
            float[,] buffer = new float[resolution + 1, resolution + 1];

            var ctx = TerrainShaperContext.CreateDefault();

            _shaper.GenerateHeightMap(0f, 0f, size, resolution, in ctx, buffer);

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float val = buffer[x, y];
                    Assert.IsFalse(float.IsNaN(val));
                    Assert.IsFalse(float.IsInfinity(val));
                }
            }
        }
    }
}
