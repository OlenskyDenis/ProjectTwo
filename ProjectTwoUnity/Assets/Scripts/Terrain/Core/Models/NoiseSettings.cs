namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Synthesis mode for procedural terrain noise generation.
    /// </summary>
    public enum NoiseType
    {
        PerlinFbm = 0,
        RidgedMultifractal = 1,
        Billow = 2
    }

    /// <summary>
    /// Configuration parameters for procedural noise sampling.
    /// </summary>
    [Serializable]
    public struct NoiseSettings : IEquatable<NoiseSettings>
    {
        [Tooltip("Procedural noise algorithm type.")]
        public NoiseType Type;

        [Tooltip("Seed for deterministic procedural noise generation.")]
        public int Seed;

        [Tooltip("Frequency scale of the noise. Higher values zoom out the landscape.")]
        [Range(0.001f, 500f)]
        public float Scale;

        [Tooltip("Number of fractal octave layers (fBm).")]
        [Range(1, 8)]
        public int Octaves;

        [Tooltip("Amplitude reduction factor per octave (roughness).")]
        [Range(0.01f, 1f)]
        public float Persistence;

        [Tooltip("Frequency multiplication factor per octave.")]
        [Range(1f, 5f)]
        public float Lacunarity;

        [Tooltip("Global vertical elevation scale multiplier.")]
        public float HeightMultiplier;

        [Tooltip("Coordinate translation offset.")]
        public Vector2 Offset;

        public static NoiseSettings Default => new NoiseSettings
        {
            Type = NoiseType.PerlinFbm,
            Seed = 1337,
            Scale = 50f,
            Octaves = 4,
            Persistence = 0.5f,
            Lacunarity = 2.0f,
            HeightMultiplier = 30f,
            Offset = Vector2.zero
        };

        public void Validate()
        {
            if (Scale <= 0.0001f) Scale = 0.001f;
            if (Octaves < 1) Octaves = 1;
            if (Octaves > 8) Octaves = 8;
            if (Persistence < 0.01f) Persistence = 0.01f;
            if (Persistence > 1f) Persistence = 1f;
            if (Lacunarity < 1f) Lacunarity = 1f;
            if (HeightMultiplier < 0f) HeightMultiplier = 0f;
        }

        public bool Equals(NoiseSettings other)
        {
            return Type == other.Type &&
                   Seed == other.Seed &&
                   Mathf.Approximately(Scale, other.Scale) &&
                   Octaves == other.Octaves &&
                   Mathf.Approximately(Persistence, other.Persistence) &&
                   Mathf.Approximately(Lacunarity, other.Lacunarity) &&
                   Mathf.Approximately(HeightMultiplier, other.HeightMultiplier) &&
                   Offset == other.Offset;
        }

        public override bool Equals(object obj) => obj is NoiseSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)Type;
                hash = hash * 31 + Seed;
                hash = hash * 31 + Scale.GetHashCode();
                hash = hash * 31 + Octaves;
                hash = hash * 31 + Persistence.GetHashCode();
                hash = hash * 31 + Lacunarity.GetHashCode();
                hash = hash * 31 + HeightMultiplier.GetHashCode();
                hash = hash * 31 + Offset.GetHashCode();
                return hash;
            }
        }
    }
}
