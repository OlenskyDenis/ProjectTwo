namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure value struct representing a river channel segment connecting two nodes.
    /// </summary>
    [Serializable]
    public struct RiverSegment : IEquatable<RiverSegment>
    {
        public int Id;
        public int StartNodeId;
        public int EndNodeId;
        public Vector3 StartPosition;
        public Vector3 ControlPoint;
        public Vector3 EndPosition;
        public float Length;
        public float ChannelWidth;
        public float StartWidth;
        public float EndWidth;
        public float CarveDepth;
        public int StreamOrder;
        public float FlowRate;
        public bool IsWaterfall;

        public int FromNodeId => StartNodeId;
        public int ToNodeId => EndNodeId;
        public int SegmentId => Id;
        public float FlowSpeed => FlowRate;

        public RiverSegment(
            int id,
            int startNodeId,
            int endNodeId,
            Vector3 startPosition,
            Vector3 controlPoint,
            Vector3 endPosition,
            float length,
            float channelWidth,
            float carveDepth,
            int streamOrder,
            float flowRate,
            float startWidth = 0f,
            float endWidth = 0f,
            bool isWaterfall = false)
        {
            Id = id;
            StartNodeId = startNodeId;
            EndNodeId = endNodeId;
            StartPosition = startPosition;
            ControlPoint = controlPoint;
            EndPosition = endPosition;
            Length = length;
            ChannelWidth = channelWidth;
            StartWidth = startWidth > 0f ? startWidth : channelWidth;
            EndWidth = endWidth > 0f ? endWidth : channelWidth;
            CarveDepth = carveDepth;
            StreamOrder = streamOrder;
            FlowRate = flowRate;
            IsWaterfall = isWaterfall;
        }

        public bool Equals(RiverSegment other)
        {
            return Id == other.Id &&
                   StartNodeId == other.StartNodeId &&
                   EndNodeId == other.EndNodeId &&
                   StartPosition == other.StartPosition &&
                   ControlPoint == other.ControlPoint &&
                   EndPosition == other.EndPosition &&
                   Mathf.Approximately(Length, other.Length) &&
                   Mathf.Approximately(ChannelWidth, other.ChannelWidth) &&
                   Mathf.Approximately(StartWidth, other.StartWidth) &&
                   Mathf.Approximately(EndWidth, other.EndWidth) &&
                   Mathf.Approximately(CarveDepth, other.CarveDepth) &&
                   StreamOrder == other.StreamOrder &&
                   Mathf.Approximately(FlowRate, other.FlowRate) &&
                   IsWaterfall == other.IsWaterfall;
        }

        public override bool Equals(object obj) => obj is RiverSegment other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Id;
                hash = hash * 31 + StartNodeId;
                hash = hash * 31 + EndNodeId;
                hash = hash * 31 + StartPosition.GetHashCode();
                hash = hash * 31 + ControlPoint.GetHashCode();
                hash = hash * 31 + EndPosition.GetHashCode();
                hash = hash * 31 + Length.GetHashCode();
                hash = hash * 31 + ChannelWidth.GetHashCode();
                hash = hash * 31 + StartWidth.GetHashCode();
                hash = hash * 31 + EndWidth.GetHashCode();
                hash = hash * 31 + CarveDepth.GetHashCode();
                hash = hash * 31 + StreamOrder;
                hash = hash * 31 + FlowRate.GetHashCode();
                hash = hash * 31 + IsWaterfall.GetHashCode();
                return hash;
            }
        }
    }
}
