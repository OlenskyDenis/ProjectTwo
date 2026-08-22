namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Presentation.Components;

    [TestFixture]
    public class DomainModelTests
    {
        [Test]
        public void TerrainShaperContext_DefaultInitialization_SetsAllProperties()
        {
            var ctx = TerrainShaperContext.CreateDefault();

            Assert.IsNotNull(ctx.Noise);
            Assert.IsNotNull(ctx.Macro);
            Assert.IsNotNull(ctx.Tectonics);
            Assert.IsNotNull(ctx.HeightCurve);
            Assert.IsNotNull(ctx.Water);
            Assert.IsNotNull(ctx.River);
            Assert.IsNotNull(ctx.Hydrology);
            Assert.IsNotNull(ctx.RiverGraph);
            Assert.IsNotNull(ctx.Falloff);
        }

        [Test]
        public void ChunkGenerationPayload_Initialization_PreservesProperties()
        {
            var coord = new ChunkCoordinate(2, 3);
            var heightMap = new HeightMap(new float[4, 4]);
            var visualData = new TerrainMeshData(4, 6);
            var collisionData = new TerrainMeshData(4, 6);
            var riverData = RiverWaterMeshData.Empty;

            var payload = new ChunkGenerationPayload(
                coord,
                heightMap,
                visualData,
                collisionData,
                riverData,
                targetLOD: 0,
                hasCollider: true);

            Assert.AreEqual(coord, payload.Coordinate);
            Assert.AreEqual(heightMap, payload.HeightMap);
            Assert.AreEqual(visualData, payload.VisualMeshData);
            Assert.AreEqual(collisionData, payload.CollisionMeshData);
            Assert.AreEqual(riverData, payload.RiverMeshData);
            Assert.AreEqual(0, payload.TargetLOD);
            Assert.IsTrue(payload.HasCollider);
        }

        [Test]
        public void NoiseSettings_Validate_ClampsInvalidParameters()
        {
            NoiseSettings settings = new NoiseSettings
            {
                Scale = -10f,
                Octaves = 0,
                Persistence = -0.5f,
                Lacunarity = 0.5f,
                HeightMultiplier = -5f
            };

            settings.Validate();

            Assert.GreaterOrEqual(settings.Scale, 0.001f);
            Assert.AreEqual(1, settings.Octaves);
            Assert.AreEqual(0.01f, settings.Persistence);
            Assert.AreEqual(1f, settings.Lacunarity);
            Assert.AreEqual(0f, settings.HeightMultiplier);

            settings.Octaves = 100;
            settings.Persistence = 5f;
            settings.Validate();

            Assert.AreEqual(8, settings.Octaves);
            Assert.AreEqual(1f, settings.Persistence);
        }

        [Test]
        public void NoiseSettings_EqualityAndHashCode_FunctionProperly()
        {
            NoiseSettings s1 = NoiseSettings.Default;
            NoiseSettings s2 = NoiseSettings.Default;
            NoiseSettings s3 = NoiseSettings.Default;
            s3.Seed = 999;

            Assert.IsTrue(s1.Equals(s2));
            Assert.IsFalse(s1.Equals(s3));
            Assert.AreEqual(s1.GetHashCode(), s2.GetHashCode());
            Assert.IsTrue(s1.Equals((object)s2));
            Assert.IsFalse(s1.Equals(null));
        }

        [Test]
        public void LODInfo_CreateDefaultTiers_ReturnsValidConfiguration()
        {
            LODInfo[] tiers = LODInfo.CreateDefaultTiers(800f);

            Assert.IsNotNull(tiers);
            Assert.AreEqual(4, tiers.Length);
            Assert.AreEqual(0, tiers[0].LodIndex);
            Assert.AreEqual(1, tiers[0].MeshResolutionStep);
            Assert.IsTrue(tiers[0].HasCollider);
            Assert.IsFalse(tiers[1].HasCollider);
            Assert.AreEqual(800f, tiers[3].VisibleDistanceThreshold);
        }

        [Test]
        public void LODInfo_Constructor_ClampsResolutionStep()
        {
            LODInfo lod = new LODInfo(0, 100f, 0, true);
            Assert.AreEqual(1, lod.MeshResolutionStep);
        }

        [Test]
        public void TerrainRegion_CreateDefaultRegions_ReturnsPopulatedBands()
        {
            TerrainRegion[] regions = TerrainRegion.CreateDefaultRegions();

            Assert.IsNotNull(regions);
            Assert.GreaterOrEqual(regions.Length, 5);
            Assert.AreEqual("Deep Water", regions[0].Name);
            Assert.AreEqual(1.0f, regions[regions.Length - 1].HeightThreshold);
        }

        [Test]
        public void HeightMap_GetNormalizedValue_ClampsOutOfBoundsIndices()
        {
            float[,] data = new float[4, 4];
            data[0, 0] = 0.2f;
            data[3, 3] = 0.8f;
            HeightMap heightMap = new HeightMap(data);

            Assert.AreEqual(0.2f, heightMap.GetNormalizedValue(-2, -5));
            Assert.AreEqual(0.8f, heightMap.GetNormalizedValue(10, 20));
        }

        [Test]
        public void HeightMap_Constructor_ThrowsOnNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => new HeightMap(null));
        }

        [Test]
        public void TerrainMeshData_CreateMeshAndApplyToMesh_BehavesCorrectly()
        {
            TerrainMeshData meshData = new TerrainMeshData(4, 6);
            meshData.Vertices[0] = new Vector3(0, 0, 0);
            meshData.Vertices[1] = new Vector3(1, 0, 0);
            meshData.Vertices[2] = new Vector3(0, 0, 1);
            meshData.Vertices[3] = new Vector3(1, 0, 1);

            meshData.AddTriangle(0, 2, 1);
            meshData.AddTriangle(2, 3, 1);

            // Adding beyond capacity safely ignored
            meshData.AddTriangle(0, 1, 2);

            Mesh mesh = meshData.CreateMesh();
            Assert.IsNotNull(mesh);
            Assert.AreEqual(4, mesh.vertexCount);

            Mesh targetMesh = new Mesh();
            meshData.ApplyToMesh(targetMesh);
            Assert.AreEqual(4, targetMesh.vertexCount);

            // Null apply does not throw
            Assert.DoesNotThrow(() => meshData.ApplyToMesh(null));
        }

        [Test]
        public void ChunkObjectPool_Instances_AreMarkedWithDontSave_ToPreventSceneDiskPollution_WhileMaintainingObservability()
        {
            GameObject parentGo = new GameObject("TestTerrainPoolParent");
            try
            {
                var pool = new ProjectTwo.Terrain.Presentation.Pooling.ChunkObjectPool(parentGo.transform, null, 2);
                var chunk = pool.GetChunk();
                Assert.IsNotNull(chunk);
                Assert.AreEqual(HideFlags.DontSave, chunk.gameObject.hideFlags,
                    "Chunk GameObjects must be marked with HideFlags.DontSave to prevent saving to scene disk while maintaining Hierarchy observability in PlayMode.");
                pool.Clear();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parentGo);
            }
        }
    }
}
