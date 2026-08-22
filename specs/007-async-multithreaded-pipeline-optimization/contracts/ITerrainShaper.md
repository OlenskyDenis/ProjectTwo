# Contract: ITerrainShaper

**Interface**: `ProjectTwo.Terrain.Core.Contracts.ITerrainShaper`
**Namespace**: `ProjectTwo.Terrain.Core.Contracts`

## Overview
The `ITerrainShaper` defines the mathematical continuous elevation and heightmap service. It incorporates multi-octave noise, macro landmass masks, tectonic boundaries, and river graph network carving.

## Refactored Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe mathematical service calculating compound procedural elevation and heightmaps.
    /// Incorporates noise types, macro continental masks, tectonics, river carving, water basins, and elevation curves.
    /// Strictly adheres to Constitution Principle VI (Single Authoritative Pipeline) and clean parameter encapsulation.
    /// </summary>
    public interface ITerrainShaper
    {
        /// <summary>
        /// Calculates the final composite world elevation at (worldX, worldZ) using the provided generation context.
        /// </summary>
        /// <param name="worldX">World space X coordinate in meters.</param>
        /// <param name="worldZ">World space Z coordinate in meters.</param>
        /// <param name="context">Encapsulated domain settings context.</param>
        /// <returns>World elevation in meters.</returns>
        float CalculateElevation(
            float worldX,
            float worldZ,
            in TerrainShaperContext context);

        /// <summary>
        /// Populates a 2D float array with compound normalized elevations [0..1] for a specified chunk boundary.
        /// </summary>
        /// <param name="startX">World space origin X in meters.</param>
        /// <param name="startZ">World space origin Z in meters.</param>
        /// <param name="size">Spatial width/depth of the chunk in meters.</param>
        /// <param name="resolution">Number of quad segments along one dimension.</param>
        /// <param name="context">Encapsulated domain settings context.</param>
        /// <param name="outputBuffer">Pre-allocated 2D array [res+1, res+1] for normalized elevations.</param>
        void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            in TerrainShaperContext context,
            float[,] outputBuffer);
    }
}
```
