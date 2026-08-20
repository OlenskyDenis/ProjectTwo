namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Defines an individual landscape biome / elevation layer with height threshold, slope angle, textures, and color.
    /// </summary>
    [Serializable]
    public struct TerrainRegion
    {
        [Tooltip("Name of the biome / elevation layer.")]
        public string Name;

        [Tooltip("Normalized height threshold boundary (0.0 to 1.0).")]
        [Range(0f, 1f)]
        public float HeightThreshold;

        [Tooltip("Slope angle threshold in degrees (0 to 90) for steep cliff transitions (e.g. rocks on slopes).")]
        [Range(0f, 90f)]
        public float SlopeThreshold;

        [Tooltip("Color tint for this elevation band.")]
        public Color ColorTint;

        [Tooltip("Optional Albedo texture for this biome layer.")]
        public Texture2D AlbedoTexture;

        [Tooltip("Optional Normal map for this biome layer.")]
        public Texture2D NormalMap;

        [Tooltip("Texture UV tiling scale.")]
        public Vector2 Tiling;

        [Tooltip("Blend transition softness between adjacent layers.")]
        [Range(0.01f, 1f)]
        public float BlendSoftness;

        [Tooltip("Optional custom material override for this biome layer.")]
        public Material CustomMaterialOverride;

        public TerrainRegion(string name, float heightThreshold, Color colorTint)
        {
            Name = name;
            HeightThreshold = heightThreshold;
            SlopeThreshold = 0f;
            ColorTint = colorTint;
            AlbedoTexture = null;
            NormalMap = null;
            Tiling = new Vector2(1f, 1f);
            BlendSoftness = 0.1f;
            CustomMaterialOverride = null;
        }

        public static TerrainRegion[] CreateDefaultRegions()
        {
            return new[]
            {
                new TerrainRegion("Deep Water", 0.2f, new Color(0.1f, 0.25f, 0.65f)),
                new TerrainRegion("Shallow Water", 0.35f, new Color(0.2f, 0.5f, 0.85f)),
                new TerrainRegion("Sand Beach", 0.4f, new Color(0.85f, 0.8f, 0.5f)),
                new TerrainRegion("Green Grass", 0.65f, new Color(0.3f, 0.65f, 0.25f)),
                new TerrainRegion("Pine Forest", 0.75f, new Color(0.18f, 0.45f, 0.15f)),
                new TerrainRegion("Grey Mountain Rock", 0.9f, new Color(0.5f, 0.45f, 0.4f)) { SlopeThreshold = 35f },
                new TerrainRegion("Snowy Peaks", 1.0f, new Color(0.95f, 0.95f, 0.98f))
            };
        }

        public static TerrainRegion[] CreateAutumnRegions()
        {
            return new[]
            {
                new TerrainRegion("Cold Deep Lake", 0.22f, new Color(0.08f, 0.18f, 0.35f)),
                new TerrainRegion("Wet Pebbles", 0.35f, new Color(0.45f, 0.4f, 0.32f)),
                new TerrainRegion("Golden Meadow", 0.55f, new Color(0.82f, 0.68f, 0.22f)),
                new TerrainRegion("Amber Woods", 0.72f, new Color(0.85f, 0.42f, 0.12f)),
                new TerrainRegion("Rust Canyon", 0.88f, new Color(0.55f, 0.32f, 0.22f)) { SlopeThreshold = 32f },
                new TerrainRegion("Frosty Crest", 1.0f, new Color(0.88f, 0.88f, 0.92f))
            };
        }

        public static TerrainRegion[] CreateArcticRegions()
        {
            return new[]
            {
                new TerrainRegion("Frozen Deep", 0.25f, new Color(0.12f, 0.25f, 0.42f)),
                new TerrainRegion("Glacial Ice", 0.4f, new Color(0.6f, 0.82f, 0.92f)),
                new TerrainRegion("Frost Moss", 0.58f, new Color(0.42f, 0.55f, 0.5f)),
                new TerrainRegion("Slate Granite", 0.78f, new Color(0.38f, 0.4f, 0.45f)) { SlopeThreshold = 30f },
                new TerrainRegion("Deep Snow", 1.0f, new Color(0.98f, 0.98f, 1.0f))
            };
        }

        public static TerrainRegion[] CreateDesertRegions()
        {
            return new[]
            {
                new TerrainRegion("Basalt Basin", 0.2f, new Color(0.28f, 0.22f, 0.2f)),
                new TerrainRegion("Orange Dust", 0.38f, new Color(0.88f, 0.52f, 0.25f)),
                new TerrainRegion("Red Dune Sand", 0.6f, new Color(0.82f, 0.38f, 0.18f)),
                new TerrainRegion("Terracotta Cliffs", 0.82f, new Color(0.65f, 0.28f, 0.18f)) { SlopeThreshold = 28f },
                new TerrainRegion("Sunbaked Mesa", 1.0f, new Color(0.92f, 0.75f, 0.55f))
            };
        }

        public static TerrainRegion[] CreateTropicalRegions()
        {
            return new[]
            {
                new TerrainRegion("Turquoise Ocean", 0.28f, new Color(0.05f, 0.45f, 0.65f)),
                new TerrainRegion("Coral White Sand", 0.38f, new Color(0.95f, 0.92f, 0.78f)),
                new TerrainRegion("Jungle Rainforest", 0.68f, new Color(0.12f, 0.58f, 0.2f)),
                new TerrainRegion("Volcanic Basalt", 0.88f, new Color(0.3f, 0.28f, 0.28f)) { SlopeThreshold = 35f },
                new TerrainRegion("Misty Cloud Peak", 1.0f, new Color(0.85f, 0.92f, 0.88f))
            };
        }
    }
}
