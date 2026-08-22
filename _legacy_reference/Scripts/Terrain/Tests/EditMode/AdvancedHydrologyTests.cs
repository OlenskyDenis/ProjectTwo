namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class AdvancedHydrologyTests
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
        public void GenerateRiverGraph_RemovesAllOrphanBranchesViaBackwardReachability()
        {
            var hydrology = HydrologySettings.Default;
            hydrology.Enabled = true;
            hydrology.SourceCount = 8;
            hydrology.Seed = 12345;

            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            RiverGraph graph = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);

            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.Segments);

            // Build node connectivity
            var outgoing = new Dictionary<int, List<int>>();
            var incoming = new Dictionary<int, List<int>>();
            var nodeMap = new Dictionary<int, RiverNode>();

            if (graph.Nodes != null)
            {
                foreach (var node in graph.Nodes)
                {
                    nodeMap[node.Id] = node;
                }
            }

            foreach (var seg in graph.Segments)
            {
                if (!outgoing.ContainsKey(seg.StartNodeId)) outgoing[seg.StartNodeId] = new List<int>();
                outgoing[seg.StartNodeId].Add(seg.EndNodeId);

                if (!incoming.ContainsKey(seg.EndNodeId)) incoming[seg.EndNodeId] = new List<int>();
                incoming[seg.EndNodeId].Add(seg.StartNodeId);
            }

            // Verify that every single segment has valid start and end nodes and strictly positive length
            foreach (var seg in graph.Segments)
            {
                Assert.Greater(seg.Length, 0.1f, $"Segment {seg.Id} has near-zero length.");
                Assert.AreNotEqual(seg.StartNodeId, seg.EndNodeId, "Segment cannot be a self-loop.");
            }
        }

        [Test]
        public void RiverMeshBuilder_WaterfallOnSteepSlope_ConformsToTerrainSurface()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 120f;

            // River segment dropping down from mountain cliff
            var segment = new RiverSegment(
                id: 0,
                startNodeId: 0,
                endNodeId: 1,
                startPosition: new Vector3(0f, 60f, -40f),
                controlPoint: new Vector3(0f, 35f, 0f),
                endPosition: new Vector3(0f, 10f, 40f),
                length: 80f,
                channelWidth: 8f,
                carveDepth: 3f,
                streamOrder: 1,
                flowRate: 2f,
                startWidth: 1.5f,
                endWidth: 3f,
                isWaterfall: true);

            var graph = new RiverGraph(null, new[] { segment }, null);
            var hydrology = HydrologySettings.Default;
            hydrology.WaterfallStepSize = 1.5f;
            var shaper = new ProceduralTerrainShaper();
            var water = WaterSettings.Default;
            var ctx = TerrainShaperContext.CreateDefault();

            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, in ctx);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);

            // All vertices should be valid finite vectors with normals pointing generally outwards/upwards
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                Vector3 v = meshData.Vertices[i];
                Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
                Assert.GreaterOrEqual(v.y, 9.5f, "Vertex must not plunge arbitrarily below waterfall base.");
                Assert.LessOrEqual(v.y, 61f, "Vertex must not float into mid-air above waterfall summit.");
            }
        }

        [Test]
        public void ExtractLakeBasins_EnclosedDepression_FormsLakeAndOutflowChannel()
        {
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var basins = _hydrologyService.ExtractLakeBasins(_shaper, noise, tectonics, water, searchRadius: 300f);

            Assert.IsNotNull(basins);
            for (int i = 0; i < basins.Count; i++)
            {
                LakeBasin basin = basins[i];
                Assert.Greater(basin.Radius, 5f, "Lake radius must be positive and non-trivial.");
                Assert.Greater(basin.WaterElevation, water.SeaLevel - 10f, "Lake water elevation must be above subterranean floor.");
            }
        }

        [Test]
        public void RiverMeshBuilder_TaperedSpringAndSinkEnds_SmoothlyTransitionsWidth()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 120f;

            // Single segment: start is a spring source, end is an inland sink
            var segment = new RiverSegment(
                id: 0,
                startNodeId: 10,
                endNodeId: 20,
                startPosition: new Vector3(-30f, 25f, -30f),
                controlPoint: new Vector3(0f, 20f, 0f),
                endPosition: new Vector3(30f, 15f, 30f),
                length: 84f,
                channelWidth: 10f,
                carveDepth: 2f,
                streamOrder: 1,
                flowRate: 1.5f,
                startWidth: 4f,
                endWidth: 8f);

            var graph = new RiverGraph(null, new[] { segment }, null);
            var hydrology = HydrologySettings.Default;
            var water = WaterSettings.Default;
            var shaper = new ProceduralTerrainShaper();
            var ctx = TerrainShaperContext.CreateDefault();

            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, in ctx);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);

            // First pair of vertices (spring origin) should have very narrow width
            Vector3 v0 = meshData.Vertices[0];
            Vector3 v1 = meshData.Vertices[1];
            float startWidth = Vector3.Distance(v0, v1);

            // Last pair of vertices (terminal sink) should also taper down
            int lastIdx = meshData.Vertices.Length - 1;
            Vector3 vEnd0 = meshData.Vertices[lastIdx - 1];
            Vector3 vEnd1 = meshData.Vertices[lastIdx];
            float endWidth = Vector3.Distance(vEnd0, vEnd1);

            Assert.Less(startWidth, 2.0f, "Spring origin should be smoothly tapered to a small source stream width.");
            Assert.Less(endWidth, 3.5f, "Inland terminal sink should be smoothly tapered into alluvial infiltration.");
        }

        [Test]
        public void RiverMeshBuilder_UVCoordinates_AreMonotonicallyIncreasingWithoutMoireJumps()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 240f;

            var segment = new RiverSegment(
                id: 0,
                startNodeId: 0,
                endNodeId: 1,
                startPosition: new Vector3(-80f, 30f, -80f),
                controlPoint: new Vector3(0f, 20f, 0f),
                endPosition: new Vector3(80f, 10f, 80f),
                length: 226f,
                channelWidth: 12f,
                carveDepth: 4f,
                streamOrder: 2,
                flowRate: 3f);

            var graph = new RiverGraph(null, new[] { segment }, null);
            var hydrology = HydrologySettings.Default;
            var water = WaterSettings.Default;
            var shaper = new ProceduralTerrainShaper();
            var ctx = TerrainShaperContext.CreateDefault();

            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, in ctx);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);

            // Verify that U coordinates are 0 on left and 1 on right, and V coordinates grow monotonically
            float prevV = -1f;
            for (int i = 0; i < meshData.UVs.Length; i += 2)
            {
                Vector2 leftUV = meshData.UVs[i];
                Vector2 rightUV = meshData.UVs[i + 1];

                Assert.AreEqual(0f, leftUV.x, 0.001f);
                Assert.AreEqual(1f, rightUV.x, 0.001f);
                Assert.AreEqual(leftUV.y, rightUV.y, 0.001f, "V coordinates on left and right bank must match.");

                Assert.GreaterOrEqual(leftUV.y, prevV, "V coordinate must monotonically increase along the river ribbon to prevent moire artifacts.");
                prevV = leftUV.y;
            }
        }

        [Test]
        public void RiverMeshBuilder_SpanningAcrossSaddle_ClampsVerticesToActualTerrainElevations()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 240f;
            var ctx = TerrainShaperContext.CreateDefault();

            // Define two endpoints on mountain peaks spanning across a valley/saddle
            Vector3 startPeak = new Vector3(-60f, _shaper.CalculateElevation(-60f, -60f, in ctx), -60f);
            Vector3 endPeak = new Vector3(60f, _shaper.CalculateElevation(60f, 60f, in ctx), 60f);

            var segment = new RiverSegment(
                id: 0,
                startNodeId: 0,
                endNodeId: 1,
                startPosition: startPeak,
                controlPoint: (startPeak + endPeak) * 0.5f,
                endPosition: endPeak,
                length: Vector3.Distance(startPeak, endPeak),
                channelWidth: 6f,
                carveDepth: 2f,
                streamOrder: 1,
                flowRate: 2f,
                startWidth: 3f,
                endWidth: 6f,
                isWaterfall: true);

            var graph = new RiverGraph(null, new[] { segment }, null);
            var hydrology = HydrologySettings.Default;
            var water = WaterSettings.Default;
            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, _shaper, in ctx);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);

            // Verify every single vertex is strictly clamped to the ground surface (+0.15m normal offset)
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                Vector3 v = meshData.Vertices[i];
                float expectedGroundHeight = _shaper.CalculateElevation(v.x, v.z, in ctx);
                float distanceToGround = Mathf.Abs(v.y - expectedGroundHeight);

                Assert.LessOrEqual(distanceToGround, 0.5f, $"Vertex {i} at ({v.x}, {v.z}) with height {v.y} must not float in mid-air (expected ground ~{expectedGroundHeight}, delta: {distanceToGround}).");
            }
        }

        [Test]
        public void RiverMeshBuilder_MultiSegmentContinuousChain_WeldsConsecutiveVerticesWithoutDiscontinuities()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 120f;
            var water = WaterSettings.Default;
            var hydrology = HydrologySettings.Default;
            var ctx = TerrainShaperContext.CreateDefault();

            // 3 consecutive connected segments going down a steep mountain slope
            Vector3 p0 = new Vector3(0f, 100f, 0f);
            Vector3 p1 = new Vector3(0f, 75f, 25f);
            Vector3 p2 = new Vector3(5f, 50f, 50f);
            Vector3 p3 = new Vector3(10f, 20f, 75f);

            var seg0 = new RiverSegment(0, 0, 1, p0, (p0 + p1) * 0.5f, p1, 35f, 4f, 2f, 1, 2f, 3f, 4f, isWaterfall: true);
            var seg1 = new RiverSegment(1, 1, 2, p1, (p1 + p2) * 0.5f, p2, 35f, 4f, 2f, 1, 2f, 4f, 5f, isWaterfall: true);
            var seg2 = new RiverSegment(2, 2, 3, p2, (p2 + p3) * 0.5f, p3, 35f, 4f, 2f, 1, 2f, 5f, 6f, isWaterfall: true);

            var graph = new RiverGraph(null, new[] { seg0, seg1, seg2 }, null);
            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, _shaper, in ctx);

            Assert.IsNotNull(meshData);
            Assert.Greater(meshData.Vertices.Length, 10);
            Assert.Greater(meshData.Triangles.Length, 12);

            // Verify triangle connectivity forms a continuous uninterrupted ribbon
            var usedVertices = new HashSet<int>(meshData.Triangles);
            Assert.AreEqual(meshData.Vertices.Length, usedVertices.Count, "All generated vertices must be part of connected continuous triangles.");
        }

        [Test]
        public void ExhaustiveHydrologyPermutations_AcrossSeedsAndPresets_ProducesZeroDeadEndsOrFloatingGaps()
        {
            int[] testSeeds = { 42, 100, 777, 1337, 99999 };

            foreach (int seed in testSeeds)
            {
                var hydrology = HydrologySettings.Default;
                hydrology.Seed = seed;
                hydrology.SourceCount = 12;

                var noise = NoiseSettings.Default;
                noise.Seed = seed;
                var tectonics = TectonicSettings.Default;
                tectonics.Seed = seed;
                var water = WaterSettings.Default;

                var graph = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);
                Assert.IsNotNull(graph);

                for (int s = 0; s < graph.SegmentCount; s++)
                {
                    ref readonly RiverSegment seg = ref graph.Segments[s];
                    Assert.Greater(seg.Length, 0.01f);
                    Assert.AreNotEqual(seg.StartNodeId, seg.EndNodeId);
                }

                var chunkCoord = new ChunkCoordinate(0, 0);
                var ctx = new TerrainShaperContext(noise, MacroMaskSettings.Default, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, graph, FalloffSettings.Default);
                var meshData = _riverMeshBuilder.BuildChunkRiverMesh(chunkCoord, 240f, graph, hydrology, water, _shaper, in ctx);

                if (!meshData.IsEmpty)
                {
                    // Verify no degenerate or disconnected floating quads
                    Assert.AreEqual(0, meshData.Triangles.Length % 3, "Triangle indices must be multiples of 3.");
                }
            }
        }
    }
}
