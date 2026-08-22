namespace ProjectTwo.Terrain.Core.Services
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe in-memory cache storage for terrain chunks.
    /// </summary>
    public class MemoryChunkStorage : IChunkStorage
    {
        private readonly ConcurrentDictionary<ChunkCoordinate, HeightMap> _cache = new ConcurrentDictionary<ChunkCoordinate, HeightMap>();

        public bool TryGetChunk(ChunkCoordinate coord, out HeightMap heightMap)
        {
            return _cache.TryGetValue(coord, out heightMap);
        }

        public Task SaveChunkAsync(ChunkCoordinate coord, HeightMap heightMap)
        {
            if (heightMap != null)
            {
                _cache[coord] = heightMap;
            }
            return Task.CompletedTask;
        }

        public bool ContainsChunk(ChunkCoordinate coord)
        {
            return _cache.ContainsKey(coord);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
