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

        private MeshFilter _riverMeshFilter;
        private MeshRenderer _riverMeshRenderer;
        private Mesh _riverMesh;

        public ChunkCoordinate Coordinate { get; private set; }
        public HeightMap HeightMap { get; private set; }
        public int CurrentLOD { get; private set; } = -1;

        private float _chunkSize;
        private float _heightMultiplier;
        private LODInfo[] _lodTiers;
        private TerrainRegion[] _regions;

        private static Material _cachedDefaultMaterial;
        private static Material _cachedDefaultRiverMaterial;

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

        public void SetRiverMesh(RiverWaterMeshData riverMeshData, Material riverMaterial = null)
        {
            if (riverMeshData == null || riverMeshData.IsEmpty)
            {
                if (_riverMeshFilter != null && _riverMeshFilter.gameObject.activeSelf)
                {
                    _riverMeshFilter.gameObject.SetActive(false);
                }
                return;
            }

            if (_riverMeshFilter == null)
            {
                var riverChild = new GameObject("RiverWaterMesh");
                riverChild.transform.SetParent(transform, false);
                _riverMeshFilter = riverChild.AddComponent<MeshFilter>();
                _riverMeshRenderer = riverChild.AddComponent<MeshRenderer>();
                _riverMesh = new Mesh { name = "ChunkRiverWaterMesh" };
                _riverMeshFilter.sharedMesh = _riverMesh;
            }

            _riverMeshFilter.gameObject.SetActive(true);

            if (_riverMesh == null)
            {
                _riverMesh = new Mesh { name = "ChunkRiverWaterMesh" };
                _riverMeshFilter.sharedMesh = _riverMesh;
            }

            _riverMesh.Clear();
            _riverMesh.vertices = riverMeshData.Vertices;
            _riverMesh.normals = riverMeshData.Normals;
            _riverMesh.uv = riverMeshData.UVs;
            _riverMesh.triangles = riverMeshData.Triangles;
            _riverMesh.RecalculateBounds();

            if (_riverMeshRenderer != null)
            {
                if (riverMaterial != null)
                {
                    _riverMeshRenderer.sharedMaterial = riverMaterial;
                }
                else
                {
                    if (_cachedDefaultRiverMaterial == null || _cachedDefaultRiverMaterial.shader == null || !_cachedDefaultRiverMaterial.shader.isSupported)
                    {
                        Shader waterShader = Shader.Find("ProjectTwo/Terrain/WaterSimple");
                        if (waterShader == null || !waterShader.isSupported)
                        {
                            waterShader = Shader.Find("Universal Render Pipeline/Lit") ??
                                         Shader.Find("Universal Render Pipeline/Simple Lit") ??
                                         Shader.Find("ProjectTwo/Terrain/VertexColorLit") ??
                                         Shader.Find("Standard") ??
                                         Shader.Find("Sprites/Default") ??
                                         Shader.Find("Unlit/Color");
                        }
                        if (waterShader != null)
                        {
                            _cachedDefaultRiverMaterial = new Material(waterShader) { name = "DefaultRiverWaterMat" };
                            Color riverColor = new Color(0.15f, 0.48f, 0.85f, 0.85f);
                            if (_cachedDefaultRiverMaterial.HasProperty("_BaseColor"))
                            {
                                _cachedDefaultRiverMaterial.SetColor("_BaseColor", riverColor);
                            }
                            if (_cachedDefaultRiverMaterial.HasProperty("_Color"))
                            {
                                _cachedDefaultRiverMaterial.SetColor("_Color", riverColor);
                            }
                            _cachedDefaultRiverMaterial.color = riverColor;
                        }
                    }

                    if (_cachedDefaultRiverMaterial != null)
                    {
                        _riverMeshRenderer.sharedMaterial = _cachedDefaultRiverMaterial;
                    }
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
            if (_riverMeshFilter != null)
            {
                _riverMeshFilter.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }
    }
}
