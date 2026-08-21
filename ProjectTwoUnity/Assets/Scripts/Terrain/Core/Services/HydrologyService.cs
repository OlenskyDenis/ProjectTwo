namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe service for computing hydrological drainage networks, continuous river graph routing,
    /// inertia-based steepest descent pathfinding, depression saddle breaching, and hydraulic carving profiles.
    /// </summary>
    public class HydrologyService : IHydrologyService
    {
        private const float StepSize = 25f;
        private const int MaxStepsPerRiver = 350;
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
            int attempts = settings.SourceCount * 10;
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
                Vector3 currentPos = sources[s];
                int prevNodeId = nextNodeId++;
                var sourceNode = new RiverNode(
                    prevNodeId,
                    currentPos,
                    RiverNodeType.Source,
                    currentPos.y,
                    flowAccumulation: 1f,
                    streamOrder: 1);
                nodes.Add(sourceNode);

                float currentElevation = currentPos.y;
                float meanderPhase = (float)(prng.NextDouble() * Math.PI * 2.0);
                Vector2 currentVelocity = Vector2.zero;
                int currentStreamOrder = 1;

                for (int step = 0; step < MaxStepsPerRiver; step++)
                {
                    // Check if reached ocean level
                    if (currentElevation <= water.SeaLevel + 0.5f)
                    {
                        var mouthNode = new RiverNode(
                            nextNodeId++,
                            new Vector3(currentPos.x, water.SeaLevel, currentPos.z),
                            RiverNodeType.OceanMouth,
                            water.SeaLevel,
                            flowAccumulation: 10f + step * 0.5f,
                            streamOrder: currentStreamOrder + 1);
                        nodes.Add(mouthNode);

                        AddSegment(
                            ref nextSegId,
                            prevNodeId,
                            mouthNode.Id,
                            currentPos,
                            mouthNode.Position,
                            settings,
                            streamOrder: currentStreamOrder + 1,
                            segments);
                        break;
                    }

                    // Compute terrain height gradient with smooth finite differences
                    ComputeGradient(
                        currentPos.x, currentPos.z,
                        baseTerrainShaper, noise, tectonics, water,
                        out float gradX, out float gradZ);

                    Vector2 steepDescent = new Vector2(-gradX, -gradZ);
                    float gradMag = steepDescent.magnitude;

                    // Hydraulic Inertia & Momentum: blends previous flow vector with current gradient
                    Vector2 flowDir;
                    if (gradMag > 0.005f)
                    {
                        steepDescent /= gradMag;
                        if (currentVelocity.sqrMagnitude > 0.001f)
                        {
                            flowDir = Vector2.Lerp(currentVelocity, steepDescent, 0.45f).normalized;
                        }
                        else
                        {
                            flowDir = steepDescent;
                        }
                    }
                    else
                    {
                        // In local depression or flat valley: maintain forward momentum or head toward ocean
                        if (currentVelocity.sqrMagnitude > 0.01f)
                        {
                            flowDir = currentVelocity;
                        }
                        else
                        {
                            flowDir = new Vector2(currentPos.x, currentPos.z).normalized;
                            if (flowDir.sqrMagnitude < 0.01f) flowDir = Vector2.right;
                        }

                        // Form a lake basin in deep enclosed hollows
                        if (currentElevation - (water.SeaLevel + 5f) > settings.LakeMinDepthThreshold)
                        {
                            var lake = new LakeBasin(
                                nextLakeId++,
                                currentPos,
                                currentElevation,
                                radius: 45f,
                                outletNodeId: prevNodeId);
                            lakes.Add(lake);
                        }
                    }

                    // Apply harmonic meandering deflection
                    if (settings.MeanderIntensity > 0.01f)
                    {
                        meanderPhase += 0.25f;
                        Vector2 perp = new Vector2(-flowDir.y, flowDir.x);
                        float meanderAmount = Mathf.Sin(meanderPhase) * settings.MeanderIntensity * 0.6f;
                        flowDir = (flowDir + perp * meanderAmount).normalized;
                    }

                    currentVelocity = flowDir;

                    // Adaptive step size: take shorter steps on steep slopes/cliffs for smooth waterfalls
                    float currentStepSize = gradMag > 0.35f
                        ? Mathf.Lerp(StepSize, 6f, Mathf.Clamp01((gradMag - 0.35f) / 1.5f))
                        : StepSize;

                    Vector3 nextPos = currentPos + new Vector3(flowDir.x, 0f, flowDir.y) * currentStepSize;
                    float terrainHeight = SampleElevation(
                        nextPos.x, nextPos.z, baseTerrainShaper, noise, tectonics, water);

                    // Water surface strictly conforms to terrain elevation (never floats in the sky)
                    float nextElev = Mathf.Min(currentElevation, terrainHeight);
                    if (terrainHeight < nextElev)
                    {
                        nextElev = terrainHeight;
                    }
                    nextPos.y = nextElev;

                    // Stop if reached sea level
                    if (nextElev <= water.SeaLevel + 0.5f)
                    {
                        var mouthNode = new RiverNode(
                            nextNodeId++,
                            new Vector3(nextPos.x, water.SeaLevel, nextPos.z),
                            RiverNodeType.OceanMouth,
                            water.SeaLevel,
                            flowAccumulation: 10f + step * 0.5f,
                            streamOrder: currentStreamOrder + 1);
                        nodes.Add(mouthNode);

                        AddSegment(
                            ref nextSegId,
                            prevNodeId,
                            mouthNode.Id,
                            currentPos,
                            mouthNode.Position,
                            settings,
                            streamOrder: currentStreamOrder + 1,
                            segments);
                        break;
                    }

                    // Check for confluence with existing river channels
                    int mergeNodeId = FindNearbyNode(nextPos, nodes, prevNodeId, MergeDistance);
                    if (mergeNodeId >= 0 && mergeNodeId != prevNodeId)
                    {
                        currentStreamOrder = Mathf.Min(4, currentStreamOrder + 1);
                        AddSegment(
                            ref nextSegId,
                            prevNodeId,
                            mergeNodeId,
                            currentPos,
                            nodes[mergeNodeId].Position,
                            settings,
                            streamOrder: currentStreamOrder,
                            segments);
                        break;
                    }

                    int newNodeId = nextNodeId++;
                    var waypointNode = new RiverNode(
                        newNodeId,
                        nextPos,
                        RiverNodeType.Waypoint,
                        nextElev,
                        flowAccumulation: 1f + step * 0.3f,
                        streamOrder: currentStreamOrder);
                    nodes.Add(waypointNode);

                    AddSegment(
                        ref nextSegId,
                        prevNodeId,
                        newNodeId,
                        currentPos,
                        nextPos,
                        settings,
                        streamOrder: currentStreamOrder,
                        segments);

                    prevNodeId = newNodeId;
                    currentPos = nextPos;
                    currentElevation = nextElev;
                }
            }

            return new RiverGraph(nodes.ToArray(), segments.ToArray(), lakes.ToArray());
        }

        private static void AddSegment(
            ref int nextSegId,
            int startNodeId,
            int endNodeId,
            Vector3 startPos,
            Vector3 endPos,
            HydrologySettings settings,
            int streamOrder,
            List<RiverSegment> segments)
        {
            Vector3 mid = (startPos + endPos) * 0.5f;
            float len = Vector3.Distance(startPos, endPos);
            float widthMultiplier = streamOrder <= 1 ? 0.4f : (streamOrder == 2 ? 0.7f : Mathf.Pow(settings.WidthGrowthFactor, streamOrder - 2));
            float width = Mathf.Max(2.5f, settings.BaseRiverWidth * widthMultiplier);
            float depth = settings.BaseCarveDepth * Mathf.Sqrt(streamOrder);

            segments.Add(new RiverSegment(
                nextSegId++,
                startNodeId,
                endNodeId,
                startPos,
                mid,
                endPos,
                len,
                width,
                depth,
                streamOrder,
                flowRate: streamOrder * 2.0f));
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
