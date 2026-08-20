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

            var meshData = _meshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water);

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

            var meshData = _meshBuilder.BuildChunkRiverMesh(coord, chunkSize, graph, hydrology, water);

            Assert.IsNotNull(meshData);
            Assert.IsTrue(meshData.IsEmpty, "Non-intersecting chunk should return empty river mesh data.");
        }
    }
}
