namespace ProjectTwo.Terrain.Core.Models
{
    using System;

    /// <summary>
    /// Lightweight, engine-agnostic descriptor that identifies a visual material/shading profile
    /// without coupling core domain calculations to UnityEngine rendering objects.
    /// </summary>
    [Serializable]
    public readonly struct MaterialDescriptor : IEquatable<MaterialDescriptor>
    {
        public static readonly MaterialDescriptor Default = new MaterialDescriptor("default_terrain", "Default", 0);
        public static readonly MaterialDescriptor DefaultWater = new MaterialDescriptor("default_water", "Water", 0);

        /// <summary>
        /// Unique identifier for this material profile/preset (e.g. "alpine_biome", "desert_rock").
        /// </summary>
        public string DescriptorId { get; }

        /// <summary>
        /// Human-readable label or display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Hash code representing visual variant or parameter checksum.
        /// </summary>
        public int VariantHash { get; }

        public MaterialDescriptor(string descriptorId, string displayName = null, int variantHash = 0)
        {
            DescriptorId = string.IsNullOrEmpty(descriptorId) ? "default" : descriptorId;
            DisplayName = displayName ?? DescriptorId;
            VariantHash = variantHash;
        }

        public bool Equals(MaterialDescriptor other)
        {
            return string.Equals(DescriptorId, other.DescriptorId, StringComparison.OrdinalIgnoreCase) &&
                   VariantHash == other.VariantHash;
        }

        public override bool Equals(object obj)
        {
            return obj is MaterialDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (DescriptorId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(DescriptorId) : 0);
                hash = (hash * 397) ^ VariantHash;
                return hash;
            }
        }

        public static bool operator ==(MaterialDescriptor left, MaterialDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MaterialDescriptor left, MaterialDescriptor right)
        {
            return !left.Equals(right);
        }

        public override string ToString() => $"{DisplayName} ({DescriptorId})";
    }
}
