namespace ProjectTwo.Terrain.Presentation.Materials
{
    using System;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Procedural engine for synthesizing seamless tileable surface textures (Albedo & Normal maps)
    /// using multi-octave noise, Voronoi cellular patterns, and analytical height derivation.
    /// </summary>
    public static class ProceduralTextureGenerator
    {
        public enum SurfacePreset
        {
            Grass,
            Rock,
            Sand,
            Snow,
            Dirt
        }

        [Serializable]
        public struct TextureGenerationParams
        {
            public int Resolution;
            public float Scale;
            public int Octaves;
            public float Persistence;
            public float Lacunarity;
            public Color BaseColor;
            public Color HighlightColor;
            public Color ShadowColor;
            public float Contrast;
            public float NormalStrength;
            public bool UseVoronoi;

            public static TextureGenerationParams CreateGrass()
            {
                return new TextureGenerationParams
                {
                    Resolution = 512,
                    Scale = 12f,
                    Octaves = 5,
                    Persistence = 0.55f,
                    Lacunarity = 2.2f,
                    BaseColor = new Color(0.22f, 0.48f, 0.18f),
                    HighlightColor = new Color(0.38f, 0.65f, 0.25f),
                    ShadowColor = new Color(0.12f, 0.28f, 0.08f),
                    Contrast = 1.2f,
                    NormalStrength = 2.5f,
                    UseVoronoi = false
                };
            }

            public static TextureGenerationParams CreateRock()
            {
                return new TextureGenerationParams
                {
                    Resolution = 512,
                    Scale = 8f,
                    Octaves = 6,
                    Persistence = 0.6f,
                    Lacunarity = 2.1f,
                    BaseColor = new Color(0.42f, 0.40f, 0.38f),
                    HighlightColor = new Color(0.65f, 0.62f, 0.58f),
                    ShadowColor = new Color(0.22f, 0.20f, 0.19f),
                    Contrast = 1.5f,
                    NormalStrength = 4.0f,
                    UseVoronoi = true
                };
            }

            public static TextureGenerationParams CreateSand()
            {
                return new TextureGenerationParams
                {
                    Resolution = 512,
                    Scale = 16f,
                    Octaves = 4,
                    Persistence = 0.45f,
                    Lacunarity = 2.0f,
                    BaseColor = new Color(0.78f, 0.72f, 0.48f),
                    HighlightColor = new Color(0.92f, 0.88f, 0.65f),
                    ShadowColor = new Color(0.62f, 0.55f, 0.35f),
                    Contrast = 1.0f,
                    NormalStrength = 1.8f,
                    UseVoronoi = false
                };
            }

            public static TextureGenerationParams CreateSnow()
            {
                return new TextureGenerationParams
                {
                    Resolution = 512,
                    Scale = 14f,
                    Octaves = 4,
                    Persistence = 0.4f,
                    Lacunarity = 2.0f,
                    BaseColor = new Color(0.92f, 0.94f, 0.98f),
                    HighlightColor = new Color(1.0f, 1.0f, 1.0f),
                    ShadowColor = new Color(0.78f, 0.84f, 0.92f),
                    Contrast = 0.9f,
                    NormalStrength = 1.2f,
                    UseVoronoi = false
                };
            }

            public static TextureGenerationParams CreateDirt()
            {
                return new TextureGenerationParams
                {
                    Resolution = 512,
                    Scale = 10f,
                    Octaves = 5,
                    Persistence = 0.5f,
                    Lacunarity = 2.2f,
                    BaseColor = new Color(0.35f, 0.25f, 0.18f),
                    HighlightColor = new Color(0.48f, 0.38f, 0.26f),
                    ShadowColor = new Color(0.20f, 0.14f, 0.10f),
                    Contrast = 1.3f,
                    NormalStrength = 3.0f,
                    UseVoronoi = true
                };
            }
        }

        public static Texture2D GenerateSeamlessAlbedo(TextureGenerationParams p)
        {
            int res = Mathf.Clamp(p.Resolution, 64, 2048);
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;
                    float height = SampleSeamlessNoise(u, v, p);

                    // Apply contrast
                    float val = Mathf.Clamp01((height - 0.5f) * p.Contrast + 0.5f);

                    Color col;
                    if (val < 0.5f)
                    {
                        float t = val * 2f;
                        col = Color.Lerp(p.ShadowColor, p.BaseColor, t);
                    }
                    else
                    {
                        float t = (val - 0.5f) * 2f;
                        col = Color.Lerp(p.BaseColor, p.HighlightColor, t);
                    }

                    col.a = 1f;
                    pixels[y * res + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            return tex;
        }

        public static Texture2D GenerateSeamlessNormalMap(TextureGenerationParams p)
        {
            int res = Mathf.Clamp(p.Resolution, 64, 2048);
            Texture2D normalTex = new Texture2D(res, res, TextureFormat.RGBA32, true, true);
            normalTex.wrapMode = TextureWrapMode.Repeat;
            normalTex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[res * res];
            float step = 1.0f / res;

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Sample neighbors with seamless toroidal wrapping
                    float hL = SampleSeamlessNoise((u - step + 1f) % 1f, v, p);
                    float hR = SampleSeamlessNoise((u + step) % 1f, v, p);
                    float hD = SampleSeamlessNoise(u, (v - step + 1f) % 1f, p);
                    float hU = SampleSeamlessNoise(u, (v + step) % 1f, p);

                    float dx = (hR - hL) * p.NormalStrength;
                    float dy = (hU - hD) * p.NormalStrength;

                    Vector3 normal = new Vector3(-dx, -dy, 1.0f).normalized;

                    // Pack into 0..1 range
                    Color packed = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1.0f);

                    pixels[y * res + x] = packed;
                }
            }

            normalTex.SetPixels(pixels);
            normalTex.Apply(true);
            return normalTex;
        }

        public static float SampleSeamlessNoise(float u, float v, TextureGenerationParams p)
        {
            // Map 2D UV (0..1) to a 4D torus for mathematically seamless tiling
            float s = u * Mathf.PI * 2f;
            float t = v * Mathf.PI * 2f;

            float nx = Mathf.Cos(s) / (Mathf.PI * 2f) * p.Scale;
            float ny = Mathf.Sin(s) / (Mathf.PI * 2f) * p.Scale;
            float nz = Mathf.Cos(t) / (Mathf.PI * 2f) * p.Scale;
            float nw = Mathf.Sin(t) / (Mathf.PI * 2f) * p.Scale;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < p.Octaves; i++)
            {
                // Multi-axis Perlin sampling
                float noiseVal1 = Mathf.PerlinNoise((nx + 100f) * frequency, (nz + 100f) * frequency);
                float noiseVal2 = Mathf.PerlinNoise((ny + 200f) * frequency, (nw + 200f) * frequency);
                float combined = (noiseVal1 + noiseVal2) * 0.5f;

                if (p.UseVoronoi)
                {
                    float voronoi = SampleVoronoi2D(u * p.Scale * frequency, v * p.Scale * frequency);
                    combined = Mathf.Lerp(combined, voronoi, 0.4f);
                }

                total += combined * amplitude;
                maxValue += amplitude;

                amplitude *= p.Persistence;
                frequency *= p.Lacunarity;
            }

            return maxValue > 0f ? total / maxValue : 0.5f;
        }

        private static float SampleVoronoi2D(float x, float y)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;

            float minDist = 1.0f;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int neighborX = ix + dx;
                    int neighborY = iy + dy;

                    // Pseudo-random point in grid cell
                    float randX = (Mathf.Sin(neighborX * 127.1f + neighborY * 311.7f) * 43758.5453f) % 1f;
                    float randY = (Mathf.Sin(neighborX * 269.5f + neighborY * 183.3f) * 43758.5453f) % 1f;
                    if (randX < 0) randX += 1f;
                    if (randY < 0) randY += 1f;

                    float px = dx + randX - fx;
                    float py = dy + randY - fy;
                    float dist = Mathf.Sqrt(px * px + py * py);

                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
            }

            return Mathf.Clamp01(minDist);
        }

        public static bool SaveTextureToPng(Texture2D texture, string relativeAssetPath)
        {
            if (texture == null || string.IsNullOrEmpty(relativeAssetPath))
                return false;

            try
            {
                byte[] pngBytes = texture.EncodeToPNG();
                string fullPath = Path.Combine(Application.dataPath, relativeAssetPath.Replace("Assets/", "").Replace("Assets\\", ""));
                
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(fullPath, pngBytes);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProceduralTextureGenerator] Failed to save PNG to {relativeAssetPath}: {ex.Message}");
                return false;
            }
        }
    }
}
