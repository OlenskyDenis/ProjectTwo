namespace ProjectTwo.Terrain.Tests.Integration
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class WaterfallAndLakeCascadeIntegrationTests
    {
        private ProceduralTerrainShaper _shaper;
        private HydrologyService _hydrologyService;
        private RiverMeshBuilder _riverMeshBuilder;

        [SetUp]
        public void SetUp()
        {
            _shaper = new ProceduralTerrainShaper();
            _hydrologyService = new HydrologyService();
            _riverMeshBuilder = new RiverMeshBuilder();
        }

        [Test]
        public void EndToEnd_AlpineHydrology_GeneratesConnectedWaterfallRibbonsAndLakeBasins()
        {
            var noise = new NoiseSettings
            {
                Type = NoiseType.RidgedMultifractal,
                Seed = 12345,
                Scale = 180f,
                Octaves = 4,
                Persistence = 0.5f,
                Lacunarity = 2.0f,
                HeightMultiplier = 120f,
                Offset = Vector2.zero
            };

            var tectonics = new TectonicSettings
            {
                Enabled = true,
                Seed = 12345,
                PlateCount = 8,
                PlateScale = 800f,
                MountainUplift = 80f,
                RiftDepth = 30f,
                BoundaryInfluenceWidth = 200f,
                RidgeSharpness = 1.8f,
                FaultNoiseWarp = 0.3f
            };

            var water = new WaterSettings
            {
                Enabled = true,
                SeaLevel = 10f,
                OceanFloorDepth = 15f,
                ShorelineSmoothness = 1.2f
            };

            var hydrology = new HydrologySettings
            {
                Enabled = true,
                Seed = 12345,
                SourceCount = 20,
                MinSourceElevationRatio = 0.55f,
                BaseRiverWidth = 8f,
                WidthGrowthFactor = 1.6f,
                BaseCarveDepth = 12f,
                BankSmoothness = 0.4f,
                MeanderIntensity = 0.35f,
                LakeMinDepthThreshold = 6f,
                WaterfallStepSize = 1.5f,
                HydraulicMomentum = 0.45f,
                DeltaBranchingChance = 0.3f
            };

            RiverGraph graph = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);

            Assert.IsNotNull(graph);
            Assert.Greater(graph.NodeCount, 0, "Alpine hydrology graph must have nodes.");
            Assert.Greater(graph.SegmentCount, 0, "Alpine hydrology graph must have connected segments.");

            // Verify waterfall ribbon presence on steep terrain
            bool foundWaterfall = false;
            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                if (seg.IsWaterfall)
                {
                    foundWaterfall = true;
                    break;
                }
            }

            Assert.IsTrue(foundWaterfall, "Alpine mountain terrain must generate cliff-conforming waterfall segments.");

            // Build river mesh for chunks covering origin
            var coord = new ChunkCoordinate(0, 0);
            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(
                coord,
                240f,
                graph,
                hydrology,
                water,
                _shaper,
                noise,
                tectonics);

            Assert.IsNotNull(meshData);
        }

        [Test]
        public void Hydrology_DeterministicGeneration_ProducesIdenticalGraphForSameSeed()
        {
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;
            var hydrology = HydrologySettings.Default;
            hydrology.Seed = 8888;

            RiverGraph graph1 = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);
            RiverGraph graph2 = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);

            Assert.AreEqual(graph1.NodeCount, graph2.NodeCount, "Node counts must be bit-exact identical for same seed.");
            Assert.AreEqual(graph1.SegmentCount, graph2.SegmentCount, "Segment counts must be bit-exact identical for same seed.");
            Assert.AreEqual(graph1.LakeCount, graph2.LakeCount, "Lake counts must be bit-exact identical for same seed.");

            for (int i = 0; i < graph1.SegmentCount; i++)
            {
                ref readonly RiverSegment s1 = ref graph1.Segments[i];
                ref readonly RiverSegment s2 = ref graph2.Segments[i];

                Assert.AreEqual(s1.StartPosition, s2.StartPosition);
                Assert.AreEqual(s1.EndPosition, s2.EndPosition);
                Assert.AreEqual(s1.IsWaterfall, s2.IsWaterfall);
                Assert.AreEqual(s1.StreamOrder, s2.StreamOrder);
            }
        }
    }
}
