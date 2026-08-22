namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Standalone ScriptableObject asset holding a complete terrain generation preset.
    /// Used for saving, loading, and applying predefined world archetypes (e.g., Mountains, Plains, Islands).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTerrainPreset", menuName = "Terrain/Archetype Preset", order = 130)]
    public class TerrainPreset : ScriptableObject
    {
        [Header("Preset Metadata")]
        public string PresetName = "Custom Preset";
        [TextArea(2, 4)]
        public string Description = "Procedural terrain preset.";

        [Header("Grid & Metrics")]
        public int ChunkSize = 240;
        public int ChunkResolution = 120;

        [Header("Noise & Macro Topography")]
        public NoiseSettings NoiseSettings = NoiseSettings.Default;
        public MacroMaskSettings MacroSettings = MacroMaskSettings.Default;

        [Header("Tectonic Plates & Ridges")]
        public TectonicSettings TectonicSettings = TectonicSettings.Default;

        [Header("Elevation Shaping")]
        public HeightCurveSettings HeightCurveSettings = HeightCurveSettings.Default;
        public FalloffSettings FalloffSettings = FalloffSettings.Default;

        [Header("Hydrology & Water")]
        public WaterSettings WaterSettings = WaterSettings.Default;
        public RiverSettings RiverSettings = RiverSettings.Default;
        public HydrologySettings HydrologySettings = HydrologySettings.Default;

        [Header("Biomes & Regions")]
        public TerrainRegion[] Regions;

        [Header("LOD & Streaming")]
        public float MaxViewDistance = 600f;
    }
}
