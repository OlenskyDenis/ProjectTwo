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
    /// </summary>
    public class ProceduralTerrainShaper : ITerrainShaper
    {
        private static readonly int[] GradientsX = { 1, -1, 1, -1, 1, -1, 0, 0 };
        private static readonly int[] GradientsY = { 0, 0, 1, 1, -1, -1, 1, -1 };

        public float CalculateElevation(
            float worldX,
            float worldZ,
            NoiseSettings noise,
            MacroMaskSettings macro,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            FalloffSettings falloff)
        {
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

            // 4. Apply Procedural River Carving
            if (river.Enabled && river.CarveDepth > 0.001f)
            {
                float riverSample = SampleRiverMask(worldX, worldZ, river);
                worldHeight -= riverSample * river.CarveDepth;
            }

            // 5. Apply Boundary / Island Falloff
            if (falloff.Mode != FalloffMode.None)
            {
                float falloffFactor = CalculateFalloffFactor(worldX, worldZ, falloff);
                worldHeight *= falloffFactor;
            }

            // 6. Apply Global Sea Level & Ocean Floor Basin
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

        public void GenerateHeightMap(
            float startX,
            float startZ,
            float size,
            int resolution,
            NoiseSettings noise,
            MacroMaskSettings macro,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            FalloffSettings falloff,
            float[,] outputBuffer)
        {
            if (resolution < 1) resolution = 120;
            int vertexCount = resolution + 1;
            float stepSize = size / resolution;

            float heightMultiplier = noise.HeightMultiplier > 0.0001f ? noise.HeightMultiplier : 1f;

            for (int y = 0; y < vertexCount; y++)
            {
                float currentZ = startZ + y * stepSize;
                for (int x = 0; x < vertexCount; x++)
                {
                    float currentX = startX + x * stepSize;
                    float worldElevation = CalculateElevation(
                        currentX,
                        currentZ,
                        noise,
                        macro,
                        heightCurve,
                        water,
                        river,
                        falloff);

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

            float normalized = (settings.Type == NoiseType.PerlinFbm)
                ? (noiseHeight / maxPossibleHeight + 1f) * 0.5f
                : noiseHeight / maxPossibleHeight;

            return Math.Max(0f, Math.Min(1f, normalized));
        }

        private static float SamplePerlin01(float x, float z, float scale, int seed)
        {
            int[] permutation = CreatePermutationTable(seed);
            float sampleX = x / scale;
            float sampleY = z / scale;
            float raw = EvaluateGradientNoise(sampleX, sampleY, permutation);
            return Math.Max(0f, Math.Min(1f, (raw + 1f) * 0.5f));
        }

        private static float SampleRiverMask(float x, float z, RiverSettings river)
        {
            int[] permutation = CreatePermutationTable(river.Seed);
            float sampleX = x * river.Frequency;
            float sampleY = z * river.Frequency;

            // Inverted ridge mask: absolute value close to 0 forms river centerlines
            float n = Math.Abs(EvaluateGradientNoise(sampleX, sampleY, permutation));
            float halfWidth = (river.RiverbedWidth * river.Frequency);
            if (halfWidth <= 0.0001f) halfWidth = 0.05f;

            if (n < halfWidth)
            {
                float t = n / halfWidth;
                float mask = 1f - t;
                // Smooth step
                return mask * mask * (3f - 2f * mask);
            }

            return 0f;
        }

        private static float CalculateFalloffFactor(float x, float z, FalloffSettings falloff)
        {
            float dist = 0f;
            if (falloff.Mode == FalloffMode.Circular)
            {
                dist = (float)Math.Sqrt(x * x + z * z);
            }
            else if (falloff.Mode == FalloffMode.Square)
            {
                dist = Math.Max(Math.Abs(x), Math.Abs(z));
            }

            if (dist <= falloff.FalloffStartRadius) return 1f;
            if (dist >= falloff.FalloffEndRadius) return 0f;

            float range = falloff.FalloffEndRadius - falloff.FalloffStartRadius;
            float t = (dist - falloff.FalloffStartRadius) / range;
            float factor = 1f - t;
            if (Math.Abs(falloff.PowerExponent - 1f) > 0.001f)
            {
                factor = (float)Math.Pow(factor, falloff.PowerExponent);
            }

            return Math.Max(0f, Math.Min(1f, factor));
        }

        private static float EvaluateGradientNoise(float x, float y, int[] p)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            float xf = x - (float)Math.Floor(x);
            float yf = y - (float)Math.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = p[p[xi] + yi] % 8;
            int ab = p[p[xi] + yi + 1] % 8;
            int ba = p[p[xi + 1] + yi] % 8;
            int bb = p[p[xi + 1] + yi + 1] % 8;

            float x1 = Lerp(DotGridGradient(aa, xf, yf), DotGridGradient(ba, xf - 1f, yf), u);
            float x2 = Lerp(DotGridGradient(ab, xf, yf - 1f), DotGridGradient(bb, xf - 1f, yf - 1f), u);

            return Lerp(x1, x2, v);
        }

        private static float DotGridGradient(int gradIndex, float x, float y)
        {
            return GradientsX[gradIndex] * x + GradientsY[gradIndex] * y;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        private static int[] CreatePermutationTable(int seed)
        {
            int[] p = new int[512];
            int[] baseTable = new int[256];

            for (int i = 0; i < 256; i++)
            {
                baseTable[i] = i;
            }

            uint state = (uint)(seed ^ 0x5DEECE66DL);
            for (int i = 255; i > 0; i--)
            {
                state = state * 1664525u + 1013904223u;
                int j = (int)(state % (uint)(i + 1));

                int temp = baseTable[i];
                baseTable[i] = baseTable[j];
                baseTable[j] = temp;
            }

            for (int i = 0; i < 512; i++)
            {
                p[i] = baseTable[i & 255];
            }

            return p;
        }
    }
}
