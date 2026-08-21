namespace ProjectTwo.Terrain.Presentation.Components
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;
    using ProjectTwo.Terrain.Presentation.Config;
    using ProjectTwo.Terrain.Presentation.Debug;
    using ProjectTwo.Terrain.Presentation.Pooling;

    /// <summary>
    /// Main controller managing infinite chunk streaming, background calculation tasks,
    /// cooperative CancellationToken cancellation, LOD updates, and spatial queries.
    /// Fully integrates global tectonic macro-zoning and hydrological river networks.
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

        [Tooltip("Number of preview chunks in radius for Editor Scene View (e.g. 1 = 3x3 grid of chunks).")]
        [Range(0, 3)]
        public int EditorPreviewRadius = 1;

        [Tooltip("Use fast low-resolution draft mesh during active slider manipulation.")]
        public bool UseDraftPreviewOnDrag = true;

        [Header("Debug Visualization")]
        [Tooltip("Draw tectonic plate boundaries and drift vectors in Scene View.")]
        public bool ShowTectonicGizmos = true;

        [Tooltip("Draw vector river splines, sources, and lake basins in Scene View.")]
        public bool ShowRiverGizmos = true;

        // Events
        public event Action<ChunkEventArgs> OnChunkLoaded;
        public event Action<ChunkEventArgs> OnChunkUnloaded;
        public event Action OnTerrainRegenerated;

        // Core Domain Services
        private ITerrainShaper _terrainShaper;
        private HeightMapBuilder _heightMapBuilder;
        private IChunkStorage _chunkStorage;
        private ITectonicService _tectonicService;
        private IHydrologyService _hydrologyService;
        private IRiverMeshBuilder _riverMeshBuilder;

        // Cached Global Macro Data
        private TectonicBoundary[] _cachedTectonicBoundaries;
        private RiverGraph _cachedRiverGraph;

        // Runtime Tracking
        private ChunkObjectPool _chunkPool;
        private readonly Dictionary<ChunkCoordinate, TerrainChunkView> _activeChunks = new Dictionary<ChunkCoordinate, TerrainChunkView>();
        private readonly HashSet<ChunkCoordinate> _inFlightCoordinates = new HashSet<ChunkCoordinate>();
        private readonly ConcurrentQueue<ChunkGenerationResult> _completedQueue = new ConcurrentQueue<ChunkGenerationResult>();

        // Cooperative Task Cancellation
        private CancellationTokenSource _generationCts;

        private Vector2 _lastViewerPosition;
        private const float ViewerMoveThreshold = 25f;

        private struct ChunkGenerationResult
        {
            public ChunkCoordinate Coordinate;
            public HeightMap HeightMap;
            public int Resolution;
        }

        private void Awake()
        {
            InitializeServices();
        }

        private void OnDestroy()
        {
            CancelInFlightTasks();
            ClearAllChunks();
            if (_chunkPool != null)
            {
                _chunkPool.Clear();
                _chunkPool = null;
            }
        }

        private void OnDisable()
        {
            CancelInFlightTasks();
            ClearAllChunks();
            if (_chunkPool != null)
            {
                _chunkPool.Clear();
                _chunkPool = null;
            }
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
                    if (Viewer.GetComponent<FreeFlyCameraController>() == null)
                    {
                        Viewer.gameObject.AddComponent<FreeFlyCameraController>();
                    }

                    float terrainHeightAtViewer = GetHeight(Viewer.position.x, Viewer.position.z);
                    if (Viewer.position.y <= terrainHeightAtViewer + 10f)
                    {
                        Viewer.position = new Vector3(Viewer.position.x, terrainHeightAtViewer + 45f, Viewer.position.z);
                        Viewer.rotation = Quaternion.Euler(20f, Viewer.eulerAngles.y, 0f);
                    }
                }

                if (FindAnyObjectByType<FPSCounter>() == null)
                {
                    gameObject.AddComponent<FPSCounter>();
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

            _terrainShaper = new ProceduralTerrainShaper();
            _heightMapBuilder = new HeightMapBuilder(_terrainShaper);
            _chunkStorage = new MemoryChunkStorage();
            _tectonicService = new TectonicService();
            _hydrologyService = new HydrologyService();
            _riverMeshBuilder = new RiverMeshBuilder();

            // Build Global Tectonic Macro Partition
            if (Configuration.TectonicSettings.Enabled)
            {
                _tectonicService.GenerateTectonicPartition(
                    Configuration.TectonicSettings,
                    out _,
                    out _cachedTectonicBoundaries);
            }
            else
            {
                _cachedTectonicBoundaries = null;
            }

            // Build Global Hydrological River Graph
            if (Configuration.HydrologySettings.Enabled)
            {
                _cachedRiverGraph = _hydrologyService.GenerateRiverGraph(
                    Configuration.HydrologySettings,
                    _terrainShaper,
                    Configuration.NoiseSettings,
                    Configuration.TectonicSettings,
                    Configuration.WaterSettings);
            }
            else
            {
                _cachedRiverGraph = RiverGraph.Empty;
            }

            if (_chunkPool == null)
            {
                _chunkPool = new ChunkObjectPool(transform, Configuration.TerrainMaterial, 36);
            }
        }

        public void CancelInFlightTasks()
        {
            if (_generationCts != null)
            {
                _generationCts.Cancel();
                _generationCts.Dispose();
                _generationCts = null;
            }

            _inFlightCoordinates.Clear();
            while (_completedQueue.TryDequeue(out _)) { }
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

            if (Configuration.EnablePersistence && _chunkStorage.TryGetChunk(coord, out HeightMap cachedMap))
            {
                _completedQueue.Enqueue(new ChunkGenerationResult { Coordinate = coord, HeightMap = cachedMap, Resolution = Configuration.ChunkResolution });
                return;
            }

            if (_generationCts == null || _generationCts.IsCancellationRequested)
            {
                _generationCts = new CancellationTokenSource();
            }

            CancellationToken token = _generationCts.Token;

            int resolution = Configuration.ChunkResolution;
            int size = Configuration.ChunkSize;
            Vector3 worldOrigin = coord.ToWorldPosition(size);
            float startX = worldOrigin.x - size * 0.5f;
            float startZ = worldOrigin.z - size * 0.5f;

            NoiseSettings noise = Configuration.NoiseSettings;
            MacroMaskSettings macro = Configuration.MacroSettings;
            TectonicSettings tectonics = Configuration.TectonicSettings;
            TectonicBoundary[] boundaries = _cachedTectonicBoundaries;
            HeightCurveSettings curve = Configuration.HeightCurveSettings;
            WaterSettings water = Configuration.WaterSettings;
            RiverSettings river = Configuration.RiverSettings;
            HydrologySettings hydrology = Configuration.HydrologySettings;
            RiverGraph graph = _cachedRiverGraph;
            FalloffSettings falloff = Configuration.FalloffSettings;

            Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;

                HeightMap map = _heightMapBuilder.GenerateCompoundHeightMap(
                    startX,
                    startZ,
                    size,
                    resolution,
                    noise,
                    macro,
                    tectonics,
                    boundaries,
                    curve,
                    water,
                    river,
                    hydrology,
                    graph,
                    falloff);

                if (token.IsCancellationRequested) return;

                if (Configuration.EnablePersistence)
                {
                    _chunkStorage.SaveChunkAsync(coord, map);
                }

                _completedQueue.Enqueue(new ChunkGenerationResult { Coordinate = coord, HeightMap = map, Resolution = resolution });
            }, token);
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

                    // Build and bind procedural river water ribbon mesh for this chunk
                    if (Configuration.HydrologySettings.Enabled && _cachedRiverGraph != null)
                    {
                        RiverWaterMeshData riverWater = _riverMeshBuilder.BuildChunkRiverMesh(
                            result.Coordinate,
                            Configuration.ChunkSize,
                            _cachedRiverGraph,
                            Configuration.HydrologySettings,
                            Configuration.WaterSettings);

                        chunk.SetRiverMesh(riverWater);
                    }
                    else
                    {
                        chunk.SetRiverMesh(null);
                    }

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
            CancelInFlightTasks();
            ClearAllChunks();
            InitializeServices();

            if (!Application.isPlaying)
            {
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
            ClearAllChunks();
            InitializeServices();

            int radius = Mathf.Clamp(EditorPreviewRadius, 0, 3);
            int resolution = Configuration.ChunkResolution;
            int size = Configuration.ChunkSize;

            for (int zOffset = -radius; zOffset <= radius; zOffset++)
            {
                for (int xOffset = -radius; xOffset <= radius; xOffset++)
                {
                    ChunkCoordinate coord = new ChunkCoordinate(xOffset, zOffset);
                    Vector3 worldOrigin = coord.ToWorldPosition(size);
                    float startX = worldOrigin.x - size * 0.5f;
                    float startZ = worldOrigin.z - size * 0.5f;

                    HeightMap map = _heightMapBuilder.GenerateCompoundHeightMap(
                        startX,
                        startZ,
                        size,
                        resolution,
                        Configuration.NoiseSettings,
                        Configuration.MacroSettings,
                        Configuration.TectonicSettings,
                        _cachedTectonicBoundaries,
                        Configuration.HeightCurveSettings,
                        Configuration.WaterSettings,
                        Configuration.RiverSettings,
                        Configuration.HydrologySettings,
                        _cachedRiverGraph,
                        Configuration.FalloffSettings);

                    GameObject chunkGo = new GameObject($"EditorChunk_{coord.X}_{coord.Z}",
                        typeof(MeshFilter), typeof(MeshRenderer), typeof(TerrainChunkView));
                    chunkGo.hideFlags = HideFlags.DontSave;
                    chunkGo.transform.SetParent(transform);
                    chunkGo.transform.localPosition = coord.ToWorldPosition(size);

                    TerrainChunkView chunk = chunkGo.GetComponent<TerrainChunkView>();
                    chunk.Initialize(
                        coord,
                        map,
                        size,
                        Configuration.NoiseSettings.HeightMultiplier,
                        Configuration.LodTiers,
                        Configuration.Regions,
                        Configuration.TerrainMaterial);

                    if (Configuration.HydrologySettings.Enabled && _cachedRiverGraph != null)
                    {
                        RiverWaterMeshData riverWater = _riverMeshBuilder.BuildChunkRiverMesh(
                            coord,
                            size,
                            _cachedRiverGraph,
                            Configuration.HydrologySettings,
                            Configuration.WaterSettings);

                        chunk.SetRiverMesh(riverWater);
                    }

                    chunk.UpdateLOD(0f);
                    _activeChunks[coord] = chunk;
                }
            }
        }

        public void ClearAllChunks()
        {
            CancelInFlightTasks();

            foreach (var kvp in _activeChunks)
            {
                if (kvp.Value != null && _chunkPool != null)
                {
                    _chunkPool.ReturnChunk(kvp.Value);
                }
            }
            _activeChunks.Clear();

            if (!Application.isPlaying)
            {
                TerrainChunkView[] children = GetComponentsInChildren<TerrainChunkView>(true);
                foreach (TerrainChunkView child in children)
                {
                    if (child != null && child.gameObject != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Configuration == null) return;

            if (ShowTectonicGizmos && Configuration.TectonicSettings.Enabled)
            {
                TectonicDebugGizmo.DrawTectonicGizmos(Configuration.TectonicSettings, transform.position);
            }

            if (ShowRiverGizmos && Configuration.HydrologySettings.Enabled && _cachedRiverGraph != null)
            {
                RiverGraphDebugGizmo.DrawRiverGizmos(_cachedRiverGraph, transform.position);
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
                float localZ = (worldZ - origin.z + Configuration.ChunkSize * 0.5f) / Configuration.ChunkSize;

                localX = Mathf.Clamp01(localX);
                localZ = Mathf.Clamp01(localZ);

                return chunk.HeightMap.InterpolateValue(localX, localZ);
            }

            if (_terrainShaper != null)
            {
                return _terrainShaper.CalculateElevation(
                    worldX,
                    worldZ,
                    Configuration.NoiseSettings,
                    Configuration.MacroSettings,
                    Configuration.TectonicSettings,
                    _cachedTectonicBoundaries,
                    Configuration.HeightCurveSettings,
                    Configuration.WaterSettings,
                    Configuration.RiverSettings,
                    Configuration.HydrologySettings,
                    _cachedRiverGraph,
                    Configuration.FalloffSettings);
            }

            return 0f;
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
