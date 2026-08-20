namespace ProjectTwo.Terrain.Tests.Integration
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class TectonicRidgeContinuityTests
    {
        private TectonicService _tectonicService;
        private ProceduralTerrainShaper _shaper;

        [SetUp]
        public void SetUp()
        {
            _tectonicService = new TectonicService();
            _shaper = new ProceduralTerrainShaper();
        }

        [Test]
        public void MountainRidge_SpansAcrossAdjacentChunks_WithoutSeamOrZeroGaps()
        {
            var tectonics = TectonicSettings.Default;
            tectonics.PlateCount = 8;
            tectonics.PlateScale = 1200f;
            tectonics.MountainUplift = 120f;
            tectonics.BoundaryInfluenceWidth = 300f;

            _tectonicService.GenerateTectonicPartition(tectonics, out _, out var boundaries);

            // Find a convergent boundary
            TectonicBoundary? targetBoundary = null;
            for (int i = 0; i < boundaries.Length; i++)
            {
                if (boundaries[i].BoundaryType == TectonicBoundaryType.Convergent)
                {
                    targetBoundary = boundaries[i];
                    break;
                }
            }

            Assert.IsTrue(targetBoundary.HasValue, "Should generate at least one convergent boundary.");
            var b = targetBoundary.Value;

            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            macro.Enabled = false;
            var heightCurve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            water.Enabled = false;
            var river = RiverSettings.Default;
            river.Enabled = false;
            var hydrology = HydrologySettings.Default;
            hydrology.Enabled = false;
            var falloff = FalloffSettings.Default;

            // Sample 20 points along the boundary segment
            int sampleCount = 20;
            float minElev = float.MaxValue;
            float maxElev = float.MinValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                Vector2 pos = Vector2.Lerp(b.StartPoint, b.EndPoint, t);

                float elev = _shaper.CalculateElevation(
                    pos.x,
                    pos.y,
                    noise,
                    macro,
                    tectonics,
                    boundaries,
                    heightCurve,
                    water,
                    river,
                    hydrology,
                    null,
                    falloff);

                if (elev < minElev) minElev = elev;
                if (elev > maxElev) maxElev = elev;
            }

            Assert.Greater(maxElev, 80f, "Ridge elevation should reach mountain heights along convergent line.");
            Assert.Greater(minElev, 20f, "Mountain chain must maintain unbroken continuity along the boundary path.");
        }
    }
}
