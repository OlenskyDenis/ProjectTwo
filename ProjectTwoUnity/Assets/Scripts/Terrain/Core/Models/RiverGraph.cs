namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Thread-safe container encapsulating nodes, segments, and spatial hash grid index for fast chunk queries.
    /// </summary>
    public class RiverGraph
    {
        public readonly RiverNode[] Nodes;
        public readonly RiverSegment[] Segments;
        public readonly LakeBasin[] Lakes;

        private readonly Dictionary<int, List<int>> _spatialHashGrid;
        private readonly float _cellSize;

        public const float DefaultCellSize = 128f;

        public RiverGraph(
            RiverNode[] nodes,
            RiverSegment[] segments,
            LakeBasin[] lakes,
            float cellSize = DefaultCellSize)
        {
            Nodes = nodes ?? Array.Empty<RiverNode>();
            Segments = segments ?? Array.Empty<RiverSegment>();
            Lakes = lakes ?? Array.Empty<LakeBasin>();
            _cellSize = Mathf.Max(16f, cellSize);
            _spatialHashGrid = new Dictionary<int, List<int>>();

            BuildSpatialIndex();
        }

        public static RiverGraph Empty => new RiverGraph(null, null, null);

        public int NodeCount => Nodes.Length;
        public int SegmentCount => Segments.Length;
        public int LakeCount => Lakes.Length;

        private void BuildSpatialIndex()
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                ref readonly RiverSegment seg = ref Segments[i];
                GetBoundingCellRange(seg.StartPosition, seg.ControlPoint, seg.EndPosition, seg.ChannelWidth * 2f,
                    out int minCellX, out int minCellZ, out int maxCellX, out int maxCellZ);

                for (int cx = minCellX; cx <= maxCellX; cx++)
                {
                    for (int cz = minCellZ; cz <= maxCellZ; cz++)
                    {
                        int key = GetCellHash(cx, cz);
                        if (!_spatialHashGrid.TryGetValue(key, out var list))
                        {
                            list = new List<int>(4);
                            _spatialHashGrid[key] = list;
                        }
                        list.Add(i);
                    }
                }
            }
        }

        private void GetBoundingCellRange(
            Vector3 p0, Vector3 p1, Vector3 p2, float padding,
            out int minCellX, out int minCellZ, out int maxCellX, out int maxCellZ)
        {
            float minX = Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)) - padding;
            float maxX = Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)) + padding;
            float minZ = Mathf.Min(p0.z, Mathf.Min(p1.z, p2.z)) - padding;
            float maxZ = Mathf.Max(p0.z, Mathf.Max(p1.z, p2.z)) + padding;

            minCellX = Mathf.FloorToInt(minX / _cellSize);
            maxCellX = Mathf.FloorToInt(maxX / _cellSize);
            minCellZ = Mathf.FloorToInt(minZ / _cellSize);
            maxCellZ = Mathf.FloorToInt(maxZ / _cellSize);
        }

        public static int GetCellHash(int cellX, int cellZ)
        {
            unchecked
            {
                return cellX * 73856093 ^ cellZ * 19349663;
            }
        }

        /// <summary>
        /// Retrieves all candidate river segments overlapping the given 2D world bounding box.
        /// </summary>
        public void QuerySegmentsInBounds(
            float minX, float minZ, float maxX, float maxZ,
            List<int> resultSegmentIndices)
        {
            if (resultSegmentIndices == null) return;
            resultSegmentIndices.Clear();

            int minCellX = Mathf.FloorToInt(minX / _cellSize);
            int maxCellX = Mathf.FloorToInt(maxX / _cellSize);
            int minCellZ = Mathf.FloorToInt(minZ / _cellSize);
            int maxCellZ = Mathf.FloorToInt(maxZ / _cellSize);

            HashSet<int> visited = null;

            for (int cx = minCellX; cx <= maxCellX; cx++)
            {
                for (int cz = minCellZ; cz <= maxCellZ; cz++)
                {
                    int key = GetCellHash(cx, cz);
                    if (_spatialHashGrid.TryGetValue(key, out var list))
                    {
                        if (visited == null) visited = new HashSet<int>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            int segIndex = list[i];
                            if (visited.Add(segIndex))
                            {
                                resultSegmentIndices.Add(segIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
