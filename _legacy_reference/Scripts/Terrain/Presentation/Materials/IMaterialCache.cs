namespace ProjectTwo.Terrain.Presentation.Materials
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Thread-safe in-memory cache for managing shared runtime material instances and their lifecycles.
    /// </summary>
    public interface IMaterialCache : IDisposable
    {
        /// <summary>
        /// Gets an existing cached material or generates and caches a new one using the provided factory method.
        /// </summary>
        /// <param name="key">Unique deterministic cache key.</param>
        /// <param name="factory">Factory method creating the Material if not present in cache.</param>
        /// <returns>The shared Material instance.</returns>
        Material GetOrAdd(string key, Func<Material> factory);

        /// <summary>
        /// Attempts to get a material by cache key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="material">The resulting Material if found.</param>
        /// <returns>True if key exists in cache; otherwise false.</returns>
        bool TryGet(string key, out Material material);

        /// <summary>
        /// Clears all entries from the cache and disposes generated material assets to prevent memory leaks.
        /// </summary>
        void Clear();
    }
}
