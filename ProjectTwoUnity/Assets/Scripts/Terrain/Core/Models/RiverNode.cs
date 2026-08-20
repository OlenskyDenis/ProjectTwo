namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure value struct representing a topological point / junction in the river network.
    /// </summary>
    [Serializable]
    public struct RiverNode : IEquatable<RiverNode>
    {
        public int Id;
        public Vector3 Position;
        public RiverNodeType NodeType;
        public float Elevation;
        public float FlowAccumulation;
        public int StreamOrder;

        public RiverNode(
            int id,
            Vector3 position,
            RiverNodeType nodeType,
            float elevation,
            float flowAccumulation,
            int streamOrder)
        {
            Id = id;
            Position = position;
            NodeType = nodeType;
            Elevation = elevation;
            FlowAccumulation = flowAccumulation;
            StreamOrder = streamOrder;
        }

        public bool Equals(RiverNode other)
        {
            return Id == other.Id &&
                   Position == other.Position &&
                   NodeType == other.NodeType &&
                   Mathf.Approximately(Elevation, other.Elevation) &&
                   Mathf.Approximately(FlowAccumulation, other.FlowAccumulation) &&
                   StreamOrder == other.StreamOrder;
        }

        public override bool Equals(object obj) => obj is RiverNode other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Id;
                hash = hash * 31 + Position.GetHashCode();
                hash = hash * 31 + (int)NodeType;
                hash = hash * 31 + Elevation.GetHashCode();
                hash = hash * 31 + FlowAccumulation.GetHashCode();
                hash = hash * 31 + StreamOrder;
                return hash;
            }
        }
    }
}
