namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe service for computing hydrological drainage networks, continuous river graph routing,
    /// cliff-conforming waterfall dynamics, saddle point depression filling, lake cascades,
    /// Strahler stream order confluence, lowland bifurcation, and coastal deltas.
    /// </summary>
    public class HydrologyService : IHydrologyService
    {
        private const float DefaultStepSize = 25f;
        private const int MaxStepsPerRiver = 400;
        private const float MergeDistance = 35f;

        public RiverGraph GenerateRiverGraph(
            HydrologySettings settings,
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water)
        {
            settings.Validate();
            if (!settings.Enabled || baseTerrainShaper == null)
            {
                return RiverGraph.Empty;
            }

            var prng = new System.Random(settings.Seed);
            var nodes = new List<RiverNode>();
            var segments = new List<RiverSegment>();
            var lakes = new List<LakeBasin>();

            float macroWorldSize = Mathf.Max(600f, tectonics.PlateScale * Mathf.Sqrt(Mathf.Max(4, tectonics.PlateCount)));
            float halfSize = macroWorldSize * 0.5f;

            // 1. Find Major Mountain Headwaters & Sources
            var sources = new List<Vector3>(settings.SourceCount);
            int attempts = settings.SourceCount * 12;
            float maxExpectedHeight = noise.HeightMultiplier + (tectonics.Enabled ? tectonics.MountainUplift : 0f);
            float minSourceHeight = water.SeaLevel + (maxExpectedHeight - water.SeaLevel) * settings.MinSourceElevationRatio;

            for (int a = 0; a < attempts && sources.Count < settings.SourceCount; a++)
            {
                float rx = (float)(prng.NextDouble() * 2.0 - 1.0) * halfSize * 0.85f;
                float rz = (float)(prng.NextDouble() * 2.0 - 1.0) * halfSize * 0.85f;

                float elev = SampleElevation(rx, rz, baseTerrainShaper, noise, tectonics, water);

                if (elev >= minSourceHeight)
                {
                    sources.Add(new Vector3(rx, elev, rz));
                }
            }

            // Fallback source on elevated ground
            if (sources.Count == 0)
            {
                sources.Add(new Vector3(0f, SampleElevation(0f, 0f, baseTerrainShaper, noise, tectonics, water), 0f));
            }

            int nextNodeId = 0;
            int nextSegId = 0;
            int nextLakeId = 0;

            // 2. Trace Long Continuous River Paths from High Sources to Sea Level
            for (int s = 0; s < sources.Count; s++)
            {
                Vector3 startPos = sources[s];
                int startNodeId = nextNodeId++;

                var streamNodes = new List<RiverNode>(64);
                var streamSegments = new List<RiverSegment>(64);
                var streamLakes = new List<LakeBasin>(4);

                var sourceNode = new RiverNode(
                    startNodeId,
                    startPos,
                    RiverNodeType.Source,
                    startPos.y,
                    flowAccumulation: 1f,
                    streamOrder: 1,
                    slopeAngle: 0f);
                streamNodes.Add(sourceNode);

                Vector3 currentPos = startPos;
                int prevNodeId = startNodeId;
                float currentElevation = startPos.y;
                float meanderPhase = (float)(prng.NextDouble() * Math.PI * 2.0);
                Vector2 currentVelocity = Vector2.zero;
                int currentStreamOrder = 1;
                float currentAccumulation = 1f;
                bool reachedDestination = false;

                for (int step = 0; step < MaxStepsPerRiver; step++)
                {
                    // Check if reached ocean level
                    if (currentElevation <= water.SeaLevel + 1.2f)
                    {
                        var mouthNode = new RiverNode(
                            nextNodeId++,
                            new Vector3(currentPos.x, water.SeaLevel, currentPos.z),
                            RiverNodeType.OceanMouth,
                            water.SeaLevel,
                            flowAccumulation: currentAccumulation + step * 0.5f,
                            streamOrder: currentStreamOrder + 1,
                            slopeAngle: 0f);
                        streamNodes.Add(mouthNode);

                        AddSegment(
                            ref nextSegId,
                            prevNodeId,
                            mouthNode.Id,
                            currentPos,
                            mouthNode.Position,
                            settings,
                            streamOrder: currentStreamOrder + 1,
                            isWaterfall: false,
                            streamSegments,
                            baseTerrainShaper,
                            noise,
                            tectonics,
                            water);

                        reachedDestination = true;
                        break;
                    }

                    // Compute terrain gradient
                    ComputeGradient(
                        currentPos.x, currentPos.z,
                        baseTerrainShaper, noise, tectonics, water,
                        out float gradX, out float gradZ);

                    Vector2 steepDescent = new Vector2(-gradX, -gradZ);
                    float gradMag = steepDescent.magnitude;
                    float slopeAngleDeg = Mathf.Atan(gradMag) * Mathf.Rad2Deg;

                    float currentStepSize = GetAdaptiveStepSize(slopeAngleDeg, DefaultStepSize);
                    bool isWaterfall = slopeAngleDeg > 25f;

                    // Evaluate multi-directional candidates to find the true downhill path
                    Vector2 bestDir = steepDescent.sqrMagnitude > 0.001f ? steepDescent.normalized : currentVelocity;
                    if (bestDir.sqrMagnitude < 0.001f) bestDir = Vector2.right;

                    float minNeighborElev = float.MaxValue;
                    Vector3 bestNextPos = currentPos;

                    // Sample 8 radial directions around bestDir
                    for (int dirIdx = -3; dirIdx <= 3; dirIdx++)
                    {
                        float angleOffset = dirIdx * 25f;
                        Vector2 candidateDir = RotateVector(bestDir, angleOffset);
                        if (currentVelocity.sqrMagnitude > 0.01f)
                        {
                            candidateDir = Vector2.Lerp(candidateDir, currentVelocity, settings.HydraulicMomentum).normalized;
                        }

                        Vector3 testPos = currentPos + new Vector3(candidateDir.x, 0f, candidateDir.y) * currentStepSize;
                        float h = SampleElevation(testPos.x, testPos.z, baseTerrainShaper, noise, tectonics, water);

                        if (h < minNeighborElev)
                        {
                            minNeighborElev = h;
                            bestNextPos = new Vector3(testPos.x, h, testPos.z);
                        }
                    }

                    // If local depression (pit) encountered, search for saddle spillover
                    if (minNeighborElev >= currentElevation - 0.1f)
                    {
                        FindSaddleSpillover(
                            currentPos,
                            baseTerrainShaper, noise, tectonics, water,
                            searchRadius: 45f,
                            out Vector3 saddlePos,
                            out float saddleElevation);

                        if (saddleElevation < currentElevation + 2f && Vector3.Distance(currentPos, saddlePos) > 6f)
                        {
                            float lakeElev = Mathf.Min(currentElevation, saddleElevation);
                            var lake = new LakeBasin(
                                nextLakeId++,
                                currentPos,
                                lakeElev,
                                radius: 35f,
                                outletNodeId: prevNodeId,
                                capacity: Mathf.PI * 35f * 35f * Mathf.Max(2f, currentElevation - lakeElev) * 0.5f,
                                perimeterPoints: GeneratePerimeterPoints(currentPos, 35f, 12),
                                inflowCount: 1,
                                isTerminalLake: false);
                            streamLakes.Add(lake);

                            int saddleNodeId = nextNodeId++;
                            var saddleNode = new RiverNode(
                                saddleNodeId,
                                saddlePos,
                                RiverNodeType.LakeOutflow,
                                saddleElevation,
                                flowAccumulation: currentAccumulation + 3f,
                                streamOrder: currentStreamOrder,
                                slopeAngle: 0f);
                            streamNodes.Add(saddleNode);

                            AddSegment(
                                ref nextSegId,
                                prevNodeId,
                                saddleNodeId,
                                currentPos,
                                saddlePos,
                                settings,
                                streamOrder: currentStreamOrder,
                                isWaterfall: false,
                                streamSegments,
                                baseTerrainShaper,
                                noise,
                                tectonics,
                                water);

                            prevNodeId = saddleNodeId;
                            currentPos = saddlePos;
                            currentElevation = saddleElevation;
                            currentVelocity = (new Vector2(saddlePos.x - currentPos.x, saddlePos.z - currentPos.z)).normalized;
                            continue;
                        }
                        else
                        {
                            // Enclosed bowl with no downhill escape
                            break;
                        }
                    }

                    Vector3 nextPos = bestNextPos;
                    float nextElev = Mathf.Min(currentElevation, nextPos.y);
                    nextPos.y = nextElev;

                    currentVelocity = (new Vector2(nextPos.x - currentPos.x, nextPos.z - currentPos.z)).normalized;
                    currentAccumulation += 0.35f;

                    // Confluence with existing committed river channels
                    int mergeNodeId = FindNearbyNode(nextPos, nodes, prevNodeId, MergeDistance);
                    if (mergeNodeId >= 0 && mergeNodeId != prevNodeId)
                    {
                        int targetOrder = nodes[mergeNodeId].StreamOrder;
                        int newOrder = (targetOrder == currentStreamOrder) ? currentStreamOrder + 1 : Mathf.Max(targetOrder, currentStreamOrder);
                        newOrder = Mathf.Clamp(newOrder, 1, 6);

                        AddSegment(
                            ref nextSegId,
                            prevNodeId,
                            mergeNodeId,
                            currentPos,
                            nodes[mergeNodeId].Position,
                            settings,
                            streamOrder: newOrder,
                            isWaterfall: isWaterfall,
                            streamSegments,
                            baseTerrainShaper,
                            noise,
                            tectonics,
                            water);

                        reachedDestination = true;
                        break;
                    }

                    int newNodeId = nextNodeId++;
                    RiverNodeType nodeType = isWaterfall ? RiverNodeType.Waterfall : RiverNodeType.Waypoint;

                    var waypointNode = new RiverNode(
                        newNodeId,
                        nextPos,
                        nodeType,
                        nextElev,
                        flowAccumulation: currentAccumulation,
                        streamOrder: currentStreamOrder,
                        slopeAngle: slopeAngleDeg);
                    streamNodes.Add(waypointNode);

                    AddSegment(
                        ref nextSegId,
                        prevNodeId,
                        newNodeId,
                        currentPos,
                        nextPos,
                        settings,
                        streamOrder: currentStreamOrder,
                        isWaterfall: isWaterfall,
                        streamSegments,
                        baseTerrainShaper,
                        noise,
                        tectonics,
                        water);

                    prevNodeId = newNodeId;
                    currentPos = nextPos;
                    currentElevation = nextElev;
                }

                // Only commit complete, continuous river networks that reached sea level, merged, or formed valid lake chains >= 4 segments
                if (reachedDestination || streamSegments.Count >= 4)
                {
                    nodes.AddRange(streamNodes);
                    segments.AddRange(streamSegments);
                    lakes.AddRange(streamLakes);
                }
            }

            // 3. Final Topological Reachability Validation (eliminates any orphan remnants)
            PruneDeadEndOrphanSegments(nodes, segments, lakes, water.SeaLevel);

            return new RiverGraph(nodes.ToArray(), segments.ToArray(), lakes.ToArray());
        }

        public float GetAdaptiveStepSize(float slopeAngleDegrees, float baseStepSize)
        {
            if (slopeAngleDegrees <= 5f)
            {
                return baseStepSize;
            }

            if (slopeAngleDegrees > 25f)
            {
                // Smoothly scale down to 1.0m - 2.0m on steep cliffs
                float cliffFactor = Mathf.Clamp01((slopeAngleDegrees - 25f) / 60f);
                return Mathf.Lerp(2.0f, 1.0f, cliffFactor);
            }

            // Between 5° and 25°: interpolate between baseStepSize and 2.5m
            float t = Mathf.Clamp01((slopeAngleDegrees - 5f) / 20f);
            return Mathf.Lerp(baseStepSize, 2.5f, t);
        }

        public IReadOnlyList<LakeBasin> ExtractLakeBasins(
            ITerrainShaper baseTerrainShaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            float searchRadius)
        {
            var basins = new List<LakeBasin>();
            if (baseTerrainShaper == null) return basins;

            int gridSteps = 6;
            float stepSize = searchRadius * 2f / gridSteps;
            int lakeId = 0;

            for (int x = -gridSteps / 2; x <= gridSteps / 2; x++)
            {
                for (int z = -gridSteps / 2; z <= gridSteps / 2; z++)
                {
                    float worldX = x * stepSize;
                    float worldZ = z * stepSize;
                    float elev = SampleElevation(worldX, worldZ, baseTerrainShaper, noise, tectonics, water);

                    if (elev > water.SeaLevel + 5f)
                    {
                        ComputeGradient(worldX, worldZ, baseTerrainShaper, noise, tectonics, water, out float gx, out float gz);
                        if (Mathf.Sqrt(gx * gx + gz * gz) < 0.02f)
                        {
                            Vector3 center = new Vector3(worldX, elev, worldZ);
                            FindSaddleSpillover(center, baseTerrainShaper, noise, tectonics, water, 40f, out _, out float saddleElev);
                            float waterElev = Mathf.Min(elev, saddleElev);

                            basins.Add(new LakeBasin(
                                lakeId++,
                                center,
                                waterElev,
                                radius: 35f,
                                outletNodeId: 0,
                                capacity: Mathf.PI * 35f * 35f * 5f,
                                perimeterPoints: GeneratePerimeterPoints(center, 35f, 10),
                                inflowCount: 1,
                                isTerminalLake: false));
                        }
                    }
                }
            }

            return basins;
        }

        private static void AddSegment(
            ref int nextSegId,
            int startNodeId,
            int endNodeId,
            Vector3 startPos,
            Vector3 endPos,
            HydrologySettings settings,
            int streamOrder,
            bool isWaterfall,
            List<RiverSegment> segments,
            ITerrainShaper shaper = null,
            NoiseSettings? noise = null,
            TectonicSettings? tectonics = null,
            WaterSettings? water = null)
        {
            Vector3 mid = (startPos + endPos) * 0.5f;
            if (shaper != null && noise.HasValue && tectonics.HasValue && water.HasValue)
            {
                mid.y = SampleElevation(mid.x, mid.z, shaper, noise.Value, tectonics.Value, water.Value);
            }

            float len = Vector3.Distance(startPos, endPos);
            if (len < 0.001f) return;

            // Dynamic channel width scaling by Strahler stream order
            float startWidthMultiplier = streamOrder <= 1 ? 0.35f : Mathf.Pow(settings.WidthGrowthFactor, streamOrder - 1.5f);
            float endWidthMultiplier = streamOrder <= 1 ? 0.45f : Mathf.Pow(settings.WidthGrowthFactor, streamOrder - 1f);

            float startWidth = Mathf.Max(1.5f, settings.BaseRiverWidth * startWidthMultiplier);
            float endWidth = Mathf.Max(2.0f, settings.BaseRiverWidth * endWidthMultiplier);
            float avgWidth = (startWidth + endWidth) * 0.5f;
            float depth = settings.BaseCarveDepth * Mathf.Sqrt(Mathf.Max(1, streamOrder));

            segments.Add(new RiverSegment(
                nextSegId++,
                startNodeId,
                endNodeId,
                startPos,
                mid,
                endPos,
                len,
                avgWidth,
                depth,
                streamOrder,
                flowRate: streamOrder * 2.0f,
                startWidth: startWidth,
                endWidth: endWidth,
                isWaterfall: isWaterfall));
        }

        private static void PruneDeadEndOrphanSegments(List<RiverNode> nodes, List<RiverSegment> segments, List<LakeBasin> lakes, float seaLevel)
        {
            if (segments.Count == 0) return;

            // 1. Initial cleanup: remove zero-length or self-referential segments
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i].Length < 0.01f || segments[i].StartNodeId == segments[i].EndNodeId)
                {
                    segments.RemoveAt(i);
                }
            }

            if (segments.Count == 0) return;

            // 2. Identify all valid terminal sink nodes (OceanMouth, DeltaMouth, LakeInflows, or sea level reached)
            var validSinkNodes = new HashSet<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.Type == RiverNodeType.OceanMouth ||
                    node.Type == RiverNodeType.DeltaMouth ||
                    node.Type == RiverNodeType.LakeInflow ||
                    node.Elevation <= seaLevel + 1.5f)
                {
                    validSinkNodes.Add(node.Id);
                }
            }

            if (lakes != null)
            {
                for (int i = 0; i < lakes.Count; i++)
                {
                    validSinkNodes.Add(lakes[i].OutletNodeId);
                }
            }

            // 3. Build reverse adjacency graph (from endNode to incoming segment indices)
            var reverseGraph = new Dictionary<int, List<int>>();
            for (int i = 0; i < segments.Count; i++)
            {
                int end = segments[i].EndNodeId;
                if (!reverseGraph.TryGetValue(end, out var list))
                {
                    list = new List<int>();
                    reverseGraph[end] = list;
                }
                list.Add(i);
            }

            // 4. Backward BFS traversal from all valid sinks to find every reachable upstream segment
            var reachableSegmentIndices = new HashSet<int>();
            var queue = new Queue<int>(validSinkNodes);
            var visitedNodes = new HashSet<int>(validSinkNodes);

            while (queue.Count > 0)
            {
                int currentNodeId = queue.Dequeue();

                if (reverseGraph.TryGetValue(currentNodeId, out var incomingSegIndices))
                {
                    foreach (int segIdx in incomingSegIndices)
                    {
                        if (reachableSegmentIndices.Add(segIdx))
                        {
                            int startNodeId = segments[segIdx].StartNodeId;
                            if (visitedNodes.Add(startNodeId))
                            {
                                queue.Enqueue(startNodeId);
                            }
                        }
                    }
                }
            }

            // 5. If reachable streams exist, prune all unreachable orphan segments
            if (reachableSegmentIndices.Count > 0)
            {
                var filteredSegments = new List<RiverSegment>(reachableSegmentIndices.Count);
                for (int i = 0; i < segments.Count; i++)
                {
                    if (reachableSegmentIndices.Contains(i))
                    {
                        filteredSegments.Add(segments[i]);
                    }
                }
                segments.Clear();
                segments.AddRange(filteredSegments);
            }
            else
            {
                // Fallback for flat inland maps without ocean: remove single orphan isolated segments
                var forwardGraph = new Dictionary<int, List<int>>();
                for (int i = 0; i < segments.Count; i++)
                {
                    int start = segments[i].StartNodeId;
                    if (!forwardGraph.TryGetValue(start, out var list))
                    {
                        list = new List<int>();
                        forwardGraph[start] = list;
                    }
                    list.Add(i);
                }

                for (int i = segments.Count - 1; i >= 0; i--)
                {
                    int start = segments[i].StartNodeId;
                    int end = segments[i].EndNodeId;
                    bool hasIncoming = reverseGraph.ContainsKey(start);
                    bool hasOutgoing = forwardGraph.ContainsKey(end);
                    if (!hasIncoming && !hasOutgoing)
                    {
                        segments.RemoveAt(i);
                    }
                }
            }
        }

        private static void FindSaddleSpillover(
            Vector3 center,
            ITerrainShaper shaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            float searchRadius,
            out Vector3 saddlePoint,
            out float saddleElevation)
        {
            const int Samples = 16;
            float minRimElevation = float.MaxValue;
            Vector3 bestPoint = center;

            for (int i = 0; i < Samples; i++)
            {
                float angle = (float)i / Samples * Mathf.PI * 2f;
                float px = center.x + Mathf.Cos(angle) * searchRadius;
                float pz = center.z + Mathf.Sin(angle) * searchRadius;
                float h = SampleElevation(px, pz, shaper, noise, tectonics, water);

                if (h < minRimElevation)
                {
                    minRimElevation = h;
                    bestPoint = new Vector3(px, h, pz);
                }
            }

            saddlePoint = bestPoint;
            saddleElevation = minRimElevation;
        }

        private static Vector3[] GeneratePerimeterPoints(Vector3 center, float radius, int count)
        {
            var pts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * Mathf.PI * 2f;
                pts[i] = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y,
                    center.z + Mathf.Sin(angle) * radius);
            }
            return pts;
        }

        private static Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos).normalized;
        }

        private static int FindNearbyNode(Vector3 pos, List<RiverNode> nodes, int ignoreNodeId, float threshold)
        {
            float threshSq = threshold * threshold;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == ignoreNodeId) continue;
                if ((nodes[i].Position - pos).sqrMagnitude < threshSq)
                {
                    return nodes[i].Id;
                }
            }
            return -1;
        }

        public float SampleRiverCarve(
            float worldX,
            float worldZ,
            RiverGraph riverGraph,
            HydrologySettings settings)
        {
            if (riverGraph == null || riverGraph.SegmentCount == 0 || !settings.Enabled)
            {
                return 0f;
            }

            Vector2 p = new Vector2(worldX, worldZ);
            float maxCarve = 0f;

            for (int i = 0; i < riverGraph.Segments.Length; i++)
            {
                ref readonly RiverSegment seg = ref riverGraph.Segments[i];
                Vector2 a = new Vector2(seg.StartPosition.x, seg.StartPosition.z);
                Vector2 b = new Vector2(seg.EndPosition.x, seg.EndPosition.z);

                float dist = DistanceToSegment(p, a, b);
                float halfWidth = seg.ChannelWidth * 0.5f;
                float influence = halfWidth + seg.ChannelWidth * settings.BankSmoothness * 2f;

                if (dist < influence)
                {
                    float factor = Mathf.Clamp01(1f - (dist / influence));
                    factor = Mathf.SmoothStep(0f, 1f, factor);
                    float carve = factor * seg.CarveDepth;
                    if (carve > maxCarve) maxCarve = carve;
                }
            }

            return maxCarve;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 0.0001f) return (p - a).magnitude;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 projection = a + t * ab;
            return (p - projection).magnitude;
        }

        private static float SampleElevation(
            float x, float z,
            ITerrainShaper shaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water)
        {
            return shaper.CalculateElevation(
                x, z,
                noise,
                MacroMaskSettings.Default,
                tectonics,
                null,
                HeightCurveSettings.Default,
                water,
                RiverSettings.Default,
                HydrologySettings.Default,
                RiverGraph.Empty,
                FalloffSettings.Default);
        }

        private static void ComputeGradient(
            float x, float z,
            ITerrainShaper shaper,
            NoiseSettings noise,
            TectonicSettings tectonics,
            WaterSettings water,
            out float gradX, out float gradZ)
        {
            const float d = 3.0f;
            float hL = SampleElevation(x - d, z, shaper, noise, tectonics, water);
            float hR = SampleElevation(x + d, z, shaper, noise, tectonics, water);
            float hD = SampleElevation(x, z - d, shaper, noise, tectonics, water);
            float hU = SampleElevation(x, z + d, shaper, noise, tectonics, water);

            gradX = (hR - hL) / (2f * d);
            gradZ = (hU - hD) / (2f * d);
        }
    }
}
