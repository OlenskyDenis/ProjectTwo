namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class TerrainMeshBuilderTests
    {
        [Test]
        public void GenerateTerrainMesh_ProducesCorrectVertexAndTriangleCounts_ForLOD0()
        {
            int size = 16;
            float[,] values = new float[size, size];
            HeightMap heightMap = new HeightMap(values);

            TerrainMeshData meshData = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1);

            Assert.IsNotNull(meshData);
            Assert.AreEqual(size * size, meshData.Vertices.Length);
            Assert.AreEqual((size - 1) * (size - 1) * 6, meshData.Triangles.Length);
        }

        [Test]
        public void GenerateTerrainMesh_ReducesVertexCount_ForHigherLODs()
        {
            int size = 25; // 24 segments
            float[,] values = new float[size, size];
            HeightMap heightMap = new HeightMap(values);

            TerrainMeshData meshLod0 = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1);
            TerrainMeshData meshLod1 = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 2);
            TerrainMeshData meshLod2 = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 4);

            Assert.Greater(meshLod0.Vertices.Length, meshLod1.Vertices.Length);
            Assert.Greater(meshLod1.Vertices.Length, meshLod2.Vertices.Length);
        }

        [Test]
        public void GenerateTerrainMesh_RecalculatesNormals_WithoutZeroLength()
        {
            int size = 8;
            float[,] values = new float[size, size];
            values[4, 4] = 0.8f;
            HeightMap heightMap = new HeightMap(values);

            TerrainMeshData meshData = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1);

            foreach (Vector3 normal in meshData.Normals)
            {
                Assert.Greater(normal.magnitude, 0.9f, "Normals must be normalized unit vectors.");
            }
        }
    }
}
