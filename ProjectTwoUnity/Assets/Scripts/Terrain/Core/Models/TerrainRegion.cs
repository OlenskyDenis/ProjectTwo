namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Defines an individual landscape biome / elevation layer with height threshold and color.
    /// </summary>
    [Serializable]
    public struct TerrainRegion
    {
        [Tooltip("Name of the biome / elevation layer.")]
        public string Name;

        [Tooltip("Normalized height threshold boundary (0.0 to 1.0).")]
        [Range(0f, 1f)]
        public float HeightThreshold;

        [Tooltip("Color tint for this elevation band.")]
        public Color ColorTint;

        public TerrainRegion(string name, float heightThreshold, Color colorTint)
        {
            Name = name;
            HeightThreshold = heightThreshold;
            ColorTint = colorTint;
        }

        public static TerrainRegion[] CreateDefaultRegions()
        {
            return new[]
            {
                new TerrainRegion("Deep Water", 0.2f, new Color(0.1f, 0.25f, 0.65f)),
                new TerrainRegion("Shallow Water", 0.35f, new Color(0.2f, 0.5f, 0.85f)),
                new TerrainRegion("Sand", 0.4f, new Color(0.85f, 0.8f, 0.5f)),
                new TerrainRegion("Grass", 0.65f, new Color(0.3f, 0.65f, 0.25f)),
                new TerrainRegion("Forest", 0.75f, new Color(0.18f, 0.45f, 0.15f)),
                new TerrainRegion("Rock", 0.9f, new Color(0.5f, 0.45f, 0.4f)),
                new TerrainRegion("Snow", 1.0f, new Color(0.95f, 0.95f, 0.98f))
            };
        }
    }
}
