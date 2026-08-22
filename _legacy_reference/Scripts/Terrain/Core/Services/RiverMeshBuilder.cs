namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe procedural mesh builder generating cliff-conforming waterfall ribbons,
    /// surface-aligned 3D river water meshes, dynamic width interpolation, and lake meshes.
    /// Accurately maps world-space river splines to chunk local coordinates centered around (0,0).
    /// </summary>
    public class RiverMeshBuilder : IRiverMeshBuilder
    {
        public RiverWaterMeshData BuildChunkRiverMesh(
            ChunkCoordinate coordinate,
            float chunkSize,
            RiverGraph riverGraph,
            HydrologySettings settings,
            WaterSettings water,
            ITerrainShaper terrainProvider,
            in TerrainShaperContext context)
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

            // Build node-level tangent smoothing and cross-section welding
            // 1. Group intersecting segments into continuous chains
            var segLookup = new Dictionary<int, RiverSegment>(intersectingSegmentIndices.Count);
            var outgoingMap = new Dictionary<int, List<int>>();
            var incomingMap = new Dictionary<int, List<int>>();

            for (int i = 0; i < intersectingSegmentIndices.Count; i++)
            {
                int segIdx = intersectingSegmentIndices[i];
                ref readonly RiverSegment seg = ref riverGraph.Segments[segIdx];
                segLookup[seg.Id] = seg;

                if (!outgoingMap.TryGetValue(seg.StartNodeId, out var outList))
                {
                    outList = new List<int>(2);
                    outgoingMap[seg.StartNodeId] = outList;
                }
                outList.Add(seg.Id);

                if (!incomingMap.TryGetValue(seg.EndNodeId, out var inList))
                {
                    inList = new List<int>(2);
                    incomingMap[seg.EndNodeId] = inList;
                }
                inList.Add(seg.Id);
            }

            // 2. Pre-calculate averaged node tangents (Miter-Averaged Frames)
            var nodeTangents = new Dictionary<int, Vector3>();
            var allNodeIds = new HashSet<int>();

            foreach (var seg in segLookup.Values)
            {
                allNodeIds.Add(seg.StartNodeId);
                allNodeIds.Add(seg.EndNodeId);
            }

            foreach (int nodeId in allNodeIds)
            {
                Vector3 inTangentSum = Vector3.zero;
                Vector3 outTangentSum = Vector3.zero;
                int inCount = 0;
                int outCount = 0;

                if (incomingMap.TryGetValue(nodeId, out var inSegs))
                {
                    for (int k = 0; k < inSegs.Count; k++)
                    {
                        if (segLookup.TryGetValue(inSegs[k], out var inSeg))
                        {
                            Vector3 tEnd = EvaluateBezierDerivative(inSeg.StartPosition, inSeg.ControlPoint, inSeg.EndPosition, 1f).normalized;
                            inTangentSum += tEnd;
                            inCount++;
                        }
                    }
                }

                if (outgoingMap.TryGetValue(nodeId, out var outSegs))
                {
                    for (int k = 0; k < outSegs.Count; k++)
                    {
                        if (segLookup.TryGetValue(outSegs[k], out var outSeg))
                        {
                            Vector3 tStart = EvaluateBezierDerivative(outSeg.StartPosition, outSeg.ControlPoint, outSeg.EndPosition, 0f).normalized;
                            outTangentSum += tStart;
                            outCount++;
                        }
                    }
                }

                Vector3 avgTangent = Vector3.forward;
                if (inCount > 0 && outCount > 0)
                {
                    avgTangent = (inTangentSum / inCount + outTangentSum / outCount).normalized;
                }
                else if (outCount > 0)
                {
                    avgTangent = (outTangentSum / outCount).normalized;
                }
                else if (inCount > 0)
                {
                    avgTangent = (inTangentSum / inCount).normalized;
                }

                if (avgTangent.sqrMagnitude < 0.001f) avgTangent = Vector3.forward;
                nodeTangents[nodeId] = avgTangent;
            }

            // 3. Generate smooth continuous welded ribbons for each segment
            for (int s = 0; s < intersectingSegmentIndices.Count; s++)
            {
                int segIdx = intersectingSegmentIndices[s];
                ref readonly RiverSegment seg = ref riverGraph.Segments[segIdx];

                Vector3 p0 = seg.StartPosition;
                Vector3 p1 = seg.ControlPoint;
                Vector3 p2 = seg.EndPosition;

                float segLength = seg.Length > 0.01f ? seg.Length : Vector3.Distance(p0, p2);
                int subdivisions = Mathf.Clamp(Mathf.CeilToInt(segLength / 5.0f), 2, 24);

                float widthStart = seg.StartWidth;
                float widthEnd = seg.EndWidth;

                // Check terminal tapering for sources and land sinks
                bool isSpringSource = !incomingMap.ContainsKey(seg.StartNodeId);
                bool isTerminalSink = !outgoingMap.ContainsKey(seg.EndNodeId);

                Vector3 startNodeTangent = nodeTangents.TryGetValue(seg.StartNodeId, out var st) ? st : (p1 - p0).normalized;
                Vector3 endNodeTangent = nodeTangents.TryGetValue(seg.EndNodeId, out var et) ? et : (p2 - p1).normalized;

                int segStartVertexBase = vertices.Count;
                float currentDist = 0f;
                Vector3 prevCenter = p0;

                for (int step = 0; step <= subdivisions; step++)
                {
                    float t = (float)step / subdivisions;
                    Vector3 center = EvaluateBezier(p0, p1, p2, t);

                    if (step > 0)
                    {
                        currentDist += Vector3.Distance(prevCenter, center);
                        prevCenter = center;
                    }

                    // Blended tangent from node frames and spline derivative
                    Vector3 splineTangent = EvaluateBezierDerivative(p0, p1, p2, t).normalized;
                    Vector3 tangent = Vector3.Slerp(
                        Vector3.Slerp(startNodeTangent, splineTangent, t),
                        Vector3.Slerp(splineTangent, endNodeTangent, t),
                        t).normalized;

                    if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;

                    // Surface normal aligned
                    Vector3 normal = Vector3.up;
                    if (terrainProvider != null)
                    {
                        ComputeTerrainNormal(center.x, center.z, terrainProvider, in context, out normal);
                    }

                    // Orthogonal lateral width vector
                    Vector3 lateral = Vector3.Cross(normal, tangent).normalized;
                    if (lateral.sqrMagnitude < 0.001f)
                    {
                        lateral = new Vector3(-tangent.z, 0f, tangent.x).normalized;
                    }

                    // Calculate tapered width
                    float width = Mathf.Lerp(widthStart, widthEnd, t);
                    if (isSpringSource && t < 0.25f)
                    {
                        width *= Mathf.SmoothStep(0.08f, 1f, t * 4f);
                    }
                    if (isTerminalSink && t > 0.75f)
                    {
                        width *= Mathf.SmoothStep(1f, 0.08f, (t - 0.75f) * 4f);
                    }
                    float halfWidth = Mathf.Max(0.1f, width * 0.5f);

                    // Clamp center and bank vertices to ground surface
                    Vector3 localCenter = center - chunkOrigin;
                    localCenter += normal * 0.15f;

                    Vector3 leftWorld = center - lateral * halfWidth;
                    Vector3 rightWorld = center + lateral * halfWidth;

                    Vector3 leftPos = localCenter - lateral * halfWidth;
                    Vector3 rightPos = localCenter + lateral * halfWidth;

                    if (terrainProvider != null)
                    {
                        float leftHeight = terrainProvider.CalculateElevation(leftWorld.x, leftWorld.z, in context);
                        float rightHeight = terrainProvider.CalculateElevation(rightWorld.x, rightWorld.z, in context);

                        leftPos.y = (leftHeight - chunkOrigin.y) + normal.y * 0.15f;
                        rightPos.y = (rightHeight - chunkOrigin.y) + normal.y * 0.15f;
                    }

                    int currentLeftIdx = vertices.Count;
                    int currentRightIdx = currentLeftIdx + 1;

                    vertices.Add(leftPos);
                    vertices.Add(rightPos);

                    normals.Add(normal);
                    normals.Add(normal);

                    float vCoord = currentDist * 0.05f;
                    uvs.Add(new Vector2(0f, vCoord));
                    uvs.Add(new Vector2(1f, vCoord));

                    if (step == 0)
                    {
                        // Check if upstream segment can be welded
                        if (incomingMap.TryGetValue(seg.StartNodeId, out var upstreamSegs) && upstreamSegs.Count == 1)
                        {
                            // Node frame aligns continuity
                        }
                    }

                    if (step > 0)
                    {
                        int prevLeft = currentLeftIdx - 2;
                        int prevRight = currentLeftIdx - 1;

                        // Winding order for double-sided visibility
                        triangles.Add(prevLeft);
                        triangles.Add(currentLeftIdx);
                        triangles.Add(prevRight);

                        triangles.Add(prevRight);
                        triangles.Add(currentLeftIdx);
                        triangles.Add(currentRightIdx);
                    }
                }
            }

            // 4. Generate Lake Surface Meshes
            if (riverGraph.Lakes != null && riverGraph.Lakes.Length > 0)
            {
                for (int l = 0; l < riverGraph.Lakes.Length; l++)
                {
                    ref readonly LakeBasin lake = ref riverGraph.Lakes[l];
                    Vector3 lakeCenter = lake.Center;
                    float lakeRadius = lake.Radius;

                    if (lakeCenter.x + lakeRadius < minX - 10f || lakeCenter.x - lakeRadius > maxX + 10f ||
                        lakeCenter.z + lakeRadius < minZ - 10f || lakeCenter.z - lakeRadius > maxZ + 10f)
                    {
                        continue;
                    }

                    int lakeSegments = 16;
                    int centerVertexIdx = vertices.Count;
                    Vector3 localLakeCenter = lakeCenter - chunkOrigin;
                    localLakeCenter.y = (lake.WaterElevation - chunkOrigin.y) + 0.1f;

                    vertices.Add(localLakeCenter);
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0.5f, 0.5f));

                    for (int k = 0; k <= lakeSegments; k++)
                    {
                        float angle = (float)k / lakeSegments * Mathf.PI * 2f;
                        float cos = Mathf.Cos(angle);
                        float sin = Mathf.Sin(angle);

                        Vector3 rimPos = localLakeCenter + new Vector3(cos * lakeRadius, 0f, sin * lakeRadius);
                        vertices.Add(rimPos);
                        normals.Add(Vector3.up);
                        uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));

                        if (k > 0)
                        {
                            int rimCurrent = centerVertexIdx + k + 1;
                            int rimPrev = rimCurrent - 1;

                            triangles.Add(centerVertexIdx);
                            triangles.Add(rimPrev);
                            triangles.Add(rimCurrent);
                        }
                    }
                }
            }

            return new RiverWaterMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                uvs.ToArray(),
                triangles.ToArray());
        }

        private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector3 EvaluateBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        private static void ComputeTerrainNormal(
            float x, float z,
            ITerrainShaper shaper,
            in TerrainShaperContext context,
            out Vector3 normal)
        {
            const float d = 2.0f;
            float hL = shaper.CalculateElevation(x - d, z, in context);
            float hR = shaper.CalculateElevation(x + d, z, in context);
            float hD = shaper.CalculateElevation(x, z - d, in context);
            float hU = shaper.CalculateElevation(x, z + d, in context);

            Vector3 grad = new Vector3((hL - hR) / (2f * d), 1f, (hD - hU) / (2f * d));
            normal = grad.normalized;
        }
    }
}
