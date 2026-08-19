# Contract: `INoiseGenerator`

**Namespace**: `ProjectTwo.Terrain.Core.Contracts`  
**Purpose**: Pure algorithmic contract for evaluating procedural noise across 2D coordinates with multi-octave synthesis.

---

## C# Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# interface for procedural noise sampling algorithms.
    /// </summary>
    public interface INoiseGenerator
    {
        /// <summary>
        /// Generates a complete 2D heightmap matrix for a chunk coordinate based on provided noise settings.
        /// </summary>
        /// <param name="mapWidth">Width in grid samples.</param>
        /// <param name="mapHeight">Height in grid samples.</param>
        /// <param name="settings">Noise configuration parameters.</param>
        /// <param name="chunkCoord">Chunk grid coordinate.</param>
        /// <returns>Populated HeightMap instance.</returns>
        HeightMap GenerateHeightMap(int mapWidth, int mapHeight, NoiseSettings settings, ChunkCoordinate chunkCoord);

        /// <summary>
        /// Samples single-point 2D noise value at specific world coordinates.
        /// </summary>
        /// <param name="x">World X coordinate.</param>
        /// <param name="y">World Z/Y coordinate.</param>
        /// <param name="settings">Noise configuration parameters.</param>
        /// <returns>Normalized float value in range [0, 1].</returns>
        float SampleNoise(float x, float y, NoiseSettings settings);
    }
}
```
