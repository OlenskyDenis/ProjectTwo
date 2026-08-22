namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure value struct representing a structural boundary line segment between two plates.
    /// </summary>
    [Serializable]
    public struct TectonicBoundary : IEquatable<TectonicBoundary>
    {
        public int PlateAId;
        public int PlateBId;
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public TectonicBoundaryType BoundaryType;
        public float CollisionIntensity;
        public float InfluenceRadius;
        public float MaxUplift;

        public TectonicBoundary(
            int plateAId,
            int plateBId,
            Vector2 startPoint,
            Vector2 endPoint,
            TectonicBoundaryType boundaryType,
            float collisionIntensity,
            float influenceRadius,
            float maxUplift)
        {
            PlateAId = plateAId;
            PlateBId = plateBId;
            StartPoint = startPoint;
            EndPoint = endPoint;
            BoundaryType = boundaryType;
            CollisionIntensity = collisionIntensity;
            InfluenceRadius = influenceRadius;
            MaxUplift = maxUplift;
        }

        public bool Equals(TectonicBoundary other)
        {
            return PlateAId == other.PlateAId &&
                   PlateBId == other.PlateBId &&
                   StartPoint == other.StartPoint &&
                   EndPoint == other.EndPoint &&
                   BoundaryType == other.BoundaryType &&
                   Mathf.Approximately(CollisionIntensity, other.CollisionIntensity) &&
                   Mathf.Approximately(InfluenceRadius, other.InfluenceRadius) &&
                   Mathf.Approximately(MaxUplift, other.MaxUplift);
        }

        public override bool Equals(object obj) => obj is TectonicBoundary other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + PlateAId;
                hash = hash * 31 + PlateBId;
                hash = hash * 31 + StartPoint.GetHashCode();
                hash = hash * 31 + EndPoint.GetHashCode();
                hash = hash * 31 + (int)BoundaryType;
                hash = hash * 31 + CollisionIntensity.GetHashCode();
                hash = hash * 31 + InfluenceRadius.GetHashCode();
                hash = hash * 31 + MaxUplift.GetHashCode();
                return hash;
            }
        }
    }
}
