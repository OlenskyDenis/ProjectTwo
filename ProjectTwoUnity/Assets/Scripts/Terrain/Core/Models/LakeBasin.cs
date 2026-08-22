namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using System.Collections.Generic;
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
        public float Capacity;
        public Vector3[] PerimeterPoints;
        public int InflowCount;
        public bool IsTerminalLake;

        public float WaterLevel => WaterElevation;
        public int? OutflowNodeId => OutletNodeId >= 0 ? (int?)OutletNodeId : null;

        public LakeBasin(
            int id,
            Vector3 center,
            float waterElevation,
            float radius,
            int outletNodeId,
            float capacity = 0f,
            Vector3[] perimeterPoints = null,
            int inflowCount = 1,
            bool isTerminalLake = false)
        {
            Id = id;
            Center = center;
            WaterElevation = waterElevation;
            Radius = radius;
            OutletNodeId = outletNodeId;
            Capacity = capacity;
            PerimeterPoints = perimeterPoints ?? Array.Empty<Vector3>();
            InflowCount = inflowCount;
            IsTerminalLake = isTerminalLake;
        }

        public bool Equals(LakeBasin other)
        {
            return Id == other.Id &&
                   Center == other.Center &&
                   Mathf.Approximately(WaterElevation, other.WaterElevation) &&
                   Mathf.Approximately(Radius, other.Radius) &&
                   OutletNodeId == other.OutletNodeId &&
                   Mathf.Approximately(Capacity, other.Capacity) &&
                   InflowCount == other.InflowCount &&
                   IsTerminalLake == other.IsTerminalLake;
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
                hash = hash * 31 + Capacity.GetHashCode();
                hash = hash * 31 + InflowCount;
                hash = hash * 31 + IsTerminalLake.GetHashCode();
                return hash;
            }
        }
    }
}
