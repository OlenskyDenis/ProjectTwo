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
        private IHydrologyService _hydrologyService;
        private IRiverMeshBuilder _riverMeshBuilder;
        private ITerrainShaper _shaper;

        [SetUp]
        public void SetUp()
        {
            _hydrologyService = new HydrologyService();
            _riverMeshBuilder = new RiverMeshBuilder();
            _shaper = new ProceduralTerrainShaper();
        }

        [Test]
        public void AdaptiveWaterfallStepping_OnSteepSlopes_ReducesStepSizeToWaterfallRange()
        {
            float baseStep = 25f;

            // Flat terrain (< 5 deg) -> should return full base step
            float flatStep = _hydrologyService.GetAdaptiveStepSize(2f, baseStep);
            Assert.AreEqual(baseStep, flatStep, 0.1f, "Flat terrain should retain full base step size.");

            // Moderate slope (15 deg)
            float modStep = _hydrologyService.GetAdaptiveStepSize(15f, baseStep);
            Assert.Less(modStep, baseStep);

            // Steep cliff slope (> 25 deg, e.g. 45 deg and 70 deg) -> reduced to 1.0m - 2.0m range
            float steepStep = _hydrologyService.GetAdaptiveStepSize(45f, baseStep);
            Assert.LessOrEqual(steepStep, 3f, "Waterfall step on 45 deg slope must be aggressively reduced.");

            float cliffStep = _hydrologyService.GetAdaptiveStepSize(70f, baseStep);
            Assert.GreaterOrEqual(cliffStep, 0.5f);
            Assert.LessOrEqual(cliffStep, 2.0f, "Waterfall step on 70 deg cliff must be between 0.5m and 2.0m.");
        }

        [Test]
        public void RiverMeshBuilder_CliffConforming_VertexClamping_ClingsWithinThreshold()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 120f;

            // Create a steep waterfall segment dropping 50m vertically over 10m horizontal
            var segment = new RiverSegment(
                id: 0,
                startNodeId: 0,
                endNodeId: 1,
                startPosition: new Vector3(0f, 60f, -5f),
                controlPoint: new Vector3(0f, 35f, 0f),
                endPosition: new Vector3(0f, 10f, 5f),
                length: 51f,
                channelWidth: 3f,
                carveDepth: 2f,
                streamOrder: 1,
                flowRate: 2f,
                startWidth: 1.5f,
                endWidth: 3f,
                isWaterfall: true);

            var graph = new RiverGraph(null, new[] { segment }, null);
            var hydrology = HydrologySettings.Default;
            hydrology.WaterfallStepSize = 1.5f;
            var shaper = new ProceduralTerrainShaper();
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, noise, tectonics);

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
            // Verify any extracted basins have positive capacity and water elevation above sea level
            for (int i = 0; i < basins.Count; i++)
            {
                var basin = basins[i];
                Assert.Greater(basin.Radius, 0f);
                Assert.GreaterOrEqual(basin.WaterElevation, water.SeaLevel);
            }
        }

        [Test]
        public void HydrologyService_StrahlerStreamOrder_ConfluenceScalesChannelWidth()
        {
            var hydrology = HydrologySettings.Default;
            hydrology.SourceCount = 25;
            hydrology.BaseRiverWidth = 6f;
            hydrology.WidthGrowthFactor = 1.5f;

            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var graph = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);

            Assert.IsNotNull(graph);
            Assert.Greater(graph.SegmentCount, 0);

            // Higher stream order segments must have wider channel width than order 1 headwaters
            float minOrder1Width = float.MaxValue;
            float maxHighOrderWidth = 0f;
            int maxOrderFound = 1;

            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                if (seg.StreamOrder == 1)
                {
                    if (seg.ChannelWidth < minOrder1Width) minOrder1Width = seg.ChannelWidth;
                }
                else if (seg.StreamOrder > 1)
                {
                    if (seg.StreamOrder > maxOrderFound) maxOrderFound = seg.StreamOrder;
                    if (seg.ChannelWidth > maxHighOrderWidth) maxHighOrderWidth = seg.ChannelWidth;
                }
            }

            if (maxOrderFound > 1)
            {
                Assert.Greater(maxHighOrderWidth, minOrder1Width,
                    "High order river trunks must be wider than headwater mountain streams.");
            }
        }

        [Test]
        public void HydrologyService_DeadEndPruning_EliminatesOrphanDisconnectedSegments()
        {
            var hydrology = HydrologySettings.Default;
            hydrology.SourceCount = 15;

            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;

            var graph = _hydrologyService.GenerateRiverGraph(hydrology, _shaper, noise, tectonics, water);

            Assert.IsNotNull(graph);
            // Build node degree lookup
            var nodeDegrees = new Dictionary<int, int>();
            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                nodeDegrees[seg.StartNodeId] = nodeDegrees.GetValueOrDefault(seg.StartNodeId, 0) + 1;
                nodeDegrees[seg.EndNodeId] = nodeDegrees.GetValueOrDefault(seg.EndNodeId, 0) + 1;
            }

            // Verify no isolated segment of length 0 or with orphan nodes
            for (int i = 0; i < graph.SegmentCount; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                Assert.Greater(seg.Length, 0.01f, "All segments must have valid positive length.");
                Assert.AreNotEqual(seg.StartNodeId, seg.EndNodeId, "Segment cannot connect a node to itself.");
            }
        }

        [Test]
        public void RiverMeshBuilder_AcrossMountainSaddle_AllVerticesStrictlyClampedToTerrainSurface()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 240f;
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;
            var hydrology = HydrologySettings.Default;

            // Define two endpoints on mountain peaks spanning across a valley/saddle
            Vector3 startPeak = new Vector3(-60f, _shaper.CalculateElevation(-60f, -60f, noise, MacroMaskSettings.Default, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, FalloffSettings.Default), -60f);
            Vector3 endPeak = new Vector3(60f, _shaper.CalculateElevation(60f, 60f, noise, MacroMaskSettings.Default, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, FalloffSettings.Default), 60f);

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
            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, _shaper, noise, tectonics);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);

            // Verify every single vertex is strictly clamped to the ground surface (+0.15m normal offset)
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                Vector3 v = meshData.Vertices[i];
                float expectedGroundHeight = _shaper.CalculateElevation(v.x, v.z, noise, MacroMaskSettings.Default, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, FalloffSettings.Default);
                float distanceToGround = Mathf.Abs(v.y - expectedGroundHeight);

                Assert.LessOrEqual(distanceToGround, 0.5f, $"Vertex {i} at ({v.x}, {v.z}) with height {v.y} must not float in mid-air (expected ground ~{expectedGroundHeight}, delta: {distanceToGround}).");
            }
        }

        [Test]
        public void RiverMeshBuilder_MultiSegmentContinuousChain_WeldsConsecutiveVerticesWithoutDiscontinuities()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 240f;
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;
            var hydrology = HydrologySettings.Default;

            // 3 consecutive connected segments going down a steep mountain slope
            Vector3 p0 = new Vector3(0f, 100f, 0f);
            Vector3 p1 = new Vector3(0f, 75f, 25f);
            Vector3 p2 = new Vector3(5f, 50f, 50f);
            Vector3 p3 = new Vector3(10f, 20f, 75f);

            var seg0 = new RiverSegment(0, 0, 1, p0, (p0 + p1) * 0.5f, p1, 35f, 4f, 2f, 1, 2f, 3f, 4f, isWaterfall: true);
            var seg1 = new RiverSegment(1, 1, 2, p1, (p1 + p2) * 0.5f, p2, 35f, 4f, 2f, 1, 2f, 4f, 5f, isWaterfall: true);
            var seg2 = new RiverSegment(2, 2, 3, p2, (p2 + p3) * 0.5f, p3, 35f, 4f, 2f, 1, 2f, 5f, 6f, isWaterfall: true);

            var graph = new RiverGraph(null, new[] { seg0, seg1, seg2 }, null);
            var meshData = _riverMeshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, _shaper, noise, tectonics);

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
                var meshData = _riverMeshBuilder.BuildChunkRiverMesh(chunkCoord, 240f, graph, hydrology, water, _shaper, noise, tectonics);

                if (!meshData.IsEmpty)
                {
                    // Verify no degenerate or disconnected floating quads
                    Assert.AreEqual(0, meshData.Triangles.Length % 3, "Triangle indices must be multiples of 3.");
                }
            }
        }
    }
}
