namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    /// <summary>
    /// Contract Reflection Test Suite guarding against architectural drift, stale method overloads,
    /// parameter bloat, and duplicate calculation pathways in violation of Constitution Principle I & VI.
    /// </summary>
    [TestFixture]
    public class ContractReflectionTests
    {
        [Test]
        public void ITerrainShaper_ContainsOnlyAuthoritativePipelineOverloads()
        {
            Type shaperInterface = typeof(ITerrainShaper);
            MethodInfo[] methods = shaperInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            // Filter by name
            MethodInfo[] calcMethods = methods.Where(m => m.Name == nameof(ITerrainShaper.CalculateElevation)).ToArray();
            MethodInfo[] genMethods = methods.Where(m => m.Name == nameof(ITerrainShaper.GenerateHeightMap)).ToArray();

            Assert.AreEqual(1, calcMethods.Length,
                "ITerrainShaper must define EXACTLY 1 authoritative CalculateElevation method (no legacy overloads).");
            Assert.AreEqual(1, genMethods.Length,
                "ITerrainShaper must define EXACTLY 1 authoritative GenerateHeightMap method (no legacy overloads).");

            // Verify that CalculateElevation uses TerrainShaperContext (3 parameters)
            ParameterInfo[] calcParams = calcMethods[0].GetParameters();
            Assert.AreEqual(3, calcParams.Length, "CalculateElevation must require exactly 3 parameters (worldX, worldZ, context).");
            Assert.IsTrue(calcParams.Any(p => p.ParameterType.IsByRef && p.ParameterType.GetElementType() == typeof(TerrainShaperContext) || p.ParameterType == typeof(TerrainShaperContext)),
                "CalculateElevation must take in TerrainShaperContext.");

            // Verify that GenerateHeightMap uses TerrainShaperContext (6 parameters)
            ParameterInfo[] genParams = genMethods[0].GetParameters();
            Assert.AreEqual(6, genParams.Length, "GenerateHeightMap must require exactly 6 parameters (startX, startZ, size, resolution, context, outputBuffer).");
            Assert.IsTrue(genParams.Any(p => p.ParameterType.IsByRef && p.ParameterType.GetElementType() == typeof(TerrainShaperContext) || p.ParameterType == typeof(TerrainShaperContext)),
                "GenerateHeightMap must take in TerrainShaperContext.");
            Assert.IsTrue(genParams.Any(p => p.ParameterType == typeof(float[,])), "GenerateHeightMap must take float[,] outputBuffer.");
        }

        [Test]
        public void HeightMapBuilder_AdheresToDependencyInversionAndSinglePipeline()
        {
            Type builderType = typeof(HeightMapBuilder);
            ConstructorInfo[] constructors = builderType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            // Assert exactly one constructor accepting ITerrainShaper
            Assert.AreEqual(1, constructors.Length, "HeightMapBuilder must define exactly one explicit constructor.");
            ParameterInfo[] ctorParams = constructors[0].GetParameters();
            Assert.AreEqual(1, ctorParams.Length, "HeightMapBuilder constructor must take exactly one parameter.");
            Assert.AreEqual(typeof(ITerrainShaper), ctorParams[0].ParameterType, "HeightMapBuilder must depend on ITerrainShaper abstraction.");

            // Assert no legacy compound overloads or raw noise bypass methods
            MethodInfo[] methods = builderType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodInfo[] compoundMethods = methods.Where(m => m.Name == nameof(HeightMapBuilder.GenerateCompoundHeightMap)).ToArray();

            Assert.AreEqual(1, compoundMethods.Length,
                "HeightMapBuilder must expose exactly 1 authoritative GenerateCompoundHeightMap method.");
            Assert.AreEqual(5, compoundMethods[0].GetParameters().Length,
                "GenerateCompoundHeightMap must accept 5 parameters (startX, startZ, size, resolution, in context).");

            // Verify no obsolete raw noise bypass
            Assert.IsFalse(methods.Any(m => m.Name == "GenerateHeightMap" && m.GetParameters().Length == 4),
                "HeightMapBuilder must not expose a raw noise bypass method violating the single calculation pipeline.");
        }

        [Test]
        public void DomainContracts_ZeroStaleOverloads_EnforcedAcrossAssembly()
        {
            Assembly coreAssembly = typeof(ITerrainShaper).Assembly;
            Type[] interfaceTypes = coreAssembly.GetTypes().Where(t => t.IsInterface).ToArray();

            foreach (Type iface in interfaceTypes)
            {
                MethodInfo[] methods = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    Assert.IsFalse(method.GetCustomAttributes(typeof(ObsoleteAttribute), false).Any(),
                        $"Domain interface '{iface.Name}.{method.Name}' contains an [Obsolete] method. Stale contracts must be eliminated per Principle VI.");
                }
            }
        }
    }
}
