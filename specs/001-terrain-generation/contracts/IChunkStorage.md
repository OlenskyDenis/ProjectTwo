# Contract: `IChunkStorage`

**Namespace**: `ProjectTwo.Terrain.Core.Contracts`  
**Purpose**: Storage interface contract for saving, caching, and retrieving serialized chunk elevation/state data.

---

## C# Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using System.Threading.Tasks;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Contract for persisting and retrieving terrain chunk data.
    /// </summary>
    public interface IChunkStorage
    {
        /// <summary>
        /// Attempts to load cached chunk data for a specific coordinate.
        /// </summary>
        /// <param name="coord">Chunk coordinate key.</param>
        /// <param name="heightMap">Output heightmap if found.</param>
        /// <returns>True if chunk exists in cache/storage, false otherwise.</returns>
        bool TryGetChunk(ChunkCoordinate coord, out HeightMap heightMap);

        /// <summary>
        /// Saves or updates chunk data in storage asynchronously.
        /// </summary>
        /// <param name="coord">Chunk coordinate key.</param>
        /// <param name="heightMap">Heightmap data to store.</param>
        /// <returns>Task representing completion.</returns>
        Task SaveChunkAsync(ChunkCoordinate coord, HeightMap heightMap);

        /// <summary>
        /// Checks if a chunk at the specified coordinate exists in storage.
        /// </summary>
        bool ContainsChunk(ChunkCoordinate coord);

        /// <summary>
        /// Clears all cached chunks from memory and storage.
        /// </summary>
        void Clear();
    }
}
```
