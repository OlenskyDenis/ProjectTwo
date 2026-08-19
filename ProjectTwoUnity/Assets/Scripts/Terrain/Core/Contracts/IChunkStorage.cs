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
        bool TryGetChunk(ChunkCoordinate coord, out HeightMap heightMap);

        /// <summary>
        /// Saves or updates chunk data in storage asynchronously.
        /// </summary>
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
