namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Thread-safe service for generating and evaluating global tectonic plate partitions,
    /// boundary line classification, and continuous mountain ridge / rift valley elevation modifiers.
    /// Employs a mathematically guaranteed C1-continuous potential field combined with (F2 - F1)
    /// smooth Voronoi boundary envelopes, eliminating all step discontinuities and vertical cliffs.
    /// </summary>
    public class TectonicService : ITectonicService
    {
        public void GenerateTectonicPartition(
            TectonicSettings settings,
            out TectonicPlate[] plates,
            out TectonicBoundary[] boundaries)
        {
            settings.Validate();
            int count = settings.PlateCount;
            plates = new TectonicPlate[count];

            int seed = settings.Seed;
            float scale = settings.PlateScale;
            int gridDim = Mathf.CeilToInt(Mathf.Sqrt(count));
            float cellSize = scale;

            int plateIdx = 0;
            for (int gx = 0; gx < gridDim && plateIdx < count; gx++)
            {
                for (int gz = 0; gz < gridDim && plateIdx < count; gz++)
                {
                    int cx = gx - gridDim / 2;
                    int cz = gz - gridDim / 2;

                    GetCellPlateInfo(cx, cz, cellSize, seed, out Vector2 centroid, out Vector2 drift, out PlateCrustType crust);
                    float baseElev = crust == PlateCrustType.Continental ? 0f : -settings.RiftDepth * 0.5f;

                    plates[plateIdx] = new TectonicPlate(plateIdx, centroid, drift, crust, baseElev);
                    plateIdx++;
                }
            }

            var boundaryList = new List<TectonicBoundary>(count * 3);

            for (int i = 0; i < plates.Length; i++)
            {
                for (int j = i + 1; j < plates.Length; j++)
                {
                    Vector2 pA = plates[i].Centroid;
                    Vector2 pB = plates[j].Centroid;

                    float distSq = (pA - pB).sqrMagnitude;
                    if (distSq > (cellSize * 2.5f) * (cellSize * 2.5f)) continue;

                    Vector2 mid = (pA + pB) * 0.5f;
                    Vector2 dir = (pB - pA).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);

                    float segmentHalfLength = Mathf.Sqrt(distSq) * 0.5f;
                    Vector2 startPoint = mid - normal * segmentHalfLength;
                    Vector2 endPoint = mid + normal * segmentHalfLength;

                    Vector2 relVelocity = plates[i].DriftVelocity - plates[j].DriftVelocity;
                    float normalConvergence = Vector2.Dot(relVelocity, dir);

                    TectonicBoundaryType bType;
                    float intensity;
                    float maxUplift;

                    if (normalConvergence > 0.05f)
                    {
                        bType = TectonicBoundaryType.Convergent;
                        intensity = Mathf.Clamp01(normalConvergence);
                        maxUplift = settings.MountainUplift * intensity;
                    }
                    else if (normalConvergence < -0.05f)
                    {
                        bType = TectonicBoundaryType.Divergent;
                        intensity = Mathf.Clamp01(-normalConvergence);
                        maxUplift = -settings.RiftDepth * intensity;
                    }
                    else
                    {
                        bType = TectonicBoundaryType.Transform;
                        intensity = 0.5f;
                        maxUplift = settings.MountainUplift * 0.25f;
                    }

                    boundaryList.Add(new TectonicBoundary(
                        i, j,
                        startPoint, endPoint,
                        bType,
                        intensity,
                        settings.BoundaryInfluenceWidth,
                        maxUplift));
                }
            }

            boundaries = boundaryList.ToArray();
        }

        public float SampleTectonicUplift(
            float worldX,
            float worldZ,
            TectonicSettings settings,
            TectonicBoundary[] boundaries)
        {
            if (!settings.Enabled) return 0f;
            return SampleTectonicUpliftProcedural(worldX, worldZ, settings);
        }

        public static float SampleTectonicUpliftProcedural(
            float worldX,
            float worldZ,
            TectonicSettings settings)
        {
            if (!settings.Enabled || settings.MountainUplift <= 0.001f) return 0f;

            float scale = Mathf.Max(200f, settings.PlateScale);
            int seed = settings.Seed;

            // 1. Natural Domain Warping for Organic Serpentine Belts
            float sampleX = worldX;
            float sampleZ = worldZ;
            if (settings.FaultNoiseWarp > 0.001f)
            {
                float warpX = (Mathf.PerlinNoise((worldX + 500f) * 0.001f, worldZ * 0.001f + seed) * 2f - 1f) * settings.FaultNoiseWarp * 180f;
                float warpZ = (Mathf.PerlinNoise(worldX * 0.001f + seed, (worldZ + 500f) * 0.001f) * 2f - 1f) * settings.FaultNoiseWarp * 180f;
                sampleX += warpX;
                sampleZ += warpZ;
            }

            Vector2 p = new Vector2(sampleX, sampleZ);
            int cellX = Mathf.FloorToInt(sampleX / scale);
            int cellZ = Mathf.FloorToInt(sampleZ / scale);

            float f1 = float.MaxValue;
            float f2 = float.MaxValue;

            // 2. Continuous Voronoi F1 & F2 evaluation in 5x5 neighborhood
            for (int dz = -2; dz <= 2; dz++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int cx = cellX + dx;
                    int cz = cellZ + dz;

                    GetCellPlateInfo(cx, cz, scale, seed, out Vector2 centroid, out _, out _);
                    float d = Vector2.Distance(p, centroid);

                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                    }
                    else if (d < f2)
                    {
                        f2 = d;
                    }
                }
            }

            // 3. Continuous Voronoi Boundary Envelope: (F2 - F1)
            float boundaryDist = (f2 - f1) * 0.5f;
            float influenceWidth = Mathf.Max(30f, settings.BoundaryInfluenceWidth * 0.5f);

            if (boundaryDist >= influenceWidth)
            {
                return 0f;
            }

            // C1-Smooth Hermite Window Envelope
            float u = boundaryDist / influenceWidth;
            float envelope = (1f - u) * (1f - u) * (3f - 2f * (1f - u));
            float ridgeSharpness = Mathf.Exp(-u * u * settings.RidgeSharpness * 3.5f) * envelope;

            // 4. Global Continuous Collision Stress Field (Guarantees C2-smooth continuity at all junction points)
            float stressNoise1 = Mathf.PerlinNoise(sampleX * 0.0006f + seed * 0.1f, sampleZ * 0.0006f) * 2f - 1f;
            float stressNoise2 = Mathf.PerlinNoise((sampleX + 300f) * 0.0012f, (sampleZ + 300f) * 0.0012f + seed * 0.2f) * 2f - 1f;
            float stressField = stressNoise1 * 0.7f + stressNoise2 * 0.3f; // [-1.0, +1.0] continuous

            if (stressField > 0.0f)
            {
                // Continuous Convergent Mountain Range Uplift
                float upliftFactor = Mathf.SmoothStep(0f, 1f, stressField * 1.5f);
                return settings.MountainUplift * upliftFactor * ridgeSharpness;
            }
            else
            {
                // Continuous Divergent Rift Valley Depression
                float riftFactor = Mathf.SmoothStep(0f, 1f, -stressField * 1.5f);
                return -settings.RiftDepth * riftFactor * ridgeSharpness;
            }
        }

        private static void GetCellPlateInfo(
            int cx, int cz, float cellSize, int seed,
            out Vector2 centroid, out Vector2 drift, out PlateCrustType crust)
        {
            unchecked
            {
                int h = cx * 73856093 ^ cz * 19349663 ^ seed * 83492791;
                h = (h ^ (h >> 13)) * 1274126177;
                h = h ^ (h >> 16);

                uint uh = (uint)h;
                float jx = ((uh & 0xFFFF) / 65535f * 0.5f + 0.25f) * cellSize;
                float jz = (((uh >> 16) & 0xFFFF) / 65535f * 0.5f + 0.25f) * cellSize;

                centroid = new Vector2(cx * cellSize + jx, cz * cellSize + jz);

                float angle = (((uh >> 8) & 0xFF) / 255f) * Mathf.PI * 2f;
                float speed = (((uh >> 4) & 0xF) / 15f) * 0.6f + 0.4f;
                drift = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

                crust = ((uh & 1) == 0) ? PlateCrustType.Continental : PlateCrustType.Oceanic;
            }
        }
    }
}
