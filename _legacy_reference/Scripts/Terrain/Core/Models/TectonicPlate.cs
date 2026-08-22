namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure value struct representing a macro tectonic crustal plate.
    /// </summary>
    [Serializable]
    public struct TectonicPlate : IEquatable<TectonicPlate>
    {
        public int Id;
        public Vector2 Centroid;
        public Vector2 DriftVelocity;
        public PlateCrustType CrustType;
        public float BaseElevation;

        public TectonicPlate(int id, Vector2 centroid, Vector2 driftVelocity, PlateCrustType crustType, float baseElevation)
        {
            Id = id;
            Centroid = centroid;
            DriftVelocity = driftVelocity;
            CrustType = crustType;
            BaseElevation = baseElevation;
        }

        public bool Equals(TectonicPlate other)
        {
            return Id == other.Id &&
                   Centroid == other.Centroid &&
                   DriftVelocity == other.DriftVelocity &&
                   CrustType == other.CrustType &&
                   Mathf.Approximately(BaseElevation, other.BaseElevation);
        }

        public override bool Equals(object obj) => obj is TectonicPlate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Id;
                hash = hash * 31 + Centroid.GetHashCode();
                hash = hash * 31 + DriftVelocity.GetHashCode();
                hash = hash * 31 + (int)CrustType;
                hash = hash * 31 + BaseElevation.GetHashCode();
                return hash;
            }
        }
    }
}
