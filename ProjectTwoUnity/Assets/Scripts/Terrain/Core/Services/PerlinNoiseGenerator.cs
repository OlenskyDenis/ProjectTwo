namespace ProjectTwo.Terrain.Core.Services
{
    using System;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# deterministic Gradient Perlin Noise implementation with multi-octave Fractal Brownian Motion (fBm).
    /// Thread-safe and independent from Unity engine APIs.
    /// </summary>
    public class PerlinNoiseGenerator : INoiseGenerator
    {
        private static readonly int[] GradientsX = { 1, -1, 1, -1, 1, -1, 0, 0 };
        private static readonly int[] GradientsY = { 0, 0, 1, 1, -1, -1, 1, -1 };

        public HeightMap GenerateHeightMap(int mapWidth, int mapHeight, NoiseSettings settings, ChunkCoordinate chunkCoord)
        {
            if (mapWidth <= 0) mapWidth = 120;
            if (mapHeight <= 0) mapHeight = 120;
            settings.Validate();

            float[,] heightMap = new float[mapWidth, mapHeight];
            float maxPossibleHeight = 0f;
            float amplitude = 1f;

            for (int i = 0; i < settings.Octaves; i++)
            {
                maxPossibleHeight += amplitude;
                amplitude *= settings.Persistence;
            }

            int[] permutation = CreatePermutationTable(settings.Seed);

            // Calculate chunk base offset in noise space
            float offsetX = chunkCoord.X * (mapWidth - 1) + settings.Offset.x;
            float offsetY = chunkCoord.Z * (mapHeight - 1) + settings.Offset.y;

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int i = 0; i < settings.Octaves; i++)
                    {
                        float sampleX = (x + offsetX) / settings.Scale * frequency;
                        float sampleY = (y + offsetY) / settings.Scale * frequency;

                        float perlinValue = EvaluateGradientNoise(sampleX, sampleY, permutation);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= settings.Persistence;
                        frequency *= settings.Lacunarity;
                    }

                    // Normalize to [0, 1] range based on theoretical max height
                    float normalizedHeight = (noiseHeight / maxPossibleHeight + 1f) * 0.5f;
                    normalizedHeight = Math.Max(0f, Math.Min(1f, normalizedHeight));

                    heightMap[x, y] = normalizedHeight;

                    if (normalizedHeight > maxHeight) maxHeight = normalizedHeight;
                    if (normalizedHeight < minHeight) minHeight = normalizedHeight;
                }
            }

            return new HeightMap(heightMap, minHeight, maxHeight);
        }

        public float SampleNoise(float x, float y, NoiseSettings settings)
        {
            settings.Validate();
            int[] permutation = CreatePermutationTable(settings.Seed);

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float maxPossibleHeight = 0f;

            for (int i = 0; i < settings.Octaves; i++)
            {
                maxPossibleHeight += amplitude;
                float sampleX = (x + settings.Offset.x) / settings.Scale * frequency;
                float sampleY = (y + settings.Offset.y) / settings.Scale * frequency;

                float perlinValue = EvaluateGradientNoise(sampleX, sampleY, permutation);
                noiseHeight += perlinValue * amplitude;

                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }

            float normalized = (noiseHeight / maxPossibleHeight + 1f) * 0.5f;
            return Math.Max(0f, Math.Min(1f, normalized));
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
            // 6t^5 - 15t^4 + 10t^3
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

            // Pseudo-random deterministic shuffle using custom Linear Congruential Generator (LCG)
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
