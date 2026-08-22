namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for global water levels, sea basins, and underwater shaping.
    /// </summary>
    [Serializable]
    public struct WaterSettings : IEquatable<WaterSettings>
    {
        [Tooltip("Enable global sea level and ocean basins.")]
        public bool Enabled;

        [Tooltip("Global sea level elevation (world Y). Ground below this height is treated as ocean/water body.")]
        public float SeaLevel;

        [Tooltip("Additional depression depth for ocean floors.")]
        [Range(0f, 100f)]
        public float OceanFloorDepth;

        [Tooltip("Smoothness factor for shoreline transitions.")]
        [Range(0.01f, 10f)]
        public float ShorelineSmoothness;

        [Tooltip("Optional abstract water surface material descriptor.")]
        public MaterialDescriptor WaterSurfaceMaterial;

        public static WaterSettings Default => new WaterSettings
        {
            Enabled = false,
            SeaLevel = 5f,
            OceanFloorDepth = 10f,
            ShorelineSmoothness = 2f,
            WaterSurfaceMaterial = MaterialDescriptor.DefaultWater
        };

        public void Validate()
        {
            if (OceanFloorDepth < 0f) OceanFloorDepth = 0f;
            if (ShorelineSmoothness < 0.01f) ShorelineSmoothness = 0.01f;
        }

        public bool Equals(WaterSettings other)
        {
            return Enabled == other.Enabled &&
                   Mathf.Approximately(SeaLevel, other.SeaLevel) &&
                   Mathf.Approximately(OceanFloorDepth, other.OceanFloorDepth) &&
                   Mathf.Approximately(ShorelineSmoothness, other.ShorelineSmoothness) &&
                   WaterSurfaceMaterial == other.WaterSurfaceMaterial;
        }

        public override bool Equals(object obj) => obj is WaterSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Enabled.GetHashCode();
                hash = hash * 31 + SeaLevel.GetHashCode();
                hash = hash * 31 + OceanFloorDepth.GetHashCode();
                hash = hash * 31 + ShorelineSmoothness.GetHashCode();
                return hash;
            }
        }
    }
}
