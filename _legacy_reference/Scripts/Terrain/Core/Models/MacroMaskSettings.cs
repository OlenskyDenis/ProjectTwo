namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for secondary low-frequency continental / mountain mask noise.
    /// Used to amplify high mountain ridges while keeping valley/lowland plains flat.
    /// </summary>
    [Serializable]
    public struct MacroMaskSettings : IEquatable<MacroMaskSettings>
    {
        [Tooltip("Enable macro continent/mountain masking.")]
        public bool Enabled;

        [Tooltip("Seed offset for the macro noise generator.")]
        public int Seed;

        [Tooltip("Frequency scale of the macro mask (typically larger/zoomed out, e.g. 200-800).")]
        [Range(10f, 2000f)]
        public float Scale;

        [Tooltip("Multiplier applied to terrain height in designated mountain regions.")]
        [Range(1f, 5f)]
        public float MountainAmplification;

        [Tooltip("Height multiplier applied in valley/flat regions (0.0 to 1.0).")]
        [Range(0f, 1f)]
        public float ValleyDamping;

        [Tooltip("Non-linear power exponent to sharpen the contrast between mountains and plains.")]
        [Range(0.5f, 4f)]
        public float PowerExponent;

        public static MacroMaskSettings Default => new MacroMaskSettings
        {
            Enabled = false,
            Seed = 999,
            Scale = 400f,
            MountainAmplification = 2.5f,
            ValleyDamping = 0.3f,
            PowerExponent = 1.5f
        };

        public void Validate()
        {
            if (Scale < 1f) Scale = 1f;
            if (MountainAmplification < 1f) MountainAmplification = 1f;
            if (ValleyDamping < 0f) ValleyDamping = 0f;
            if (ValleyDamping > 1f) ValleyDamping = 1f;
            if (PowerExponent < 0.1f) PowerExponent = 0.1f;
        }

        public bool Equals(MacroMaskSettings other)
        {
            return Enabled == other.Enabled &&
                   Seed == other.Seed &&
                   Mathf.Approximately(Scale, other.Scale) &&
                   Mathf.Approximately(MountainAmplification, other.MountainAmplification) &&
                   Mathf.Approximately(ValleyDamping, other.ValleyDamping) &&
                   Mathf.Approximately(PowerExponent, other.PowerExponent);
        }

        public override bool Equals(object obj) => obj is MacroMaskSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Enabled.GetHashCode();
                hash = hash * 31 + Seed;
                hash = hash * 31 + Scale.GetHashCode();
                hash = hash * 31 + MountainAmplification.GetHashCode();
                hash = hash * 31 + ValleyDamping.GetHashCode();
                hash = hash * 31 + PowerExponent.GetHashCode();
                return hash;
            }
        }
    }
}
