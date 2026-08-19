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
            Assert.Greater(meshData.Vertices.Length, size * size);
            Assert.Greater(meshData.Triangles.Length, (size - 1) * (size - 1) * 6);
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
        public void GenerateTerrainMesh_HandlesLODStepLessThanOne_Safely()
        {
            int size = 10;
            float[,] values = new float[size, size];
            HeightMap heightMap = new HeightMap(values);

            TerrainMeshData meshData = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 0);

            Assert.IsNotNull(meshData);
            Assert.Greater(meshData.Vertices.Length, size * size);
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

        [Test]
        public void GenerateTerrainMesh_AppliesElevationRegionColors()
        {
            int size = 4;
            float[,] values = new float[size, size];
            values[0, 0] = 0.1f; // Region 1
            values[1, 1] = 0.5f; // Region 2
            values[2, 2] = 0.95f; // Fallback Region

            TerrainRegion[] regions = new[]
            {
                new TerrainRegion("Water", 0.3f, Color.blue),
                new TerrainRegion("Grass", 0.7f, Color.green),
                new TerrainRegion("Snow", 1.0f, Color.white)
            };

            HeightMap heightMap = new HeightMap(values);
            TerrainMeshData meshData = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1, regions);

            Assert.IsNotNull(meshData.Colors);
            Assert.AreEqual(Color.blue, meshData.Colors[0]);
        }

        [Test]
        public void GenerateTerrainMesh_WorksWithNullOrEmptyRegions()
        {
            int size = 4;
            float[,] values = new float[size, size];
            HeightMap heightMap = new HeightMap(values);

            TerrainMeshData meshDataNull = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1, null);
            TerrainMeshData meshDataEmpty = TerrainMeshBuilder.GenerateTerrainMesh(heightMap, 240f, 30f, 1, new TerrainRegion[0]);

            Assert.IsNotNull(meshDataNull.Colors);
            Assert.IsNotNull(meshDataEmpty.Colors);
        }

        [Test]
        public void GenerateTerrainMesh_AdjacentChunks_AlignSeamlessly_OnBothXAndZ_Axes()
        {
            PerlinNoiseGenerator noiseGen = new PerlinNoiseGenerator();
            NoiseSettings settings = NoiseSettings.Default;
            int size = 25; // 24 segments
            float chunkSize = 240f;

            ChunkCoordinate chunkCenter = new ChunkCoordinate(0, 0);
            ChunkCoordinate chunkEast = new ChunkCoordinate(1, 0);
            ChunkCoordinate chunkNorth = new ChunkCoordinate(0, 1);

            HeightMap mapCenter = noiseGen.GenerateHeightMap(size, size, settings, chunkCenter);
            HeightMap mapEast = noiseGen.GenerateHeightMap(size, size, settings, chunkEast);
            HeightMap mapNorth = noiseGen.GenerateHeightMap(size, size, settings, chunkNorth);

            TerrainMeshData meshCenter = TerrainMeshBuilder.GenerateTerrainMesh(mapCenter, chunkSize, 30f, 1);
            TerrainMeshData meshEast = TerrainMeshBuilder.GenerateTerrainMesh(mapEast, chunkSize, 30f, 1);
            TerrainMeshData meshNorth = TerrainMeshBuilder.GenerateTerrainMesh(mapNorth, chunkSize, 30f, 1);

            Vector3 offsetCenter = chunkCenter.ToWorldPosition(chunkSize);
            Vector3 offsetEast = chunkEast.ToWorldPosition(chunkSize);
            Vector3 offsetNorth = chunkNorth.ToWorldPosition(chunkSize);

            // 1. Validate X-Axis Seam (Center East edge == East chunk West edge)
            for (int z = 0; z < size; z++)
            {
                int idxCenterEast = z * size + (size - 1);
                int idxEastWest = z * size + 0;

                Vector3 vertCenter = meshCenter.Vertices[idxCenterEast] + offsetCenter;
                Vector3 vertEast = meshEast.Vertices[idxEastWest] + offsetEast;

                Assert.AreEqual(vertCenter.x, vertEast.x, 1e-4f, $"X-alignment mismatch at line {z}");
                Assert.AreEqual(vertCenter.y, vertEast.y, 1e-4f, $"X-seam elevation mismatch at line {z}");
                Assert.AreEqual(vertCenter.z, vertEast.z, 1e-4f, $"X-seam Z-position mismatch at line {z}");
            }

            // 2. Validate Z-Axis Seam (Center North edge == North chunk South edge)
            for (int x = 0; x < size; x++)
            {
                int idxCenterNorth = (size - 1) * size + x;
                int idxNorthSouth = 0 * size + x;

                Vector3 vertCenter = meshCenter.Vertices[idxCenterNorth] + offsetCenter;
                Vector3 vertNorth = meshNorth.Vertices[idxNorthSouth] + offsetNorth;

                Assert.AreEqual(vertCenter.x, vertNorth.x, 1e-4f, $"Z-seam X-position mismatch at column {x}");
                Assert.AreEqual(vertCenter.y, vertNorth.y, 1e-4f, $"Z-seam elevation mismatch at column {x}");
                Assert.AreEqual(vertCenter.z, vertNorth.z, 1e-4f, $"Z-alignment mismatch at column {x}");
            }
        }
    }
}
