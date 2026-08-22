namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    public enum FalloffMode
    {
        None = 0,
        Circular = 1,
        Square = 2
    }

    /// <summary>
    /// Configuration for edge falloff and island shaping masks.
    /// Gradually decreases terrain height towards world bounds or island borders.
    /// </summary>
    [Serializable]
    public struct FalloffSettings : IEquatable<FalloffSettings>
    {
        [Tooltip("Falloff shape mode.")]
        public FalloffMode Mode;

        [Tooltip("Inner radius where falloff begins (in world units).")]
        public float FalloffStartRadius;

        [Tooltip("Outer radius where terrain reaches 0 elevation (in world units).")]
        public float FalloffEndRadius;

        [Tooltip("Power curve exponent controlling the slope of the edge drop-off.")]
        [Range(0.5f, 5f)]
        public float PowerExponent;

        public static FalloffSettings Default => new FalloffSettings
        {
            Mode = FalloffMode.None,
            FalloffStartRadius = 300f,
            FalloffEndRadius = 600f,
            PowerExponent = 2.0f
        };

        public void Validate()
        {
            if (FalloffStartRadius < 0f) FalloffStartRadius = 0f;
            if (FalloffEndRadius <= FalloffStartRadius) FalloffEndRadius = FalloffStartRadius + 50f;
            if (PowerExponent < 0.1f) PowerExponent = 0.1f;
        }

        public bool Equals(FalloffSettings other)
        {
            return Mode == other.Mode &&
                   Mathf.Approximately(FalloffStartRadius, other.FalloffStartRadius) &&
                   Mathf.Approximately(FalloffEndRadius, other.FalloffEndRadius) &&
                   Mathf.Approximately(PowerExponent, other.PowerExponent);
        }

        public override bool Equals(object obj) => obj is FalloffSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)Mode;
                hash = hash * 31 + FalloffStartRadius.GetHashCode();
                hash = hash * 31 + FalloffEndRadius.GetHashCode();
                hash = hash * 31 + PowerExponent.GetHashCode();
                return hash;
            }
        }
    }
}
