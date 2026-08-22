namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class RiverMeshBuilderTests
    {
        private IRiverMeshBuilder _meshBuilder;

        [SetUp]
        public void SetUp()
        {
            _meshBuilder = new RiverMeshBuilder();
        }

        [Test]
        public void BuildChunkRiverMesh_IntersectingSegment_GeneratesWaterMeshData()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 120f;

            // River segment crossing chunk (0, 0)
            var segments = new[]
            {
                new RiverSegment(
                    0, 0, 1,
                    new Vector3(-50f, 20f, -50f),
                    new Vector3(0f, 15f, 0f),
                    new Vector3(50f, 10f, 50f),
                    length: 141f,
                    channelWidth: 12f,
                    carveDepth: 8f,
                    streamOrder: 2,
                    flowRate: 3f)
            };

            var graph = new RiverGraph(null, segments, null);
            var hydrology = HydrologySettings.Default;
            var water = WaterSettings.Default;
            var shaper = new ProceduralTerrainShaper();
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;

            var meshData = _meshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, noise, tectonics);

            Assert.IsNotNull(meshData);
            Assert.IsFalse(meshData.IsEmpty);
            Assert.Greater(meshData.Vertices.Length, 0, "Water mesh must have vertices.");
            Assert.Greater(meshData.Triangles.Length, 0, "Water mesh must have triangles.");
            Assert.AreEqual(meshData.Vertices.Length, meshData.UVs.Length);
            Assert.AreEqual(meshData.Vertices.Length, meshData.Normals.Length);
        }

        [Test]
        public void BuildChunkRiverMesh_NoIntersectingSegment_ReturnsEmpty()
        {
            var coord = new ChunkCoordinate(10, 10);
            float chunkSize = 120f;

            var segments = new[]
            {
                new RiverSegment(
                    0, 0, 1,
                    new Vector3(-50f, 20f, -50f),
                    new Vector3(0f, 15f, 0f),
                    new Vector3(50f, 10f, 50f),
                    length: 141f,
                    channelWidth: 12f,
                    carveDepth: 8f,
                    streamOrder: 2,
                    flowRate: 3f)
            };

            var graph = new RiverGraph(null, segments, null);
            var hydrology = HydrologySettings.Default;
            var water = WaterSettings.Default;
            var shaper = new ProceduralTerrainShaper();
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;

            var meshData = _meshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water, shaper, noise, tectonics);

            Assert.IsNotNull(meshData);
            Assert.IsTrue(meshData.IsEmpty, "Non-intersecting chunk should return empty river mesh data.");
        }

        [Test]
        public void BuildChunkRiverMesh_Vertices_AreBoundedWithinExpectedElevationRange()
        {
            var coord = new ChunkCoordinate(0, 0);
            float chunkSize = 240f;

            var segments = new[]
            {
                new RiverSegment(
                    0, 0, 1,
                    new Vector3(-40f, 18f, -40f),
                    new Vector3(0f, 15f, 0f),
                    new Vector3(40f, 12f, 40f),
                    length: 113f,
                    channelWidth: 10f,
                    carveDepth: 5f,
                    streamOrder: 1,
                    flowRate: 2f)
            };

            var graph = new RiverGraph(null, segments, null);
            var shaper = new ProceduralTerrainShaper();
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var meshData = _meshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, HydrologySettings.Default, WaterSettings.Default, shaper, noise, tectonics);

            Assert.IsFalse(meshData.IsEmpty);
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                Vector3 v = meshData.Vertices[i];
                Assert.GreaterOrEqual(v.y, 10f, $"River vertex {i} Y elevation must not plunge below lower segment height.");
                Assert.LessOrEqual(v.y, 25f, $"River vertex {i} Y elevation must not spike into the sky.");
            }
        }

        [Test]
        public void HydrologyService_GenerateRiverGraph_ElevationsAreBoundedBySingleHeightMultiplier()
        {
            var shaper = new ProceduralTerrainShaper();
            var hydrology = new HydrologyService();
            var noise = NoiseSettings.Default;
            var tectonics = TectonicSettings.Default;
            var water = WaterSettings.Default;
            var hydroSettings = HydrologySettings.Default;

            RiverGraph graph = hydrology.GenerateRiverGraph(hydroSettings, shaper, noise, tectonics, water);

            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.Segments);

            float maxAllowedHeight = noise.HeightMultiplier + (tectonics.Enabled ? tectonics.MountainUplift : 0f) + 10f;

            for (int i = 0; i < graph.Segments.Length; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                Assert.LessOrEqual(seg.StartPosition.y, maxAllowedHeight, $"River segment {i} StartPosition.y must not be quadratically inflated into sky.");
                Assert.LessOrEqual(seg.EndPosition.y, maxAllowedHeight, $"River segment {i} EndPosition.y must not be quadratically inflated into sky.");
                Assert.LessOrEqual(seg.ControlPoint.y, maxAllowedHeight, $"River segment {i} ControlPoint.y must not be quadratically inflated into sky.");
            }
        }
    }
}

