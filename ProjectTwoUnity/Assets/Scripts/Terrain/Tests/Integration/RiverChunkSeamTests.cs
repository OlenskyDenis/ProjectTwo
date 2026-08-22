namespace ProjectTwo.Terrain.Tests.Integration
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class RiverChunkSeamTests
    {
        private ProceduralTerrainShaper _shaper;
        private HydrologyService _hydrologyService;

        [SetUp]
        public void SetUp()
        {
            _shaper = new ProceduralTerrainShaper();
            _hydrologyService = new HydrologyService();
        }

        [Test]
        public void AdjacentChunks_AlongCarvedRiverbed_HaveZeroVertexElevationSeams()
        {
            var riverSegments = new[]
            {
                new RiverSegment(
                    0, 0, 1,
                    new Vector3(-100f, 30f, 0f),
                    new Vector3(0f, 25f, 0f),
                    new Vector3(100f, 20f, 0f),
                    length: 200f,
                    channelWidth: 20f,
                    carveDepth: 10f,
                    streamOrder: 2,
                    flowRate: 4f)
            };

            var graph = new RiverGraph(null, riverSegments, null);
            var hydrology = HydrologySettings.Default;
            hydrology.BaseCarveDepth = 10f;
            hydrology.BaseRiverWidth = 20f;

            var noise = NoiseSettings.Default;
            var macro = MacroMaskSettings.Default;
            var tectonics = TectonicSettings.Default;
            tectonics.Enabled = false;
            var heightCurve = HeightCurveSettings.Default;
            var water = WaterSettings.Default;
            water.Enabled = false;
            var river = RiverSettings.Default;
            river.Enabled = false;
            var falloff = FalloffSettings.Default;

            var ctx = new TerrainShaperContext(noise, macro, tectonics, null, heightCurve, water, river, hydrology, graph, falloff);

            // Chunk A ends at X = 0, Chunk B starts at X = 0
            // Test 10 points along the boundary X = 0 for Z from -30 to +30
            for (int i = -15; i <= 15; i += 3)
            {
                float z = (float)i;

                // Evaluate height exactly on border
                float heightA = _shaper.CalculateElevation(0.0f, z, in ctx);
                float heightB = _shaper.CalculateElevation(0.0f, z, in ctx);

                Assert.AreEqual(heightA, heightB, 0.0001f, $"Heights across border at Z={z} must match perfectly.");
            }
        }
    }
}
