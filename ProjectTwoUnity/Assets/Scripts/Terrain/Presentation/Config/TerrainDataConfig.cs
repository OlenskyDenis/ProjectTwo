namespace ProjectTwo.Terrain.Presentation.Config
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Standalone ScriptableObject configuration asset holding all terrain generation parameters.
    /// Automatically enforces valid mathematical multiples to guarantee seamless chunk borders across all LOD tiers.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "Terrain/Configuration Preset", order = 120)]
    public class TerrainDataConfig : ScriptableObject
    {
        [Header("Chunk Grid Setup")]
        [Tooltip("Size of each chunk in world units (automatically snapped to multiples of 12 for seamless LODs).")]
        [Range(24, 480)]
        public int ChunkSize = 240;

        [Tooltip("Number of grid segments per edge (must be divisible by LOD steps 1, 2, 4, 6, e.g., 24, 48, 72, 96, 120, 240). Heightmap will contain (Resolution + 1) vertices.")]
        [Range(24, 240)]
        public int ChunkResolution = 120;

        [Header("Noise Configuration")]
        public NoiseSettings NoiseSettings = NoiseSettings.Default;

        [Header("Macro Mountain & Continent Masking")]
        public MacroMaskSettings MacroSettings = MacroMaskSettings.Default;

        [Header("Non-Linear Elevation Curves")]
        public HeightCurveSettings HeightCurveSettings = HeightCurveSettings.Default;

        [Header("Boundary & Island Falloff")]
        public FalloffSettings FalloffSettings = FalloffSettings.Default;

        [Header("Water & Sea Basins")]
        public WaterSettings WaterSettings = WaterSettings.Default;

        [Header("Procedural Rivers")]
        public RiverSettings RiverSettings = RiverSettings.Default;

        [Header("Biome / Elevation Regions")]
        [Tooltip("List of elevation regions sorted by height threshold.")]
        public TerrainRegion[] Regions;

        [Header("LOD Settings")]
        [Tooltip("List of LOD levels with distance thresholds and resolution steps.")]
        public LODInfo[] LodTiers;

        [Header("Viewer & Streaming")]
        [Tooltip("Maximum view distance radius in world units.")]
        [Range(100f, 2000f)]
        public float MaxViewDistance = 600f;

        [Header("Visuals & Materials")]
        [Tooltip("Material assigned to generated terrain chunks.")]
        public Material TerrainMaterial;

        [Header("Persistence")]
        [Tooltip("Enable caching and persistence of visited chunk data.")]
        public bool EnablePersistence = true;

        private void OnEnable()
        {
            if (LodTiers == null || LodTiers.Length == 0)
            {
                LodTiers = LODInfo.CreateDefaultTiers(MaxViewDistance);
            }

            if (Regions == null || Regions.Length == 0)
            {
                Regions = TerrainRegion.CreateDefaultRegions();
            }

            if (HeightCurveSettings == null)
            {
                HeightCurveSettings = HeightCurveSettings.Default;
            }

            Validate();
        }

        private void OnValidate()
        {
            Validate();
        }

        public void Validate()
        {
            // Snap ChunkResolution to nearest multiple of 12 (min 24, max 240)
            if (ChunkResolution < 24) ChunkResolution = 24;
            if (ChunkResolution > 240) ChunkResolution = 240;
            ChunkResolution = Mathf.RoundToInt(ChunkResolution / 12f) * 12;

            // Snap ChunkSize to nearest multiple of 12 (min 24)
            if (ChunkSize < 24) ChunkSize = 24;
            ChunkSize = Mathf.RoundToInt(ChunkSize / 12f) * 12;

            if (MaxViewDistance < 50f) MaxViewDistance = 50f;
            NoiseSettings.Validate();
            MacroSettings.Validate();
            WaterSettings.Validate();
            RiverSettings.Validate();
            FalloffSettings.Validate();
            if (HeightCurveSettings != null) HeightCurveSettings.Validate();

            if (LodTiers == null || LodTiers.Length == 0)
            {
                LodTiers = LODInfo.CreateDefaultTiers(MaxViewDistance);
            }

            if (Regions == null || Regions.Length == 0)
            {
                Regions = TerrainRegion.CreateDefaultRegions();
            }
        }

        /// <summary>
        /// Resets all terrain parameters to clean, balanced, production-ready defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            ChunkSize = 240;
            ChunkResolution = 120;
            NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.PerlinFbm,
                Seed = 1337,
                Scale = 140f,
                Octaves = 4,
                Persistence = 0.45f,
                Lacunarity = 2.0f,
                HeightMultiplier = 45f,
                Offset = Vector2.zero
            };
            MacroSettings = MacroMaskSettings.Default;
            HeightCurveSettings = HeightCurveSettings.Default;
            FalloffSettings = FalloffSettings.Default;
            WaterSettings = WaterSettings.Default;
            RiverSettings = RiverSettings.Default;
            Regions = TerrainRegion.CreateDefaultRegions();
            MaxViewDistance = 600f;
            LodTiers = LODInfo.CreateDefaultTiers(600f);
            EnablePersistence = true;
            Validate();
        }
    }
}
