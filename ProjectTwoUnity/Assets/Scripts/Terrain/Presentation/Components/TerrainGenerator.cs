namespace ProjectTwo.Terrain.Presentation.Components
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;
    using ProjectTwo.Terrain.Presentation.Config;
    using ProjectTwo.Terrain.Presentation.Pooling;

    /// <summary>
    /// Main controller managing infinite chunk streaming, background calculation tasks, LOD updates, and spatial queries.
    /// </summary>
    [ExecuteAlways]
    public class TerrainGenerator : MonoBehaviour, ITerrainProvider
    {
        [Header("Configuration")]
        [Tooltip("Terrain configuration preset asset.")]
        public TerrainDataConfig Configuration;

        [Header("Viewer Tracking")]
        [Tooltip("Transform of the viewer (Camera or Player). Defaults to Main Camera if null.")]
        public Transform Viewer;

        [Header("Editor Preview")]
        [Tooltip("Auto-update terrain preview when values change in the Inspector.")]
        public bool AutoUpdate = true;

        // Events
        public event Action<ChunkEventArgs> OnChunkLoaded;
        public event Action<ChunkEventArgs> OnChunkUnloaded;
        public event Action OnTerrainRegenerated;

        // Core Domain Services
        private INoiseGenerator _noiseGenerator;
        private HeightMapBuilder _heightMapBuilder;
        private IChunkStorage _chunkStorage;

        // Runtime Tracking
        private ChunkObjectPool _chunkPool;
        private readonly Dictionary<ChunkCoordinate, TerrainChunkView> _activeChunks = new Dictionary<ChunkCoordinate, TerrainChunkView>();
        private readonly HashSet<ChunkCoordinate> _inFlightCoordinates = new HashSet<ChunkCoordinate>();
        private readonly ConcurrentQueue<ChunkGenerationResult> _completedQueue = new ConcurrentQueue<ChunkGenerationResult>();

        private Vector2 _lastViewerPosition;
        private const float ViewerMoveThreshold = 25f;

        private struct ChunkGenerationResult
        {
            public ChunkCoordinate Coordinate;
            public HeightMap HeightMap;
        }

        private void Awake()
        {
            InitializeServices();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (Viewer == null)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Viewer = mainCam.transform;
                    }
                }

                if (Viewer != null)
                {
                    // Automatically add smooth free fly controls if none exist
                    if (Viewer.GetComponent<FreeFlyCameraController>() == null)
                    {
                        Viewer.gameObject.AddComponent<FreeFlyCameraController>();
                    }

                    // Auto-elevate camera so it's not submerged in the ground
                    float terrainHeightAtViewer = GetHeight(Viewer.position.x, Viewer.position.z);
                    if (Viewer.position.y <= terrainHeightAtViewer + 10f)
                    {
                        Viewer.position = new Vector3(Viewer.position.x, terrainHeightAtViewer + 45f, Viewer.position.z);
                        Viewer.rotation = Quaternion.Euler(20f, Viewer.eulerAngles.y, 0f);
                    }
                }

                UpdateVisibleChunks();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            ProcessCompletedChunks();

            if (Viewer != null)
            {
                Vector2 viewerPos2D = new Vector2(Viewer.position.x, Viewer.position.z);
                if (Vector2.Distance(viewerPos2D, _lastViewerPosition) > ViewerMoveThreshold)
                {
                    _lastViewerPosition = viewerPos2D;
                    UpdateVisibleChunks();
                }
            }
        }

        private void InitializeServices()
        {
            if (Configuration == null)
            {
                Configuration = ScriptableObject.CreateInstance<TerrainDataConfig>();
            }

            Configuration.Validate();

            _noiseGenerator = new PerlinNoiseGenerator();
            _heightMapBuilder = new HeightMapBuilder(_noiseGenerator);
            _chunkStorage = new MemoryChunkStorage();

            if (_chunkPool == null)
            {
                _chunkPool = new ChunkObjectPool(transform, Configuration.TerrainMaterial, 36);
            }
        }

        public void UpdateVisibleChunks()
        {
            if (Configuration == null) return;
            InitializeServices();

            Vector3 viewerPos = Viewer != null ? Viewer.position : transform.position;

            int chunkSize = Configuration.ChunkSize;
            float maxViewDist = Configuration.MaxViewDistance;
            int chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDist / chunkSize);

            int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
            int currentChunkCoordZ = Mathf.RoundToInt(viewerPos.z / chunkSize);

            HashSet<ChunkCoordinate> visibleCoordinates = new HashSet<ChunkCoordinate>();

            for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
                {
                    ChunkCoordinate viewedChunkCoord = new ChunkCoordinate(currentChunkCoordX + xOffset, currentChunkCoordZ + yOffset);
                    float distanceToViewer = viewedChunkCoord.DistanceTo(viewerPos, chunkSize);

                    if (distanceToViewer <= maxViewDist)
                    {
                        visibleCoordinates.Add(viewedChunkCoord);

                        if (_activeChunks.TryGetValue(viewedChunkCoord, out TerrainChunkView existingChunk))
                        {
                            existingChunk.UpdateLOD(distanceToViewer);
                        }
                        else if (!_inFlightCoordinates.Contains(viewedChunkCoord))
                        {
                            RequestChunkGeneration(viewedChunkCoord);
                        }
                    }
                }
            }

            // Unload out-of-range chunks
            List<ChunkCoordinate> toRemove = new List<ChunkCoordinate>();
            foreach (var kvp in _activeChunks)
            {
                if (!visibleCoordinates.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (ChunkCoordinate coord in toRemove)
            {
                TerrainChunkView chunk = _activeChunks[coord];
                _activeChunks.Remove(coord);

                OnChunkUnloaded?.Invoke(new ChunkEventArgs(coord.X, coord.Z, chunk.transform.position, Configuration.ChunkSize, chunk.CurrentLOD));
                _chunkPool.ReturnChunk(chunk);
            }
        }

        private void RequestChunkGeneration(ChunkCoordinate coord)
        {
            _inFlightCoordinates.Add(coord);

            // Check cache first
            if (Configuration.EnablePersistence && _chunkStorage.TryGetChunk(coord, out HeightMap cachedMap))
            {
                _completedQueue.Enqueue(new ChunkGenerationResult { Coordinate = coord, HeightMap = cachedMap });
                return;
            }

            int resolution = Configuration.ChunkResolution;
            NoiseSettings noise = Configuration.NoiseSettings;

            Task.Run(() =>
            {
                HeightMap map = _heightMapBuilder.GenerateHeightMap(resolution, resolution, noise, coord);
                if (Configuration.EnablePersistence)
                {
                    _chunkStorage.SaveChunkAsync(coord, map);
                }

                _completedQueue.Enqueue(new ChunkGenerationResult { Coordinate = coord, HeightMap = map });
            });
        }

        private void ProcessCompletedChunks()
        {
            while (_completedQueue.TryDequeue(out ChunkGenerationResult result))
            {
                _inFlightCoordinates.Remove(result.Coordinate);

                Vector3 viewerPos = Viewer != null ? Viewer.position : transform.position;
                float distance = result.Coordinate.DistanceTo(viewerPos, Configuration.ChunkSize);

                if (distance <= Configuration.MaxViewDistance)
                {
                    TerrainChunkView chunk = _chunkPool.GetChunk();
                    chunk.Initialize(
                        result.Coordinate,
                        result.HeightMap,
                        Configuration.ChunkSize,
                        Configuration.NoiseSettings.HeightMultiplier,
                        Configuration.LodTiers,
                        Configuration.Regions,
                        Configuration.TerrainMaterial);

                    chunk.UpdateLOD(distance);
                    _activeChunks[result.Coordinate] = chunk;

                    OnChunkLoaded?.Invoke(new ChunkEventArgs(
                        result.Coordinate.X,
                        result.Coordinate.Z,
                        chunk.transform.position,
                        Configuration.ChunkSize,
                        chunk.CurrentLOD));
                }
            }
        }

        public void Regenerate()
        {
            ClearAllChunks();
            InitializeServices();

            if (!Application.isPlaying)
            {
                // Synchronous generation for Editor preview
                GenerateEditorPreview();
            }
            else
            {
                UpdateVisibleChunks();
            }

            OnTerrainRegenerated?.Invoke();
        }

        private void GenerateEditorPreview()
        {
            ChunkCoordinate centerCoord = new ChunkCoordinate(0, 0);
            HeightMap map = _heightMapBuilder.GenerateHeightMap(
                Configuration.ChunkResolution,
                Configuration.ChunkResolution,
                Configuration.NoiseSettings,
                centerCoord);

            TerrainChunkView previewChunk = GetComponentInChildren<TerrainChunkView>();
            if (previewChunk == null)
            {
                GameObject go = new GameObject("EditorPreviewChunk", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(TerrainChunkView));
                go.transform.SetParent(transform);
                previewChunk = go.GetComponent<TerrainChunkView>();
            }

            previewChunk.Initialize(
                centerCoord,
                map,
                Configuration.ChunkSize,
                Configuration.NoiseSettings.HeightMultiplier,
                Configuration.LodTiers,
                Configuration.Regions,
                Configuration.TerrainMaterial);

            previewChunk.UpdateLOD(0f);
        }

        public void ClearAllChunks()
        {
            _inFlightCoordinates.Clear();
            while (_completedQueue.TryDequeue(out _)) { }

            foreach (var kvp in _activeChunks)
            {
                if (kvp.Value != null)
                {
                    _chunkPool.ReturnChunk(kvp.Value);
                }
            }
            _activeChunks.Clear();

            if (!Application.isPlaying)
            {
                TerrainChunkView[] children = GetComponentsInChildren<TerrainChunkView>();
                foreach (TerrainChunkView child in children)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        #region ITerrainProvider Implementation

        public float GetHeight(float worldX, float worldZ)
        {
            if (Configuration == null) return 0f;
            ChunkCoordinate coord = ChunkCoordinate.FromWorldPosition(new Vector3(worldX, 0f, worldZ), Configuration.ChunkSize);

            if (_activeChunks.TryGetValue(coord, out TerrainChunkView chunk) && chunk.HeightMap != null)
            {
                Vector3 origin = coord.ToWorldPosition(Configuration.ChunkSize);
                float localX = (worldX - origin.x + Configuration.ChunkSize * 0.5f) / Configuration.ChunkSize;
                float localZ = (origin.z + Configuration.ChunkSize * 0.5f - worldZ) / Configuration.ChunkSize;

                localX = Mathf.Clamp01(localX);
                localZ = Mathf.Clamp01(localZ);

                float normalized = chunk.HeightMap.InterpolateValue(localX, localZ);
                return normalized * Configuration.NoiseSettings.HeightMultiplier;
            }

            // Fallback: evaluate procedural noise directly
            float sample = _noiseGenerator?.SampleNoise(worldX, worldZ, Configuration.NoiseSettings) ?? 0f;
            return sample * Configuration.NoiseSettings.HeightMultiplier;
        }

        public Vector3 GetNormal(float worldX, float worldZ)
        {
            float step = 1f;
            float hL = GetHeight(worldX - step, worldZ);
            float hR = GetHeight(worldX + step, worldZ);
            float hD = GetHeight(worldX, worldZ - step);
            float hU = GetHeight(worldX, worldZ + step);

            Vector3 normal = new Vector3(hL - hR, 2f * step, hD - hU).normalized;
            return normal;
        }

        public float GetSlope(float worldX, float worldZ)
        {
            Vector3 normal = GetNormal(worldX, worldZ);
            return Vector3.Angle(Vector3.up, normal);
        }

        public string GetBiomeAt(float worldX, float worldZ)
        {
            float height = GetHeight(worldX, worldZ);
            float normalized = Configuration.NoiseSettings.HeightMultiplier > 0f ? height / Configuration.NoiseSettings.HeightMultiplier : 0f;

            if (Configuration.Regions != null && Configuration.Regions.Length > 0)
            {
                for (int i = 0; i < Configuration.Regions.Length; i++)
                {
                    if (normalized <= Configuration.Regions[i].HeightThreshold)
                    {
                        return Configuration.Regions[i].Name;
                    }
                }
                return Configuration.Regions[Configuration.Regions.Length - 1].Name;
            }

            return "Default";
        }

        public bool IsPositionLoaded(float worldX, float worldZ)
        {
            if (Configuration == null) return false;
            ChunkCoordinate coord = ChunkCoordinate.FromWorldPosition(new Vector3(worldX, 0f, worldZ), Configuration.ChunkSize);
            return _activeChunks.ContainsKey(coord);
        }

        #endregion
    }
}
