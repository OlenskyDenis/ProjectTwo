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
            NoiseSettings noise,
            TectonicSettings tectonics,
            MacroMaskSettings macroSettings = default,
            FalloffSettings falloffSettings = default)
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
                segLookup[segIdx] = seg;

                if (!outgoingMap.TryGetValue(seg.StartNodeId, out var outList))
                {
                    outList = new List<int>();
                    outgoingMap[seg.StartNodeId] = outList;
                }
                outList.Add(segIdx);

                if (!incomingMap.TryGetValue(seg.EndNodeId, out var inList))
                {
                    inList = new List<int>();
                    incomingMap[seg.EndNodeId] = inList;
                }
                inList.Add(segIdx);
            }

            // 2. Precompute smooth continuous node frames (miter averaged)
            var nodeFrames = new Dictionary<int, (Vector3 tangent, Vector3 lateral, Vector3 normal, float width)>();
            foreach (var kvp in segLookup)
            {
                var seg = kvp.Value;
                int startId = seg.StartNodeId;
                int endId = seg.EndNodeId;

                if (!nodeFrames.ContainsKey(startId))
                {
                    Vector3 tOut = (seg.EndPosition - seg.StartPosition).normalized;
                    if (incomingMap.TryGetValue(startId, out var inList) && inList.Count > 0)
                    {
                        var prevSeg = segLookup.ContainsKey(inList[0]) ? segLookup[inList[0]] : riverGraph.Segments[inList[0]];
                        Vector3 tIn = (prevSeg.EndPosition - prevSeg.StartPosition).normalized;
                        tOut = (tIn + tOut).normalized;
                    }
                    if (tOut.sqrMagnitude < 0.001f) tOut = Vector3.forward;

                    ComputeFrameAtWorldPoint(
                        seg.StartPosition, tOut, seg.StartWidth,
                        terrainProvider, noise, tectonics, water, macroSettings, falloffSettings, settings,
                        out Vector3 lateral, out Vector3 normal);

                    nodeFrames[startId] = (tOut, lateral, normal, seg.StartWidth);
                }

                if (!nodeFrames.ContainsKey(endId))
                {
                    Vector3 tIn = (seg.EndPosition - seg.StartPosition).normalized;
                    if (outgoingMap.TryGetValue(endId, out var outList) && outList.Count > 0)
                    {
                        var nextSeg = segLookup.ContainsKey(outList[0]) ? segLookup[outList[0]] : riverGraph.Segments[outList[0]];
                        Vector3 tOut = (nextSeg.EndPosition - nextSeg.StartPosition).normalized;
                        tIn = (tIn + tOut).normalized;
                    }
                    if (tIn.sqrMagnitude < 0.001f) tIn = Vector3.forward;

                    ComputeFrameAtWorldPoint(
                        seg.EndPosition, tIn, seg.EndWidth,
                        terrainProvider, noise, tectonics, water, macroSettings, falloffSettings, settings,
                        out Vector3 lateral, out Vector3 normal);

                    nodeFrames[endId] = (tIn, lateral, normal, seg.EndWidth);
                }
            }

            // 3. Extrude continuous welded ribbons for all segments
            // Store emitted node vertices to weld consecutive connected segments
            var nodeVertexMap = new Dictionary<int, (int leftIdx, int rightIdx)>();
            var nodeDistanceMap = new Dictionary<int, float>();

            for (int s = 0; s < intersectingSegmentIndices.Count; s++)
            {
                int segIdx = intersectingSegmentIndices[s];
                ref readonly RiverSegment seg = ref riverGraph.Segments[segIdx];

                int subdivisions = 8;
                if (seg.IsWaterfall || Mathf.Abs(seg.StartPosition.y - seg.EndPosition.y) > 5f)
                {
                    float stepSize = Mathf.Max(0.75f, settings.WaterfallStepSize);
                    subdivisions = Mathf.Clamp(Mathf.CeilToInt(seg.Length / stepSize), 8, 32);
                }

                int prevLeftIdx = -1;
                int prevRightIdx = -1;

                bool isSourceSegment = !incomingMap.ContainsKey(seg.StartNodeId);
                bool isTerminalSegment = !outgoingMap.ContainsKey(seg.EndNodeId);

                float segmentStartDist = 0f;
                if (nodeDistanceMap.TryGetValue(seg.StartNodeId, out float startDist))
                {
                    segmentStartDist = startDist;
                }

                float currentDist = segmentStartDist;
                Vector3 prevCenterWorld = seg.StartPosition;

                for (int step = 0; step <= subdivisions; step++)
                {
                    float t = (float)step / subdivisions;

                    if (step == 0 && prevLeftIdx >= 0)
                    {
                        // Already welded to previous segment's end vertex ring!
                        continue;
                    }

                    Vector3 center = SampleQuadraticBezier(seg.StartPosition, seg.ControlPoint, seg.EndPosition, t);
                    Vector3 tangent = SampleQuadraticBezierTangent(seg.StartPosition, seg.ControlPoint, seg.EndPosition, t).normalized;
                    if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;

                    if (step > 0)
                    {
                        currentDist += Vector3.Distance(prevCenterWorld, center);
                    }
                    prevCenterWorld = center;

                    float currentWidth = Mathf.Lerp(seg.StartWidth, seg.EndWidth, t);
                    if (currentWidth <= 0.01f) currentWidth = seg.ChannelWidth;

                    // Natural source spring tapering
                    if (isSourceSegment && t < 0.25f)
                    {
                        currentWidth *= Mathf.SmoothStep(0.08f, 1f, t * 4f);
                    }

                    // Natural terminal infiltration tapering
                    if (isTerminalSegment && t > 0.75f)
                    {
                        currentWidth *= Mathf.SmoothStep(1f, 0.08f, (t - 0.75f) * 4f);
                    }

                    Vector3 lateral, normal;
                    if (step == 0 && nodeFrames.TryGetValue(seg.StartNodeId, out var startFrame))
                    {
                        tangent = startFrame.tangent;
                        lateral = startFrame.lateral;
                        normal = startFrame.normal;
                    }
                    else if (step == subdivisions && nodeFrames.TryGetValue(seg.EndNodeId, out var endFrame))
                    {
                        tangent = endFrame.tangent;
                        lateral = endFrame.lateral;
                        normal = endFrame.normal;
                    }
                    else
                    {
                        ComputeFrameAtWorldPoint(
                            center, tangent, currentWidth,
                            terrainProvider, noise, tectonics, water, macroSettings, falloffSettings, settings,
                            out lateral, out normal);
                    }

                    float halfWidth = Mathf.Max(0.1f, currentWidth * 0.5f);

                    // Clamp center and bank vertices to ground surface
                    Vector3 localCenter = center - chunkOrigin;
                    localCenter += normal * 0.15f;

                    Vector3 leftWorld = center - lateral * halfWidth;
                    Vector3 rightWorld = center + lateral * halfWidth;

                    Vector3 leftPos = localCenter - lateral * halfWidth;
                    Vector3 rightPos = localCenter + lateral * halfWidth;

                    if (terrainProvider != null)
                    {
                        float leftHeight = terrainProvider.CalculateElevation(
                            leftWorld.x, leftWorld.z,
                            noise, macroSettings, tectonics, null,
                            HeightCurveSettings.Default, water, RiverSettings.Default, settings, RiverGraph.Empty, falloffSettings);

                        float rightHeight = terrainProvider.CalculateElevation(
                            rightWorld.x, rightWorld.z,
                            noise, macroSettings, tectonics, null,
                            HeightCurveSettings.Default, water, RiverSettings.Default, settings, RiverGraph.Empty, falloffSettings);

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
                        nodeVertexMap[seg.StartNodeId] = (currentLeftIdx, currentRightIdx);
                        nodeDistanceMap[seg.StartNodeId] = currentDist;
                    }
                    else if (step == subdivisions)
                    {
                        nodeVertexMap[seg.EndNodeId] = (currentLeftIdx, currentRightIdx);
                        nodeDistanceMap[seg.EndNodeId] = currentDist;
                    }

                    if (prevLeftIdx >= 0)
                    {
                        triangles.Add(prevLeftIdx);
                        triangles.Add(currentLeftIdx);
                        triangles.Add(prevRightIdx);

                        triangles.Add(prevRightIdx);
                        triangles.Add(currentLeftIdx);
                        triangles.Add(currentRightIdx);
                    }

                    prevLeftIdx = currentLeftIdx;
                    prevRightIdx = currentRightIdx;
                }
            }

            return new RiverWaterMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                uvs.ToArray(),
                triangles.ToArray());
        }

        private static void ComputeFrameAtWorldPoint(
            Vector3 worldPos,
            Vector3 tangent,
            float width,
            ITerrainShaper terrainProvider,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            MacroMaskSettings macroSettings,
            FalloffSettings falloffSettings,
            HydrologySettings settings,
            out Vector3 lateral,
            out Vector3 normal)
        {
            Vector3 approxUp = Vector3.up;
            if (terrainProvider != null)
            {
                ComputeTerrainNormal(
                    worldPos.x, worldPos.z,
                    terrainProvider, noise, tectonics, water, macroSettings, falloffSettings, settings,
                    out approxUp);
            }
            else if (Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.85f)
            {
                approxUp = new Vector3(tangent.x, 0f, tangent.z).normalized;
                if (approxUp.sqrMagnitude < 0.001f) approxUp = Vector3.forward;
            }

            lateral = Vector3.Cross(tangent, approxUp).normalized;
            if (lateral.sqrMagnitude < 0.001f) lateral = Vector3.right;

            normal = Vector3.Cross(lateral, tangent).normalized;
            if (Vector3.Dot(normal, approxUp) < 0f)
            {
                normal = -normal;
                lateral = -lateral;
            }
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

        private static void ComputeTerrainNormal(
            float x, float z,
            ITerrainShaper shaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            MacroMaskSettings macro,
            FalloffSettings falloff,
            HydrologySettings hydrology,
            out Vector3 normal)
        {
            const float d = 2.0f;
            float hL = shaper.CalculateElevation(x - d, z, noise, macro, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, falloff);
            float hR = shaper.CalculateElevation(x + d, z, noise, macro, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, falloff);
            float hD = shaper.CalculateElevation(x, z - d, noise, macro, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, falloff);
            float hU = shaper.CalculateElevation(x, z + d, noise, macro, tectonics, null, HeightCurveSettings.Default, water, RiverSettings.Default, hydrology, RiverGraph.Empty, falloff);

            Vector3 grad = new Vector3((hL - hR) / (2f * d), 1f, (hD - hU) / (2f * d));
            normal = grad.normalized;
        }
    }
}
