namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for global tectonic macro-zoning and mountain ridge synthesis.
    /// </summary>
    [Serializable]
    public struct TectonicSettings : IEquatable<TectonicSettings>
    {
        [Tooltip("Enable macro-tectonic plate partitioning and ridge uplift.")]
        public bool Enabled;

        [Tooltip("Seed for deterministic tectonic plate distribution.")]
        public int Seed;

        [Tooltip("Number of tectonic plates generated in the macro world partition.")]
        [Range(4, 64)]
        public int PlateCount;

        [Tooltip("World spatial scale of individual tectonic plates.")]
        [Range(200f, 5000f)]
        public float PlateScale;

        [Tooltip("Peak mountain uplift height multiplier along convergent collision boundaries.")]
        [Range(10f, 300f)]
        public float MountainUplift;

        [Tooltip("Maximum trench / valley depression depth along divergent rift zones.")]
        [Range(0f, 150f)]
        public float RiftDepth;

        [Tooltip("Influence width of mountain ranges and boundary transition belts.")]
        [Range(50f, 1000f)]
        public float BoundaryInfluenceWidth;

        [Tooltip("Sharpness exponent of mountain ridge crests.")]
        [Range(0.5f, 4f)]
        public float RidgeSharpness;

        [Tooltip("Domain noise warping amplitude along fault lines.")]
        [Range(0f, 1f)]
        public float FaultNoiseWarp;

        public static TectonicSettings Default => new TectonicSettings
        {
            Enabled = true,
            Seed = 42,
            PlateCount = 16,
            PlateScale = 1000f,
            MountainUplift = 80f,
            RiftDepth = 30f,
            BoundaryInfluenceWidth = 250f,
            RidgeSharpness = 1.5f,
            FaultNoiseWarp = 0.3f
        };

        public void Validate()
        {
            if (PlateCount < 2) PlateCount = 2;
            if (PlateScale < 50f) PlateScale = 50f;
            if (MountainUplift < 0f) MountainUplift = 0f;
            if (RiftDepth < 0f) RiftDepth = 0f;
            if (BoundaryInfluenceWidth < 10f) BoundaryInfluenceWidth = 10f;
            if (RidgeSharpness < 0.1f) RidgeSharpness = 0.1f;
            if (FaultNoiseWarp < 0f) FaultNoiseWarp = 0f;
        }

        public bool Equals(TectonicSettings other)
        {
            return Enabled == other.Enabled &&
                   Seed == other.Seed &&
                   PlateCount == other.PlateCount &&
                   Mathf.Approximately(PlateScale, other.PlateScale) &&
                   Mathf.Approximately(MountainUplift, other.MountainUplift) &&
                   Mathf.Approximately(RiftDepth, other.RiftDepth) &&
                   Mathf.Approximately(BoundaryInfluenceWidth, other.BoundaryInfluenceWidth) &&
                   Mathf.Approximately(RidgeSharpness, other.RidgeSharpness) &&
                   Mathf.Approximately(FaultNoiseWarp, other.FaultNoiseWarp);
        }

        public override bool Equals(object obj) => obj is TectonicSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Enabled.GetHashCode();
                hash = hash * 31 + Seed;
                hash = hash * 31 + PlateCount;
                hash = hash * 31 + PlateScale.GetHashCode();
                hash = hash * 31 + MountainUplift.GetHashCode();
                hash = hash * 31 + RiftDepth.GetHashCode();
                hash = hash * 31 + BoundaryInfluenceWidth.GetHashCode();
                hash = hash * 31 + RidgeSharpness.GetHashCode();
                hash = hash * 31 + FaultNoiseWarp.GetHashCode();
                return hash;
            }
        }
    }
}
