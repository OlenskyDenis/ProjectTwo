namespace ProjectTwo.Terrain.Presentation.Pooling
{
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Components;

    /// <summary>
    /// Object pool for terrain chunk GameObjects to prevent runtime memory allocations and GC spikes.
    /// Uses transparent HideFlags.DontSave to prevent scene disk serialization while maintaining
    /// full Hierarchy observability for QA and developers in PlayMode (Constitution Principle IV & V).
    /// </summary>
    public class ChunkObjectPool
    {
        private readonly Transform _parent;
        private readonly Queue<TerrainChunkView> _pool = new Queue<TerrainChunkView>();
        private readonly Material _defaultMaterial;

        public ChunkObjectPool(Transform parent, Material customMaterial = null, int initialCapacity = 36)
        {
            _parent = parent;
            
            if (customMaterial != null)
            {
                _defaultMaterial = customMaterial;
            }
            else
            {
                Shader shader = Shader.Find("ProjectTwo/Terrain/VertexColorLit")
                             ?? Shader.Find("Universal Render Pipeline/Particles/Lit")
                             ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Sprites/Default");

                _defaultMaterial = new Material(shader) { name = "DefaultTerrainVertexMaterial" };
            }

            int capacity = Application.isPlaying ? initialCapacity : 0;
            for (int i = 0; i < capacity; i++)
            {
                TerrainChunkView chunk = CreateNewChunkInstance();
                chunk.ResetForPool();
                _pool.Enqueue(chunk);
            }
        }

        public TerrainChunkView GetChunk()
        {
            TerrainChunkView chunk = _pool.Count > 0 ? _pool.Dequeue() : CreateNewChunkInstance();
            if (chunk != null)
            {
                chunk.gameObject.SetActive(true);
            }
            return chunk;
        }

        public void ReturnChunk(TerrainChunkView chunk)
        {
            if (chunk == null) return;
            chunk.ResetForPool();
            _pool.Enqueue(chunk);
        }

        public TerrainChunkView CreateNewChunkInstance()
        {
            GameObject go = new GameObject("TerrainChunk", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(TerrainChunkView));
            go.hideFlags = HideFlags.DontSave;
            if (_parent != null)
            {
                go.transform.SetParent(_parent);
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _defaultMaterial;

            return go.GetComponent<TerrainChunkView>();
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                TerrainChunkView chunk = _pool.Dequeue();
                if (chunk != null && chunk.gameObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(chunk.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(chunk.gameObject);
                    }
                }
            }
        }
    }
}
