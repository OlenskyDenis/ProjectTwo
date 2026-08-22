namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using UnityEngine;

    /// <summary>
    /// Core procedural mathematical shaper calculating compound world elevation.
    /// Thread-safe pure C# service combining multi-type noise, macro mountain masks,
    /// non-linear curves, river channels, sea level baselines, and boundary falloff.
    /// Strictly adheres to clean parameter encapsulation via TerrainShaperContext.
    /// </summary>
    public class ProceduralTerrainShaper : ITerrainShaper
    {
        private static readonly int[] GradientsX = { 1, -1, 1, -1, 1, -1, 0, 0 };
        private static readonly int[] GradientsY = { 0, 0, 1, 1, -1, -1, 1, -1 };

        public float CalculateElevation(
            float worldX,
            float worldZ,
            in TerrainShaperContext context)
        {
            NoiseSettings noise = context.Noise;
            MacroMaskSettings macro = context.Macro;
            TectonicSettings tectonics = context.Tectonics;
            TectonicBoundary[] tectonicBoundaries = context.TectonicBoundaries;
            HeightCurveSettings heightCurve = context.HeightCurve;
            WaterSettings water = context.Water;
            RiverSettings river = context.River;
            HydrologySettings hydrology = context.Hydrology;
            RiverGraph riverGraph = context.RiverGraph;
            FalloffSettings falloff = context.Falloff;

            noise.Validate();
            macro.Validate();
            water.Validate();
            river.Validate();
            falloff.Validate();
            if (heightCurve != null) heightCurve.Validate();

            // 1. Calculate Base Noise Height in [0, 1] range
            float baseNoise = SampleBaseNoise(worldX, worldZ, noise);

            // 2. Apply Macro Continent / Mountain Masking
            float elevation = baseNoise;
            if (macro.Enabled)
            {
                float macroMask = SamplePerlin01(worldX, worldZ, macro.Scale, macro.Seed);
                if (Math.Abs(macro.PowerExponent - 1f) > 0.001f)
                {
                    macroMask = (float)Math.Pow(macroMask, macro.PowerExponent);
                }

                float regionalMultiplier = macro.ValleyDamping + macroMask * (macro.MountainAmplification - macro.ValleyDamping);
                elevation *= regionalMultiplier;
            }

            // 3. Apply Non-Linear Elevation Curves & Terrace Steps
            if (heightCurve != null && heightCurve.UseCurve)
            {
                float clampedInput = Math.Max(0f, Math.Min(1f, elevation));
                if (Math.Abs(heightCurve.PowerExponent - 1f) > 0.001f)
                {
                    clampedInput = (float)Math.Pow(clampedInput, heightCurve.PowerExponent);
                }

                if (heightCurve.ElevationCurve != null)
                {
                    elevation = heightCurve.ElevationCurve.Evaluate(clampedInput);
                }

                if (heightCurve.TerraceSteps > 0)
                {
                    elevation = (float)Math.Round(elevation * heightCurve.TerraceSteps) / heightCurve.TerraceSteps;
                }
            }

            // Convert to World Height Units
            float worldHeight = elevation * noise.HeightMultiplier;

            // 4. Apply Tectonic Ridge Uplift & Rift Depressions
            if (tectonics.Enabled)
            {
                worldHeight += SampleTectonicUpliftInline(worldX, worldZ, tectonics, tectonicBoundaries);
            }

            // 5. Apply Procedural River Carving (Simple or Vector Graph)
            if (hydrology.Enabled && riverGraph != null && riverGraph.SegmentCount > 0)
            {
                worldHeight -= SampleVectorRiverCarve(worldX, worldZ, riverGraph, hydrology);
            }
            else if (river.Enabled && river.CarveDepth > 0.001f)
            {
                float riverSample = SampleRiverMask(worldX, worldZ, river);
                worldHeight -= riverSample * river.CarveDepth;
            }

            // 6. Apply Boundary / Island Falloff
            if (falloff.Mode != FalloffMode.None)
            {
                float falloffFactor = CalculateFalloffFactor(worldX, worldZ, falloff);
                worldHeight *= falloffFactor;
            }

            // 7. Apply Global Sea Level & Ocean Floor Basin
            if (water.Enabled)
            {
                if (worldHeight < water.SeaLevel)
                {
                    float depthBelowSea = water.SeaLevel - worldHeight;
                    float basinDepth = Math.Min(water.OceanFloorDepth, depthBelowSea * water.ShorelineSmoothness);
                    worldHeight = water.SeaLevel - basinDepth;
                }
            }

            return worldHeight;
        }

        private static float SampleTectonicUpliftInline(
            float worldX,
            float worldZ,
            TectonicSettings settings,
            TectonicBoundary[] boundaries)
        {
            return TectonicService.SampleTectonicUpliftProcedural(worldX, worldZ, settings);
        }

        private static float SampleVectorRiverCarve(
            float worldX,
            float worldZ,
            RiverGraph graph,
            HydrologySettings settings)
        {
            if (graph == null || graph.SegmentCount == 0) return 0f;

            Vector2 p = new Vector2(worldX, worldZ);
            float maxCarve = 0f;

            // Sample within local neighborhood
            for (int i = 0; i < graph.Segments.Length; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                Vector2 a = new Vector2(seg.StartPosition.x, seg.StartPosition.z);
                Vector2 b = new Vector2(seg.EndPosition.x, seg.EndPosition.z);

                float dist = DistanceToSegment2D(p, a, b);
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

        private static float DistanceToSegment2D(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 0.0001f) return (p - a).magnitude;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 projection = a + t * ab;
            return (p - projection).magnitude;
        }

        public void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            in TerrainShaperContext context,
            float[,] outputBuffer)
        {
            if (resolution < 1) resolution = 120;
            int vertexCount = resolution + 1;
            float stepSize = size / resolution;

            float heightMultiplier = context.Noise.HeightMultiplier > 0.0001f ? context.Noise.HeightMultiplier : 1f;

            for (int y = 0; y < vertexCount; y++)
            {
                float currentZ = startZ + y * stepSize;
                for (int x = 0; x < vertexCount; x++)
                {
                    float currentX = startX + x * stepSize;
                    float worldElevation = CalculateElevation(
                        currentX,
                        currentZ,
                        in context);

                    // Store normalized height in [0, 1] range in the HeightMap buffer
                    outputBuffer[x, y] = worldElevation / heightMultiplier;
                }
            }
        }

        private static float SampleBaseNoise(float x, float z, NoiseSettings settings)
        {
            int[] permutation = CreatePermutationTable(settings.Seed);

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float maxPossibleHeight = 0f;

            for (int i = 0; i < settings.Octaves; i++)
            {
                maxPossibleHeight += amplitude;
                float sampleX = (x + settings.Offset.x) / settings.Scale * frequency;
                float sampleY = (z + settings.Offset.y) / settings.Scale * frequency;

                float n = 0f;
                switch (settings.Type)
                {
                    case NoiseType.PerlinFbm:
                        n = EvaluateGradientNoise(sampleX, sampleY, permutation);
                        break;
                    case NoiseType.RidgedMultifractal:
                        // 1.0 - abs(noise) creates sharp crests/ridges
                        n = 1.0f - Math.Abs(EvaluateGradientNoise(sampleX, sampleY, permutation));
                        n = n * n; // Square for sharper peaks
                        break;
                    case NoiseType.Billow:
                        // abs(noise) creates pillowy / rounded rolling mounds
                        n = Math.Abs(EvaluateGradientNoise(sampleX, sampleY, permutation));
                        break;
                }

                noiseHeight += n * amplitude;
                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }

            if (maxPossibleHeight > 0.0001f)
            {
                return Math.Max(0f, Math.Min(1f, noiseHeight / maxPossibleHeight));
            }
            return 0f;
        }

        private static float SamplePerlin01(float x, float z, float scale, int seed)
        {
            if (scale < 0.0001f) scale = 0.0001f;
            int[] perm = CreatePermutationTable(seed);
            return Math.Max(0f, Math.Min(1f, EvaluateGradientNoise(x / scale, z / scale, perm)));
        }

        private static float SampleRiverMask(float x, float z, RiverSettings settings)
        {
            if (settings.RiverbedWidth < 0.0001f) return 0f;
            int[] perm = CreatePermutationTable(settings.Seed);
            float nx = x / settings.Frequency;
            float nz = z / settings.Frequency;
            float n = EvaluateGradientNoise(nx, nz, perm);
            float distFromCenter = Math.Abs(n - 0.5f) * 2f;
            float channelFactor = 1f - Math.Min(1f, distFromCenter / (settings.RiverbedWidth / settings.Frequency));
            return Math.Max(0f, channelFactor);
        }

        private static float CalculateFalloffFactor(float x, float z, FalloffSettings settings)
        {
            if (settings.Mode == FalloffMode.None) return 1f;

            float dist = Math.Max(Math.Abs(x), Math.Abs(z));
            if (settings.Mode == FalloffMode.Circular)
            {
                dist = (float)Math.Sqrt(x * x + z * z);
            }

            if (dist <= settings.FalloffStartRadius) return 1f;
            if (dist >= settings.FalloffEndRadius) return 0f;

            float t = (dist - settings.FalloffStartRadius) / (settings.FalloffEndRadius - settings.FalloffStartRadius);
            float a = settings.PowerExponent > 0.01f ? settings.PowerExponent : 2.0f;
            float b = 3f;
            float falloff = (float)Math.Pow(t, a) / ((float)Math.Pow(t, a) + (float)Math.Pow(b - b * t, a));
            return Math.Max(0f, Math.Min(1f, 1f - falloff));
        }

        private static int[] CreatePermutationTable(int seed)
        {
            int[] p = new int[512];
            int[] permutation = new int[256];
            for (int i = 0; i < 256; i++) permutation[i] = i;

            System.Random prng = new System.Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int swapIndex = prng.Next(i + 1);
                int temp = permutation[i];
                permutation[i] = permutation[swapIndex];
                permutation[swapIndex] = temp;
            }

            for (int i = 0; i < 512; i++)
            {
                p[i] = permutation[i & 255];
            }
            return p;
        }

        private static float EvaluateGradientNoise(float x, float y, int[] p)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            float xf = x - (float)Math.Floor(x);
            float yf = y - (float)Math.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = p[p[xi] + yi];
            int ab = p[p[xi] + yi + 1];
            int ba = p[p[xi + 1] + yi];
            int bb = p[p[xi + 1] + yi + 1];

            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            float result = Lerp(x1, x2, v);
            return (result + 1f) * 0.5f;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            return GradientsX[h] * x + GradientsY[h] * y;
        }
    }
}
