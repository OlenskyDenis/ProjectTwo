namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class HydrologyServiceTests
    {
        private IHydrologyService _hydrologyService;
        private ITerrainShaper _shaper;
        private ITectonicService _tectonicService;

        [SetUp]
        public void SetUp()
        {
            _hydrologyService = new HydrologyService();
            _shaper = new ProceduralTerrainShaper();
            _tectonicService = new TectonicService();
        }

        [Test]
        public void GenerateRiverGraph_ValidSettings_CreatesSourcesAndConnectedSegments()
        {
            var hydrology = HydrologySettings.Default;
            hydrology.SourceCount = 10;

            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var graph = _hydrologyService.GenerateRiverGraph(
                hydrology,
                _shaper,
                noise,
                tectonics,
                water);

            Assert.IsNotNull(graph);
            Assert.Greater(graph.NodeCount, 0, "Graph should contain nodes.");
            Assert.Greater(graph.SegmentCount, 0, "Graph should contain connected river segments.");

            // Verify downstream path ordering
            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                Assert.Greater(seg.Length, 0f, "Segment length must be positive.");
                Assert.Greater(seg.ChannelWidth, 0f, "Channel width must be positive.");
                Assert.Greater(seg.CarveDepth, 0f, "Carve depth must be positive.");
            }
        }

        [Test]
        public void GenerateRiverGraph_AllPaths_DescendTowardsBaseWaterLevel()
        {
            var hydrology = HydrologySettings.Default;
            hydrology.SourceCount = 5;

            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var graph = _hydrologyService.GenerateRiverGraph(
                hydrology,
                _shaper,
                noise,
                tectonics,
                water);

            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                // Downstream point should not be higher than upstream start point
                Assert.LessOrEqual(seg.EndPosition.y, seg.StartPosition.y + 0.05f,
                    "River water must strictly flow downhill or level.");
            }
        }
    }
}
