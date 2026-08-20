namespace ProjectTwo.Terrain.Presentation.Components
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    /// <summary>
    /// Presentation component managing the GameObject, MeshFilter, MeshRenderer, and MeshCollider for a chunk.
    /// Completely decouples visual rendering mesh (with seamless skirts) from physical collision mesh
    /// (pure surface grid without underground skirts) to eliminate PhysX large-triangle warnings.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunkView : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _visualMesh;
        private Mesh _collisionMesh;

        public ChunkCoordinate Coordinate { get; private set; }
        public HeightMap HeightMap { get; private set; }
        public int CurrentLOD { get; private set; } = -1;

        private float _chunkSize;
        private float _heightMultiplier;
        private LODInfo[] _lodTiers;
        private TerrainRegion[] _regions;

        private static Material _cachedDefaultMaterial;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshCollider != null)
            {
                _meshCollider.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning |
                                               MeshColliderCookingOptions.WeldColocatedVertices |
                                               MeshColliderCookingOptions.CookForFasterSimulation;
                _meshCollider.enabled = false;
                _meshCollider.sharedMesh = null;
            }

            _visualMesh = new Mesh { name = "TerrainVisualMesh" };
            _collisionMesh = new Mesh { name = "TerrainCollisionMesh" };
            _meshFilter.sharedMesh = _visualMesh;
        }

        public void Initialize(
            ChunkCoordinate coordinate,
            HeightMap heightMap,
            float chunkSize,
            float heightMultiplier,
            LODInfo[] lodTiers,
            TerrainRegion[] regions,
            Material material)
        {
            Coordinate = coordinate;
            HeightMap = heightMap;
            _chunkSize = chunkSize;
            _heightMultiplier = heightMultiplier;
            _lodTiers = lodTiers;
            _regions = regions;

            if (_meshRenderer != null)
            {
                if (material != null)
                {
                    _meshRenderer.sharedMaterial = material;
                }
                else
                {
                    if (_cachedDefaultMaterial == null)
                    {
                        Shader shader = Shader.Find("ProjectTwo/Terrain/VertexColorLit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                        if (shader != null)
                        {
                            _cachedDefaultMaterial = new Material(shader) { name = "DefaultTerrainVertexColorMat" };
                        }
                    }

                    if (_cachedDefaultMaterial != null)
                    {
                        _meshRenderer.sharedMaterial = _cachedDefaultMaterial;
                    }
                }
            }

            transform.position = coordinate.ToWorldPosition(chunkSize);
            CurrentLOD = -1;
        }

        public void UpdateLOD(float distanceToViewer)
        {
            if (_lodTiers == null || _lodTiers.Length == 0)
            {
                if (CurrentLOD != 0)
                {
                    SetLOD(0, 1, Application.isPlaying);
                }
                return;
            }

            int targetLod = _lodTiers.Length - 1;
            for (int i = 0; i < _lodTiers.Length; i++)
            {
                if (distanceToViewer <= _lodTiers[i].VisibleDistanceThreshold)
                {
                    targetLod = i;
                    break;
                }
            }

            if (targetLod != CurrentLOD)
            {
                LODInfo tier = _lodTiers[targetLod];
                SetLOD(tier.LodIndex, tier.MeshResolutionStep, tier.HasCollider && Application.isPlaying);
            }
        }

        private void SetLOD(int lodIndex, int resolutionStep, bool enableCollider)
        {
            CurrentLOD = lodIndex;

            if (HeightMap == null) return;

            if (_visualMesh == null)
            {
                _visualMesh = new Mesh { name = "TerrainVisualMesh" };
                if (_meshFilter != null) _meshFilter.sharedMesh = _visualMesh;
            }

            // 1. Build and apply Visual Mesh (with skirts to seal boundary cracks)
            TerrainMeshData visualData = TerrainMeshBuilder.GenerateTerrainMesh(
                HeightMap,
                _chunkSize,
                _heightMultiplier,
                resolutionStep,
                _regions,
                includeSkirt: true);

            visualData.ApplyToMesh(_visualMesh);

            // 2. Build and apply Physics Collision Mesh (pure surface grid without underground skirts)
            if (_meshCollider != null)
            {
                if (Application.isPlaying && enableCollider && lodIndex == 0)
                {
                    if (_collisionMesh == null)
                    {
                        _collisionMesh = new Mesh { name = "TerrainCollisionMesh" };
                    }

                    TerrainMeshData collisionData = TerrainMeshBuilder.GenerateTerrainMesh(
                        HeightMap,
                        _chunkSize,
                        _heightMultiplier,
                        lodStep: 1,
                        regions: null,
                        includeSkirt: false);

                    collisionData.ApplyToMesh(_collisionMesh);

                    _meshCollider.sharedMesh = null;
                    _meshCollider.sharedMesh = _collisionMesh;
                    _meshCollider.enabled = true;
                }
                else
                {
                    if (_meshCollider.enabled)
                    {
                        _meshCollider.enabled = false;
                    }
                    _meshCollider.sharedMesh = null;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ResetForPool()
        {
            CurrentLOD = -1;
            HeightMap = null;
            if (_meshCollider != null)
            {
                _meshCollider.enabled = false;
                _meshCollider.sharedMesh = null;
            }
            gameObject.SetActive(false);
        }
    }
}
