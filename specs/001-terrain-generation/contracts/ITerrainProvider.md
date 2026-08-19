# Contract: `ITerrainProvider`

**Namespace**: `ProjectTwo.Terrain.Core.Contracts`  
**Purpose**: Primary public decoupled interface for spatial queries and chunk lifecycle notifications consumed by downstream world-population modules (vegetation spawners, building placers, bridge builders, pathfinders).

---

## C# Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Context payload emitted when a terrain chunk is loaded or regenerated.
    /// </summary>
    public readonly struct ChunkEventArgs
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public Vector3 WorldOrigin { get; }
        public float ChunkSize { get; }
        public int ActiveLOD { get; }

        public ChunkEventArgs(int chunkX, int chunkZ, Vector3 worldOrigin, float chunkSize, int activeLOD)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            WorldOrigin = worldOrigin;
            ChunkSize = chunkSize;
            ActiveLOD = activeLOD;
        }
    }

    /// <summary>
    /// Main provider contract for terrain spatial queries and events.
    /// </summary>
    public interface ITerrainProvider
    {
        /// <summary>
        /// Event fired whenever a new terrain chunk completes asynchronous generation and enters the active scene.
        /// </summary>
        event Action<ChunkEventArgs> OnChunkLoaded;

        /// <summary>
        /// Event fired whenever an out-of-range terrain chunk is unloaded or recycled.
        /// </summary>
        event Action<ChunkEventArgs> OnChunkUnloaded;

        /// <summary>
        /// Event fired when global terrain generation completes (e.g. editor regeneration).
        /// </summary>
        event Action OnTerrainRegenerated;

        /// <summary>
        /// Queries the interpolated terrain elevation at a specific world (X, Z) coordinate.
        /// </summary>
        /// <param name="worldX">World X position.</param>
        /// <param name="worldZ">World Z position.</param>
        /// <returns>World-space Y elevation.</returns>
        float GetHeight(float worldX, float worldZ);

        /// <summary>
        /// Queries the surface normal vector at a specific world (X, Z) coordinate.
        /// </summary>
        /// <param name="worldX">World X position.</param>
        /// <param name="worldZ">World Z position.</param>
        /// <returns>Normalized surface normal vector.</returns>
        Vector3 GetNormal(float worldX, float worldZ);

        /// <summary>
        /// Queries the surface steepness angle (slope in degrees from 0 to 90) at a world coordinate.
        /// </summary>
        /// <param name="worldX">World X position.</param>
        /// <param name="worldZ">World Z position.</param>
        /// <returns>Slope angle in degrees (0 = flat, 90 = vertical cliff).</returns>
        float GetSlope(float worldX, float worldZ);

        /// <summary>
        /// Queries the name of the active biome/region at a specific world coordinate.
        /// </summary>
        /// <param name="worldX">World X position.</param>
        /// <param name="worldZ">World Z position.</param>
        /// <returns>Biome name (e.g. "Grass", "Water", "Rock", "Snow").</returns>
        string GetBiomeAt(float worldX, float worldZ);

        /// <summary>
        /// Checks if a world coordinate is within currently loaded and active chunk boundaries.
        /// </summary>
        /// <param name="worldX">World X position.</param>
        /// <param name="worldZ">World Z position.</param>
        /// <returns>True if loaded, false otherwise.</returns>
        bool IsPositionLoaded(float worldX, float worldZ);
    }
}
```
