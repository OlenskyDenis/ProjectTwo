namespace ProjectTwo.Terrain.Presentation.Components
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    /// <summary>
    /// Presentation component managing the GameObject, MeshFilter, MeshRenderer, and MeshCollider for a chunk.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunkView : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        public ChunkCoordinate Coordinate { get; private set; }
        public HeightMap HeightMap { get; private set; }
        public int CurrentLOD { get; private set; } = -1;

        private float _chunkSize;
        private float _heightMultiplier;
        private LODInfo[] _lodTiers;
        private TerrainRegion[] _regions;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            _mesh = new Mesh { name = "TerrainChunkMesh" };
            _meshFilter.sharedMesh = _mesh;
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

            if (_meshRenderer != null && material != null)
            {
                _meshRenderer.sharedMaterial = material;
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
                    SetLOD(0, 1, true);
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
                SetLOD(tier.LodIndex, tier.MeshResolutionStep, tier.HasCollider);
            }
        }

        private void SetLOD(int lodIndex, int resolutionStep, bool enableCollider)
        {
            CurrentLOD = lodIndex;

            if (HeightMap == null) return;

            TerrainMeshData meshData = TerrainMeshBuilder.GenerateTerrainMesh(
                HeightMap,
                _chunkSize,
                _heightMultiplier,
                resolutionStep,
                _regions);

            meshData.ApplyToMesh(_mesh);

            if (_meshCollider != null)
            {
                if (enableCollider)
                {
                    _meshCollider.enabled = true;
                    _meshCollider.sharedMesh = null;
                    _meshCollider.sharedMesh = _mesh;
                }
                else
                {
                    _meshCollider.enabled = false;
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
            if (_meshCollider != null) _meshCollider.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
