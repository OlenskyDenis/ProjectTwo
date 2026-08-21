namespace ProjectTwo.Terrain.Presentation.Config
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Represents a single elevation and slope band for terrain surface coloring and texture assignment.
    /// </summary>
    [Serializable]
    public struct BiomeVisualBand
    {
        [Tooltip("Layer identifier / name (e.g. Grass, Rock, Snow).")]
        public string Name;

        [Tooltip("Normalized elevation threshold boundary (0.0 to 1.0).")]
        [Range(0f, 1f)]
        public float HeightThreshold;

        [Tooltip("Slope angle threshold in degrees (0 to 90) for steep cliff transitions.")]
        [Range(0f, 90f)]
        public float SlopeThreshold;

        [Tooltip("Layer tint color.")]
        public Color Tint;

        [Tooltip("Optional Albedo texture for this layer.")]
        public Texture2D AlbedoTexture;

        [Tooltip("Optional Normal map for this layer.")]
        public Texture2D NormalMap;

        [Tooltip("Texture UV tiling scale.")]
        public Vector2 Tiling;

        [Tooltip("Blend transition softness between adjacent layers.")]
        [Range(0.01f, 1f)]
        public float BlendSoftness;

        public BiomeVisualBand(string name, float heightThreshold, Color tint)
        {
            Name = name;
            HeightThreshold = heightThreshold;
            SlopeThreshold = 0f;
            Tint = tint;
            AlbedoTexture = null;
            NormalMap = null;
            Tiling = new Vector2(1f, 1f);
            BlendSoftness = 0.1f;
        }
    }

    /// <summary>
    /// Reusable visual profile asset defining shaders, surface colors, and biome bands for terrain rendering.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTerrainVisualProfile", menuName = "ProjectTwo/Terrain/Terrain Visual Profile", order = 10)]
    public class TerrainVisualProfileSO : ScriptableObject
    {
        [Header("Shader & Base Shading")]
        [Tooltip("Base shader used for terrain rendering (defaults to ProjectTwo/TerrainVertexColor).")]
        public Shader CustomTerrainShader;

        [Tooltip("Global base tint applied to terrain.")]
        public Color GlobalTint = Color.white;

        [Tooltip("Enable triplanar texture blending / procedural texture array parameters if supported by shader.")]
        public bool EnableTriplanarBlending = false;

        [Header("Fallback")]
        [Tooltip("Optional custom material override directly assigned without procedural configuration.")]
        public Material DirectMaterialOverride;

        [Header("Biome Elevation Bands")]
        [Tooltip("Ordered collection of visual bands mapped to height and slope.")]
        public List<BiomeVisualBand> BiomeBands = new List<BiomeVisualBand>();

        /// <summary>
        /// Event fired whenever profile parameters are modified in editor or runtime.
        /// </summary>
        public event Action OnProfileChanged;

        public void NotifyProfileChanged()
        {
            OnProfileChanged?.Invoke();
        }

        private void OnValidate()
        {
            NotifyProfileChanged();
        }

        /// <summary>
        /// Returns an abstract descriptor representing this profile.
        /// </summary>
        public MaterialDescriptor GetDescriptor()
        {
            int hash = 17;
            hash = hash * 31 + (CustomTerrainShader != null ? CustomTerrainShader.name.GetHashCode() : 0);
            hash = hash * 31 + GlobalTint.GetHashCode();
            hash = hash * 31 + (EnableTriplanarBlending ? 1 : 0);
            if (BiomeBands != null)
            {
                foreach (BiomeVisualBand band in BiomeBands)
                {
                    hash = hash * 31 + band.Tint.GetHashCode();
                    hash = hash * 31 + band.HeightThreshold.GetHashCode();
                }
            }
            return new MaterialDescriptor(name, name, hash);
        }

        /// <summary>
        /// Creates a default baseline visual profile populated with standard biomes.
        /// </summary>
        public static TerrainVisualProfileSO CreateDefaultProfile()
        {
            TerrainVisualProfileSO profile = CreateInstance<TerrainVisualProfileSO>();
            profile.name = "DefaultTerrainVisualProfile";
            profile.GlobalTint = Color.white;
            profile.BiomeBands = new List<BiomeVisualBand>
            {
                new BiomeVisualBand("Deep Water", 0.2f, new Color(0.1f, 0.25f, 0.65f)),
                new BiomeVisualBand("Shallow Water", 0.35f, new Color(0.2f, 0.5f, 0.85f)),
                new BiomeVisualBand("Sand Beach", 0.4f, new Color(0.85f, 0.8f, 0.5f)),
                new BiomeVisualBand("Green Grass", 0.65f, new Color(0.3f, 0.65f, 0.25f)),
                new BiomeVisualBand("Pine Forest", 0.75f, new Color(0.18f, 0.45f, 0.15f)),
                new BiomeVisualBand("Grey Mountain Rock", 0.9f, new Color(0.5f, 0.45f, 0.4f)) { SlopeThreshold = 35f },
                new BiomeVisualBand("Snowy Peaks", 1.0f, new Color(0.95f, 0.95f, 0.98f))
            };
            return profile;
        }
    }
}
