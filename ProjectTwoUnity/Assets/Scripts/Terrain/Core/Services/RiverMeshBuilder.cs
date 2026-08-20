namespace ProjectTwo.Terrain.Core.Services
{
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe procedural mesh builder generating sloped river water surface ribbons and lake meshes with flow UVs.
    /// Accurately maps world-space river splines to chunk local coordinates centered around (0,0).
    /// </summary>
    public class RiverMeshBuilder : IRiverMeshBuilder
    {
        public RiverWaterMeshData BuildChunkRiverMesh(
            ChunkCoordinate coordinate,
            float chunkSize,
            RiverGraph riverGraph,
            HydrologySettings settings,
            WaterSettings water)
        {
            if (riverGraph == null || riverGraph.SegmentCount == 0)
            {
                return RiverWaterMeshData.Empty;
            }

            float halfSize = chunkSize * 0.5f;
            float centerX = coordinate.X * chunkSize;
            float centerZ = coordinate.Z * chunkSize;

            float minX = centerX - halfSize;
            float minZ = centerZ - halfSize;
            float maxX = centerX + halfSize;
            float maxZ = centerZ + halfSize;

            var intersectingSegmentIndices = new List<int>(16);
            riverGraph.QuerySegmentsInBounds(minX - 40f, minZ - 40f, maxX + 40f, maxZ + 40f, intersectingSegmentIndices);

            if (intersectingSegmentIndices.Count == 0)
            {
                return RiverWaterMeshData.Empty;
            }

            var vertices = new List<Vector3>(intersectingSegmentIndices.Count * 32);
            var normals = new List<Vector3>(intersectingSegmentIndices.Count * 32);
            var uvs = new List<Vector2>(intersectingSegmentIndices.Count * 32);
            var triangles = new List<int>(intersectingSegmentIndices.Count * 48);

            Vector3 chunkOrigin = new Vector3(centerX, 0f, centerZ);
            const int Subdivisions = 8;

            for (int s = 0; s < intersectingSegmentIndices.Count; s++)
            {
                int segIdx = intersectingSegmentIndices[s];
                ref readonly RiverSegment seg = ref riverGraph.Segments[segIdx];

                float halfWidth = seg.ChannelWidth * 0.5f;
                int baseVertexIndex = vertices.Count;

                float cumulativeDist = 0f;
                Vector3 prevCenter = Vector3.zero;

                for (int step = 0; step <= Subdivisions; step++)
                {
                    float t = (float)step / Subdivisions;
                    Vector3 center = SampleQuadraticBezier(seg.StartPosition, seg.ControlPoint, seg.EndPosition, t);
                    Vector3 tangent = SampleQuadraticBezierTangent(seg.StartPosition, seg.ControlPoint, seg.EndPosition, t).normalized;
                    if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;

                    if (step > 0)
                    {
                        cumulativeDist += Vector3.Distance(prevCenter, center);
                    }
                    prevCenter = center;

                    Vector3 lateral = new Vector3(-tangent.z, 0f, tangent.x).normalized;

                    // Convert to chunk local space centered at (0,0)
                    Vector3 localCenter = center - chunkOrigin;
                    // Slightly raise water surface above the carved channel base
                    localCenter.y += 0.2f;

                    Vector3 leftPos = localCenter - lateral * halfWidth;
                    Vector3 rightPos = localCenter + lateral * halfWidth;

                    float vCoord = cumulativeDist * 0.04f;

                    vertices.Add(leftPos);
                    vertices.Add(rightPos);

                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);

                    uvs.Add(new Vector2(0f, vCoord));
                    uvs.Add(new Vector2(1f, vCoord));

                    if (step < Subdivisions)
                    {
                        int currentLeft = baseVertexIndex + step * 2;
                        int currentRight = currentLeft + 1;
                        int nextLeft = currentLeft + 2;
                        int nextRight = currentLeft + 3;

                        triangles.Add(currentLeft);
                        triangles.Add(nextLeft);
                        triangles.Add(currentRight);

                        triangles.Add(currentRight);
                        triangles.Add(nextLeft);
                        triangles.Add(nextRight);
                    }
                }
            }

            return new RiverWaterMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                uvs.ToArray(),
                triangles.ToArray());
        }

        private static Vector3 SampleQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector3 SampleQuadraticBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }
    }
}
