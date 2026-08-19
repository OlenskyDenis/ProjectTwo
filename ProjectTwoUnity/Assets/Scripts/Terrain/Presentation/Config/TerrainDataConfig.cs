namespace ProjectTwo.Terrain.Presentation.Config
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Standalone ScriptableObject configuration asset holding all terrain generation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "Terrain/Configuration Preset", order = 120)]
    public class TerrainDataConfig : ScriptableObject
    {
        [Header("Chunk Grid Setup")]
        [Tooltip("Size of each chunk in world units (must divide evenly by LOD steps).")]
        [Range(16, 480)]
        public int ChunkSize = 240;

        [Tooltip("Number of vertices per edge for a chunk (e.g. 120 or 240).")]
        [Range(16, 240)]
        public int ChunkResolution = 120;

        [Header("Noise Configuration")]
        public NoiseSettings NoiseSettings = NoiseSettings.Default;

        [Header("LOD Settings")]
        [Tooltip("List of LOD levels with distance thresholds and resolution steps.")]
        public LODInfo[] LodTiers;

        [Header("Viewer & Streaming")]
        [Tooltip("Maximum view distance radius in world units.")]
        [Range(100f, 2000f)]
        public float MaxViewDistance = 600f;

        [Header("Biome / Elevation Regions")]
        [Tooltip("List of elevation regions sorted by height threshold.")]
        public TerrainRegion[] Regions;

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
        }

        public void Validate()
        {
            if (ChunkSize <= 0) ChunkSize = 240;
            if (ChunkResolution < 16) ChunkResolution = 16;
            if (MaxViewDistance < 50f) MaxViewDistance = 50f;
            NoiseSettings.Validate();

            if (LodTiers == null || LodTiers.Length == 0)
            {
                LodTiers = LODInfo.CreateDefaultTiers(MaxViewDistance);
            }

            if (Regions == null || Regions.Length == 0)
            {
                Regions = TerrainRegion.CreateDefaultRegions();
            }
        }
    }
}
