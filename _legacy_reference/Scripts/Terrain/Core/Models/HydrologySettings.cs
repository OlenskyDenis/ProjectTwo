namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for vector river network pathfinding, flow accumulation, waterfall dynamics, lake cascades, and deltas.
    /// </summary>
    [Serializable]
    public struct HydrologySettings : IEquatable<HydrologySettings>
    {
        [Tooltip("Enable global vector river graph pathfinding and hydraulic carving.")]
        public bool Enabled;

        [Tooltip("Randomization seed for river network generation.")]
        public int Seed;

        [Tooltip("Target count of river source springs spawned in mountain catchments.")]
        [Range(1, 100)]
        public int SourceCount;

        [Tooltip("Minimum normalized elevation ratio for river sources (0 = sea level, 1 = max peaks).")]
        [Range(0.2f, 0.95f)]
        public float MinSourceElevationRatio;

        [Tooltip("Baseline channel width in world units for headwater mountain streams.")]
        [Range(1f, 50f)]
        public float BaseRiverWidth;

        [Tooltip("Width growth multiplier per Strahler stream order level.")]
        [Range(1f, 3f)]
        public float WidthGrowthFactor;

        [Tooltip("Maximum vertical carve depth in world units.")]
        [Range(1f, 50f)]
        public float BaseCarveDepth;

        [Tooltip("Lateral bank transition slope smoothness.")]
        [Range(0.05f, 1f)]
        public float BankSmoothness;

        [Tooltip("Amplitude and frequency of river meandering bends.")]
        [Range(0f, 1f)]
        public float MeanderIntensity;

        [Tooltip("Minimum depression depth required to instantiate a procedural lake basin.")]
        [Range(2f, 50f)]
        public float LakeMinDepthThreshold;

        [Tooltip("Subdivision step size in world units for cliff-conforming waterfalls (slopes >25°).")]
        [Range(0.5f, 10f)]
        public float WaterfallStepSize;

        [Tooltip("Inertial velocity blending factor (0 = pure steepest descent, 1 = pure forward momentum).")]
        [Range(0f, 1f)]
        public float HydraulicMomentum;

        [Tooltip("Probability of lowland channels splitting into coastal delta distributary branches.")]
        [Range(0f, 1f)]
        public float DeltaBranchingChance;

        public static HydrologySettings Default => new HydrologySettings
        {
            Enabled = true,
            Seed = 777,
            SourceCount = 20,
            MinSourceElevationRatio = 0.55f,
            BaseRiverWidth = 8f,
            WidthGrowthFactor = 1.6f,
            BaseCarveDepth = 12f,
            BankSmoothness = 0.4f,
            MeanderIntensity = 0.35f,
            LakeMinDepthThreshold = 8f,
            WaterfallStepSize = 1.5f,
            HydraulicMomentum = 0.45f,
            DeltaBranchingChance = 0.25f
        };

        public void Validate()
        {
            if (SourceCount < 1) SourceCount = 1;
            if (MinSourceElevationRatio < 0.01f) MinSourceElevationRatio = 0.01f;
            if (BaseRiverWidth < 1f) BaseRiverWidth = 1f;
            if (WidthGrowthFactor < 1f) WidthGrowthFactor = 1f;
            if (BaseCarveDepth < 0f) BaseCarveDepth = 0f;
            if (BankSmoothness < 0.01f) BankSmoothness = 0.01f;
            if (MeanderIntensity < 0f) MeanderIntensity = 0f;
            if (LakeMinDepthThreshold < 1f) LakeMinDepthThreshold = 1f;
            if (WaterfallStepSize < 0.5f) WaterfallStepSize = 0.5f;
            HydraulicMomentum = Mathf.Clamp01(HydraulicMomentum);
            DeltaBranchingChance = Mathf.Clamp01(DeltaBranchingChance);
        }

        public bool Equals(HydrologySettings other)
        {
            return Enabled == other.Enabled &&
                   Seed == other.Seed &&
                   SourceCount == other.SourceCount &&
                   Mathf.Approximately(MinSourceElevationRatio, other.MinSourceElevationRatio) &&
                   Mathf.Approximately(BaseRiverWidth, other.BaseRiverWidth) &&
                   Mathf.Approximately(WidthGrowthFactor, other.WidthGrowthFactor) &&
                   Mathf.Approximately(BaseCarveDepth, other.BaseCarveDepth) &&
                   Mathf.Approximately(BankSmoothness, other.BankSmoothness) &&
                   Mathf.Approximately(MeanderIntensity, other.MeanderIntensity) &&
                   Mathf.Approximately(LakeMinDepthThreshold, other.LakeMinDepthThreshold) &&
                   Mathf.Approximately(WaterfallStepSize, other.WaterfallStepSize) &&
                   Mathf.Approximately(HydraulicMomentum, other.HydraulicMomentum) &&
                   Mathf.Approximately(DeltaBranchingChance, other.DeltaBranchingChance);
        }

        public override bool Equals(object obj) => obj is HydrologySettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Enabled.GetHashCode();
                hash = hash * 31 + Seed;
                hash = hash * 31 + SourceCount;
                hash = hash * 31 + MinSourceElevationRatio.GetHashCode();
                hash = hash * 31 + BaseRiverWidth.GetHashCode();
                hash = hash * 31 + WidthGrowthFactor.GetHashCode();
                hash = hash * 31 + BaseCarveDepth.GetHashCode();
                hash = hash * 31 + BankSmoothness.GetHashCode();
                hash = hash * 31 + MeanderIntensity.GetHashCode();
                hash = hash * 31 + LakeMinDepthThreshold.GetHashCode();
                hash = hash * 31 + WaterfallStepSize.GetHashCode();
                hash = hash * 31 + HydraulicMomentum.GetHashCode();
                hash = hash * 31 + DeltaBranchingChance.GetHashCode();
                return hash;
            }
        }
    }
}
