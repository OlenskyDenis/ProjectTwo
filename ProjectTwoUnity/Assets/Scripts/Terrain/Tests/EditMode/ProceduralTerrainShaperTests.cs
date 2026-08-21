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
            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            float h1 = _shaper.CalculateElevation(123.45f, 678.90f, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            float h2 = _shaper.CalculateElevation(123.45f, 678.90f, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);

            Assert.AreEqual(h1, h2, 0.00001f);
        }

        [Test]
        public void CalculateElevation_SupportsAllNoiseTypes()
        {
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            var perlin = NoiseSettings.Default;
            perlin.Type = NoiseType.PerlinFbm;

            var ridged = NoiseSettings.Default;
            ridged.Type = NoiseType.RidgedMultifractal;

            var billow = NoiseSettings.Default;
            billow.Type = NoiseType.Billow;

            float hPerlin = _shaper.CalculateElevation(50f, 50f, perlin, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            float hRidged = _shaper.CalculateElevation(50f, 50f, ridged, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            float hBillow = _shaper.CalculateElevation(50f, 50f, billow, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);

            Assert.IsFalse(float.IsNaN(hPerlin));
            Assert.IsFalse(float.IsNaN(hRidged));
            Assert.IsFalse(float.IsNaN(hBillow));
        }

        [Test]
        public void CalculateElevation_MacroMaskAmplifiesMountains()
        {
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            var macroDisabled = MacroMaskSettings.Default;
            macroDisabled.Enabled = false;

            var macroEnabled = MacroMaskSettings.Default;
            macroEnabled.Enabled = true;
            macroEnabled.MountainAmplification = 3.0f;
            macroEnabled.ValleyDamping = 0.1f;

            float hBase = _shaper.CalculateElevation(100f, 100f, noise, macroDisabled, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            float hMacro = _shaper.CalculateElevation(100f, 100f, noise, macroEnabled, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);

            Assert.IsFalse(float.IsNaN(hMacro));
            Assert.AreNotEqual(hBase, hMacro);
        }

        [Test]
        public void CalculateElevation_CircularFalloffDampsEdgeElevation()
        {
            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;

            var falloff = new FalloffSettings
            {
                Mode = FalloffMode.Circular,
                FalloffStartRadius = 100f,
                FalloffEndRadius = 300f,
                PowerExponent = 2.0f
            };

            float hCenter = _shaper.CalculateElevation(0f, 0f, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            float hOuter = _shaper.CalculateElevation(350f, 0f, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);

            Assert.Greater(hCenter, 0f);
            Assert.AreEqual(0f, hOuter, 0.001f);
        }

        [Test]
        public void CalculateElevation_RiverCarvingDepressesTerrain()
        {
            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            var riverDisabled = RiverSettings.Default;
            riverDisabled.Enabled = false;

            var riverEnabled = new RiverSettings
            {
                Enabled = true,
                Seed = 1337,
                Frequency = 0.01f,
                CarveDepth = 20f,
                RiverbedWidth = 30f,
                BankSmoothness = 0.5f
            };

            float hNoRiver = _shaper.CalculateElevation(25f, 25f, noise, macro, tectonics, null, curve, water, riverDisabled, hydrology, RiverGraph.Empty, falloff);
            float hWithRiver = _shaper.CalculateElevation(25f, 25f, noise, macro, tectonics, null, curve, water, riverEnabled, hydrology, RiverGraph.Empty, falloff);

            Assert.IsFalse(float.IsNaN(hWithRiver));
            Assert.LessOrEqual(hWithRiver, hNoRiver + 0.001f);
        }

        [Test]
        public void CalculateElevation_SeaLevelClampsOceanFloor()
        {
            var noise = NoiseSettings.Default;
            noise.HeightMultiplier = 10f;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            var water = new WaterSettings
            {
                Enabled = true,
                SeaLevel = 15f,
                OceanFloorDepth = 5f,
                ShorelineSmoothness = 1f
            };

            float h = _shaper.CalculateElevation(0f, 0f, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff);
            Assert.GreaterOrEqual(h, water.SeaLevel - water.OceanFloorDepth);
        }

        [Test]
        public void GenerateHeightMap_FillsCorrectDimensions()
        {
            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            var curve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            var river = RiverSettings.Default;
            var hydrology = HydrologySettings.Default;
            var falloff = FalloffSettings.Default;

            int resolution = 24;
            float[,] buffer = new float[resolution + 1, resolution + 1];

            _shaper.GenerateHeightMap(0, 0, 240, resolution, noise, macro, tectonics, null, curve, water, river, hydrology, RiverGraph.Empty, falloff, buffer);

            Assert.AreEqual(25, buffer.GetLength(0));
            Assert.AreEqual(25, buffer.GetLength(1));
            Assert.IsFalse(float.IsNaN(buffer[0, 0]));
            Assert.IsFalse(float.IsNaN(buffer[24, 24]));
        }
    }
}
