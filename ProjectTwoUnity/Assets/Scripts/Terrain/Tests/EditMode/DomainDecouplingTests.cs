namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

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
    }
}
