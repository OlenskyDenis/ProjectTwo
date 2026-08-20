namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for procedural river carving masks.
    /// Carves smooth river channels and drainage valleys down to sea level across chunk borders.
    /// </summary>
    [Serializable]
    public struct RiverSettings : IEquatable<RiverSettings>
    {
        [Tooltip("Enable procedural river carving.")]
        public bool Enabled;

        [Tooltip("Seed offset for river channel generation.")]
        public int Seed;

        [Tooltip("River network frequency (smaller values = wider distributed rivers).")]
        [Range(0.001f, 0.05f)]
        public float Frequency;

        [Tooltip("Maximum vertical depth carved into the landscape.")]
        [Range(1f, 50f)]
        public float CarveDepth;

        [Tooltip("Width of the riverbed in world units.")]
        [Range(2f, 80f)]
        public float RiverbedWidth;

        [Tooltip("Softness/smoothness of riverbank slopes.")]
        [Range(0.01f, 1f)]
        public float BankSmoothness;

        public static RiverSettings Default => new RiverSettings
        {
            Enabled = false,
            Seed = 555,
            Frequency = 0.005f,
            CarveDepth = 12f,
            RiverbedWidth = 20f,
            BankSmoothness = 0.4f
        };

        public void Validate()
        {
            if (Frequency <= 0.0001f) Frequency = 0.001f;
            if (CarveDepth < 0f) CarveDepth = 0f;
            if (RiverbedWidth < 1f) RiverbedWidth = 1f;
            if (BankSmoothness < 0.01f) BankSmoothness = 0.01f;
        }

        public bool Equals(RiverSettings other)
        {
            return Enabled == other.Enabled &&
                   Seed == other.Seed &&
                   Mathf.Approximately(Frequency, other.Frequency) &&
                   Mathf.Approximately(CarveDepth, other.CarveDepth) &&
                   Mathf.Approximately(RiverbedWidth, other.RiverbedWidth) &&
                   Mathf.Approximately(BankSmoothness, other.BankSmoothness);
        }

        public override bool Equals(object obj) => obj is RiverSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Enabled.GetHashCode();
                hash = hash * 31 + Seed;
                hash = hash * 31 + Frequency.GetHashCode();
                hash = hash * 31 + CarveDepth.GetHashCode();
                hash = hash * 31 + RiverbedWidth.GetHashCode();
                hash = hash * 31 + BankSmoothness.GetHashCode();
                return hash;
            }
        }
    }
}
