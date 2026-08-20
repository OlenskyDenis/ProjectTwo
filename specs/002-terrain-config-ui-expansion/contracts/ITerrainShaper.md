# Contract: `ITerrainShaper`

**Namespace**: `ProjectTwo.Terrain.Core.Contracts`  
**Purpose**: Pure C# domain contract for procedural elevation calculation, multi-algorithm noise evaluation, macro masking, elevation remapping, and river carving.

---

## Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe mathematical service calculating compound procedural elevation and biome weightings.
    /// </summary>
    public interface ITerrainShaper
    {
        /// <summary>
        /// Calculates the final composite world elevation at 2D world coordinates (x, z).
        /// Incorporates macro mask, noise type, river carving, water basins, and height curves.
        /// </summary>
        /// <param name="worldX">World X coordinate.</param>
        /// <param name="worldZ">World Z coordinate.</param>
        /// <param name="config">Terrain configuration snapshot.</param>
        /// <returns>Calculated world height (Y coordinate).</returns>
        float CalculateElevation(float worldX, float worldZ, TerrainDataConfig config);

        /// <summary>
        /// Populates a 2D float array with compound elevations for a chunk bounding box.
        /// </summary>
        /// <param name="startX">World starting X.</param>
        /// <param name="startZ">World starting Z.</param>
        /// <param name="size">Chunk world size.</param>
        /// <param name="resolution">Number of segments per edge.</param>
        /// <param name="config">Terrain configuration snapshot.</param>
        /// <param name="outputBuffer">Target 2D heightmap buffer [(resolution + 1), (resolution + 1)].</param>
        void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            TerrainDataConfig config,
            float[,] outputBuffer);
    }
}
```

---

## Behavioral Rules & Invariants

1. **Deterministic Execution**: Given identical `(worldX, worldZ)` coordinates and config values, `CalculateElevation` must yield bit-exact identical heights.
2. **Zero Allocations in Steady-State**: `GenerateHeightMap` must write directly into caller-provided `outputBuffer` without heap allocations.
3. **Thread Safety**: Can be called concurrently by multiple worker threads.
