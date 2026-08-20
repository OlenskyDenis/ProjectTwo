namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure value struct representing a procedural depression lake basin.
    /// </summary>
    [Serializable]
    public struct LakeBasin : IEquatable<LakeBasin>
    {
        public int Id;
        public Vector3 Center;
        public float WaterElevation;
        public float Radius;
        public int OutletNodeId;

        public LakeBasin(int id, Vector3 center, float waterElevation, float radius, int outletNodeId)
        {
            Id = id;
            Center = center;
            WaterElevation = waterElevation;
            Radius = radius;
            OutletNodeId = outletNodeId;
        }

        public bool Equals(LakeBasin other)
        {
            return Id == other.Id &&
                   Center == other.Center &&
                   Mathf.Approximately(WaterElevation, other.WaterElevation) &&
                   Mathf.Approximately(Radius, other.Radius) &&
                   OutletNodeId == other.OutletNodeId;
        }

        public override bool Equals(object obj) => obj is LakeBasin other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Id;
                hash = hash * 31 + Center.GetHashCode();
                hash = hash * 31 + WaterElevation.GetHashCode();
                hash = hash * 31 + Radius.GetHashCode();
                hash = hash * 31 + OutletNodeId;
                return hash;
            }
        }
    }
}
