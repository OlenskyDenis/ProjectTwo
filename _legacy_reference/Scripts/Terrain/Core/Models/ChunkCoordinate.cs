namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Identifies a spatial terrain chunk in 2D discrete grid space.
    /// </summary>
    [Serializable]
    public readonly struct ChunkCoordinate : IEquatable<ChunkCoordinate>
    {
        public int X { get; }
        public int Z { get; }

        public ChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static ChunkCoordinate FromWorldPosition(Vector3 worldPosition, float chunkSize)
        {
            if (chunkSize <= 0f) chunkSize = 240f;
            int x = Mathf.RoundToInt(worldPosition.x / chunkSize);
            int z = Mathf.RoundToInt(worldPosition.z / chunkSize);
            return new ChunkCoordinate(x, z);
        }

        public Vector3 ToWorldPosition(float chunkSize)
        {
            return new Vector3(X * chunkSize, 0f, Z * chunkSize);
        }

        public float DistanceTo(Vector3 worldPosition, float chunkSize)
        {
            Vector3 chunkCenter = ToWorldPosition(chunkSize);
            Vector2 chunkPos2D = new Vector2(chunkCenter.x, chunkCenter.z);
            Vector2 viewerPos2D = new Vector2(worldPosition.x, worldPosition.z);
            return Vector2.Distance(chunkPos2D, viewerPos2D);
        }

        public bool Equals(ChunkCoordinate other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is ChunkCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public override string ToString() => $"Chunk({X}, {Z})";

        public static bool operator ==(ChunkCoordinate left, ChunkCoordinate right) => left.Equals(right);
        public static bool operator !=(ChunkCoordinate left, ChunkCoordinate right) => !left.Equals(right);
    }
}
