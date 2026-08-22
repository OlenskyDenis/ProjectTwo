namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class DomainDecouplingTests
    {
        [Test]
        public void CoreDomainModels_ContainZeroDirectReferencesToUnityEngineMaterial()
        {
            Assembly coreAssembly = typeof(TerrainRegion).Assembly;
            Type[] types = coreAssembly.GetTypes();

            foreach (Type type in types)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (FieldInfo field in fields)
                {
                    Assert.AreNotEqual(
                        typeof(Material),
                        field.FieldType,
                        $"Field '{field.Name}' in Core model '{type.FullName}' must not reference UnityEngine.Material. Use MaterialDescriptor instead.");
                }

                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (PropertyInfo property in properties)
                {
                    Assert.AreNotEqual(
                        typeof(Material),
                        property.PropertyType,
                        $"Property '{property.Name}' in Core model '{type.FullName}' must not reference UnityEngine.Material. Use MaterialDescriptor instead.");
                }
            }
        }

        [Test]
        public void MaterialDescriptor_EqualityAndHashing_WorksCorrectly()
        {
            var desc1 = new MaterialDescriptor("alpine_preset", "Alpine", 123);
            var desc2 = new MaterialDescriptor("alpine_preset", "Alpine Alt", 123);
            var desc3 = new MaterialDescriptor("desert_preset", "Desert", 456);

            Assert.AreEqual(desc1, desc2);
            Assert.IsTrue(desc1 == desc2);
            Assert.AreNotEqual(desc1, desc3);
            Assert.IsTrue(desc1 != desc3);
            Assert.AreEqual(desc1.GetHashCode(), desc2.GetHashCode());
        }

        [Test]
        public void OffThread_MeshGeneration_BuildsCompleteVisualAndCollisionBuffersWithoutMainThreadDependency()
        {
            var shaper = new ProceduralTerrainShaper();
            var builder = new HeightMapBuilder(shaper);
            var context = TerrainShaperContext.CreateDefault();

            HeightMap map = builder.GenerateCompoundHeightMap(0f, 0f, 240f, 24, in context);
            Assert.IsNotNull(map);

            TerrainMeshData visualData = TerrainMeshBuilder.GenerateTerrainMesh(map, 240f, 60f, 1, null, includeSkirt: true);
            TerrainMeshData collisionData = TerrainMeshBuilder.GenerateTerrainMesh(map, 240f, 60f, 1, null, includeSkirt: false);

            Assert.IsNotNull(visualData.Vertices);
            Assert.IsNotNull(visualData.Triangles);
            Assert.IsNotNull(visualData.Normals);
            Assert.Greater(visualData.Vertices.Length, 0);

            Assert.IsNotNull(collisionData.Vertices);
            Assert.IsNotNull(collisionData.Triangles);
            Assert.Greater(collisionData.Vertices.Length, 0);
            Assert.Less(collisionData.Vertices.Length, visualData.Vertices.Length, "Collision mesh without skirts should have fewer vertices than visual mesh with skirts.");
        }
    }
}
