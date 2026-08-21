# Core Contracts Specification & Single Pipeline Guarantees

## 1. `ITerrainShaper` (Authoritative Contract)

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe mathematical service calculating compound procedural elevation and heightmaps.
    /// Incorporates noise types, macro continental masks, tectonics, river carving, water basins, and elevation curves.
    /// </summary>
    public interface ITerrainShaper
    {
        /// <summary>
        /// Calculates the final composite world elevation incorporating global tectonics and river network graph.
        /// </summary>
        float CalculateElevation(
            float worldX,
            float worldZ,
            NoiseSettings noise,
            MacroMaskSettings macro,
            TectonicSettings tectonics,
            TectonicBoundary[] tectonicBoundaries,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            HydrologySettings hydrology,
            RiverGraph riverGraph,
            FalloffSettings falloff);

        /// <summary>
        /// Populates a 2D float array with compound elevations incorporating global tectonics and river network graph.
        /// </summary>
        void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            NoiseSettings noise,
            MacroMaskSettings macro,
            TectonicSettings tectonics,
            TectonicBoundary[] tectonicBoundaries,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            HydrologySettings hydrology,
            RiverGraph riverGraph,
            FalloffSettings falloff,
            float[,] outputBuffer);
    }
}
```

### Prohibited Stale Overloads
- `CalculateElevation(float, float, NoiseSettings, MacroMaskSettings, HeightCurveSettings, WaterSettings, RiverSettings, FalloffSettings)` — **PROHIBITED** (bypasses tectonics and hydrology).
- `GenerateHeightMap(float, float, float, int, NoiseSettings, MacroMaskSettings, HeightCurveSettings, WaterSettings, RiverSettings, FalloffSettings, float[,])` — **PROHIBITED** (bypasses tectonics and hydrology).

---

## 2. `IContractReflectionGuard` (Test Contract)

```csharp
namespace ProjectTwo.Terrain.Tests.EditMode
{
    /// <summary>
    /// Contract test fixture enforcing Constitution Principle VI and public API purity via reflection.
    /// </summary>
    public interface IContractReflectionGuard
    {
        void AssertNoStaleMethodOverloadsOnContract(System.Type contractType);
        void AssertSingleAuthoritativeCalculationPipeline();
        void AssertAllDomainContractsFrozen();
    }
}
```
