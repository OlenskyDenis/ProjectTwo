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
    /// and duplicate calculation pathways in violation of Constitution Principle I & VI.
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

            // Verify that CalculateElevation has all 12 parameters (including Tectonics & Hydrology)
            ParameterInfo[] calcParams = calcMethods[0].GetParameters();
            Assert.AreEqual(12, calcParams.Length, "CalculateElevation must require all 12 domain parameters.");
            Assert.IsTrue(calcParams.Any(p => p.ParameterType == typeof(TectonicSettings)), "CalculateElevation must take TectonicSettings.");
            Assert.IsTrue(calcParams.Any(p => p.ParameterType == typeof(TectonicBoundary[])), "CalculateElevation must take TectonicBoundary[].");
            Assert.IsTrue(calcParams.Any(p => p.ParameterType == typeof(HydrologySettings)), "CalculateElevation must take HydrologySettings.");
            Assert.IsTrue(calcParams.Any(p => p.ParameterType == typeof(RiverGraph)), "CalculateElevation must take RiverGraph.");

            // Verify that GenerateHeightMap has all 15 parameters
            ParameterInfo[] genParams = genMethods[0].GetParameters();
            Assert.AreEqual(15, genParams.Length, "GenerateHeightMap must require all 15 domain parameters.");
            Assert.IsTrue(genParams.Any(p => p.ParameterType == typeof(TectonicSettings)), "GenerateHeightMap must take TectonicSettings.");
            Assert.IsTrue(genParams.Any(p => p.ParameterType == typeof(HydrologySettings)), "GenerateHeightMap must take HydrologySettings.");
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
            Assert.AreEqual(14, compoundMethods[0].GetParameters().Length,
                "GenerateCompoundHeightMap must accept the full 14 parameters (start coords, size, resolution, plus all domain settings).");

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
