namespace ProjectTwo.Terrain.Presentation.Materials
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Thread-safe in-memory cache and lifecycle manager for shared terrain and water runtime materials.
    /// </summary>
    public sealed class TerrainMaterialCache : IMaterialCache
    {
        private readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly object _lock = new object();
        private bool _disposed;

        /// <inheritdoc />
        public Material GetOrAdd(string key, Func<Material> factory)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(TerrainMaterialCache));

                if (_cache.TryGetValue(key, out Material existing) && existing != null)
                {
                    return existing;
                }

                Material created = factory();
                if (created != null)
                {
                    _cache[key] = created;
                }
                return created;
            }
        }

        /// <inheritdoc />
        public bool TryGet(string key, out Material material)
        {
            if (string.IsNullOrEmpty(key))
            {
                material = null;
                return false;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    material = null;
                    return false;
                }

                if (_cache.TryGetValue(key, out Material existing) && existing != null)
                {
                    material = existing;
                    return true;
                }

                material = null;
                return false;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_lock)
            {
                foreach (KeyValuePair<string, Material> kvp in _cache)
                {
                    Material mat = kvp.Value;
                    if (mat != null)
                    {
                        DestroyMaterialSafely(mat);
                    }
                }
                _cache.Clear();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                Clear();
                _disposed = true;
            }
        }

        private static void DestroyMaterialSafely(Material mat)
        {
            if (mat == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(mat);
                return;
            }
#endif
            UnityEngine.Object.Destroy(mat);
        }
    }
}
