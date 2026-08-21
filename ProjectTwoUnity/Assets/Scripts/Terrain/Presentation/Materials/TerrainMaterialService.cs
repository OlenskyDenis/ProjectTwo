namespace ProjectTwo.Terrain.Presentation.Materials
{
    using System;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Config;

    /// <summary>
    /// Centralized material generation, configuration, caching, and live-update service for terrain and water surfaces.
    /// </summary>
    public sealed class TerrainMaterialService : ITerrainMaterialService
    {
        private const string DefaultTerrainShaderName = "ProjectTwo/Terrain/VertexColorLit";
        private const string DefaultWaterShaderName = "ProjectTwo/Terrain/WaterSimple";
        private const string FallbackLitShaderName = "Universal Render Pipeline/Lit";

        private readonly IMaterialCache _cache;
        private readonly bool _ownsCache;
        private bool _disposed;

        private TerrainVisualProfileSO _subscribedTerrainProfile;
        private WaterVisualProfileSO _subscribedWaterProfile;

        public TerrainMaterialService(IMaterialCache cache = null)
        {
            if (cache != null)
            {
                _cache = cache;
                _ownsCache = false;
            }
            else
            {
                _cache = new TerrainMaterialCache();
                _ownsCache = true;
            }
        }

        /// <inheritdoc />
        public Material GetOrCreateTerrainMaterial(TerrainVisualProfileSO profile = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainMaterialService));

            SubscribeTerrainProfile(profile);

            string cacheKey = ComputeTerrainKey(profile);
            return _cache.GetOrAdd(cacheKey, () => CreateTerrainMaterialInstance(profile));
        }

        /// <inheritdoc />
        public Material GetOrCreateWaterMaterial(WaterVisualProfileSO waterProfile = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainMaterialService));

            SubscribeWaterProfile(waterProfile);

            string cacheKey = ComputeWaterKey(waterProfile);
            return _cache.GetOrAdd(cacheKey, () => CreateWaterMaterialInstance(waterProfile));
        }

        /// <inheritdoc />
        public void UpdateTerrainMaterialProperties(TerrainVisualProfileSO profile)
        {
            if (_disposed || profile == null) return;

            string cacheKey = ComputeTerrainKey(profile);
            if (_cache.TryGet(cacheKey, out Material material) && material != null)
            {
                ApplyTerrainProperties(material, profile);
            }
        }

        /// <inheritdoc />
        public void UpdateWaterMaterialProperties(WaterVisualProfileSO waterProfile)
        {
            if (_disposed || waterProfile == null) return;

            string cacheKey = ComputeWaterKey(waterProfile);
            if (_cache.TryGet(cacheKey, out Material material) && material != null)
            {
                ApplyWaterProperties(material, waterProfile);
            }
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            UnsubscribeProfiles();
            if (_ownsCache)
            {
                _cache.Dispose();
            }
            _disposed = true;
        }

        private void SubscribeTerrainProfile(TerrainVisualProfileSO profile)
        {
            if (profile == null || profile == _subscribedTerrainProfile) return;

            if (_subscribedTerrainProfile != null)
            {
                _subscribedTerrainProfile.OnProfileChanged -= OnObservedTerrainProfileChanged;
            }

            _subscribedTerrainProfile = profile;
            _subscribedTerrainProfile.OnProfileChanged += OnObservedTerrainProfileChanged;
        }

        private void SubscribeWaterProfile(WaterVisualProfileSO profile)
        {
            if (profile == null || profile == _subscribedWaterProfile) return;

            if (_subscribedWaterProfile != null)
            {
                _subscribedWaterProfile.OnProfileChanged -= OnObservedWaterProfileChanged;
            }

            _subscribedWaterProfile = profile;
            _subscribedWaterProfile.OnProfileChanged += OnObservedWaterProfileChanged;
        }

        private void UnsubscribeProfiles()
        {
            if (_subscribedTerrainProfile != null)
            {
                _subscribedTerrainProfile.OnProfileChanged -= OnObservedTerrainProfileChanged;
                _subscribedTerrainProfile = null;
            }

            if (_subscribedWaterProfile != null)
            {
                _subscribedWaterProfile.OnProfileChanged -= OnObservedWaterProfileChanged;
                _subscribedWaterProfile = null;
            }
        }

        private void OnObservedTerrainProfileChanged()
        {
            if (_subscribedTerrainProfile != null)
            {
                UpdateTerrainMaterialProperties(_subscribedTerrainProfile);
            }
        }

        private void OnObservedWaterProfileChanged()
        {
            if (_subscribedWaterProfile != null)
            {
                UpdateWaterMaterialProperties(_subscribedWaterProfile);
            }
        }

        private static string ComputeTerrainKey(TerrainVisualProfileSO profile)
        {
            if (profile == null)
                return "terrain_default";

            if (profile.DirectMaterialOverride != null)
                return $"terrain_override_{profile.DirectMaterialOverride.GetHashCode()}";

            return $"terrain_profile_{profile.GetHashCode()}";
        }

        private static string ComputeWaterKey(WaterVisualProfileSO profile)
        {
            if (profile == null)
                return "water_default";

            if (profile.DirectWaterMaterialOverride != null)
                return $"water_override_{profile.DirectWaterMaterialOverride.GetHashCode()}";

            return $"water_profile_{profile.GetHashCode()}";
        }

        private static Material CreateTerrainMaterialInstance(TerrainVisualProfileSO profile)
        {
            if (profile != null && profile.DirectMaterialOverride != null)
            {
                return profile.DirectMaterialOverride;
            }

            Shader shader = ResolveShader(profile != null ? profile.CustomTerrainShader : null, DefaultTerrainShaderName);
            Material mat = new Material(shader)
            {
                name = profile != null ? $"TerrainMaterial_{profile.name}" : "DefaultTerrainVertexMaterial",
                hideFlags = HideFlags.DontSave
            };

            if (profile != null)
            {
                ApplyTerrainProperties(mat, profile);
            }

            return mat;
        }

        private static Material CreateWaterMaterialInstance(WaterVisualProfileSO profile)
        {
            if (profile != null && profile.DirectWaterMaterialOverride != null)
            {
                return profile.DirectWaterMaterialOverride;
            }

            Shader shader = ResolveShader(profile != null ? profile.CustomWaterShader : null, DefaultWaterShaderName);
            Material mat = new Material(shader)
            {
                name = profile != null ? $"WaterMaterial_{profile.name}" : "DefaultWaterMaterial",
                hideFlags = HideFlags.DontSave
            };

            if (profile != null)
            {
                ApplyWaterProperties(mat, profile);
            }
            else
            {
                ApplyDefaultWaterProperties(mat);
            }

            return mat;
        }

        private static void ApplyTerrainProperties(Material mat, TerrainVisualProfileSO profile)
        {
            if (mat == null || profile == null) return;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", profile.GlobalTint);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", profile.GlobalTint);

            // Apply Biome Bands to Triplanar shader properties
            if (profile.BiomeBands != null && profile.BiomeBands.Count > 0)
            {
                Texture2D flatTex = null;
                Texture2D flatNorm = null;
                Texture2D slopeTex = null;
                Texture2D slopeNorm = null;
                Texture2D peakTex = null;
                Texture2D peakNorm = null;

                for (int i = 0; i < profile.BiomeBands.Count; i++)
                {
                    var band = profile.BiomeBands[i];
                    string name = band.Name != null ? band.Name.ToLowerInvariant() : "";

                    if (name.Contains("snow") || band.HeightThreshold >= 0.95f)
                    {
                        if (peakTex == null && band.AlbedoTexture != null) peakTex = band.AlbedoTexture;
                        if (peakNorm == null && band.NormalMap != null) peakNorm = band.NormalMap;
                    }
                    else if (name.Contains("rock") || band.SlopeThreshold > 15f)
                    {
                        if (slopeTex == null && band.AlbedoTexture != null) slopeTex = band.AlbedoTexture;
                        if (slopeNorm == null && band.NormalMap != null) slopeNorm = band.NormalMap;
                    }
                    else if (name.Contains("grass") || name.Contains("forest") || name.Contains("meadow") || (band.HeightThreshold >= 0.4f && band.HeightThreshold < 0.85f))
                    {
                        if (flatTex == null && band.AlbedoTexture != null) flatTex = band.AlbedoTexture;
                        if (flatNorm == null && band.NormalMap != null) flatNorm = band.NormalMap;
                    }
                }

                if (flatTex != null && mat.HasProperty("_FlatTex")) mat.SetTexture("_FlatTex", flatTex);
                if (flatNorm != null && mat.HasProperty("_FlatNormal")) mat.SetTexture("_FlatNormal", flatNorm);

                if (slopeTex != null && mat.HasProperty("_SlopeTex")) mat.SetTexture("_SlopeTex", slopeTex);
                if (slopeNorm != null && mat.HasProperty("_SlopeNormal")) mat.SetTexture("_SlopeNormal", slopeNorm);

                if (peakTex != null && mat.HasProperty("_PeakTex")) mat.SetTexture("_PeakTex", peakTex);
                if (peakNorm != null && mat.HasProperty("_PeakNormal")) mat.SetTexture("_PeakNormal", peakNorm);
            }
        }

        private static void ApplyWaterProperties(Material mat, WaterVisualProfileSO profile)
        {
            if (mat == null || profile == null) return;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", profile.DeepWaterColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", profile.DeepWaterColor);

            if (mat.HasProperty("_ShallowColor"))
                mat.SetColor("_ShallowColor", profile.ShallowWaterColor);

            if (mat.HasProperty("_FlowSpeed"))
                mat.SetFloat("_FlowSpeed", profile.FlowSpeed);
        }

        private static void ApplyDefaultWaterProperties(Material mat)
        {
            if (mat == null) return;
            Color defaultColor = new Color(0.12f, 0.38f, 0.68f, 0.85f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", defaultColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", defaultColor);
        }

        private static Shader ResolveShader(Shader customShader, string defaultShaderName)
        {
            if (customShader != null && customShader.isSupported)
                return customShader;

            Shader defaultShader = Shader.Find(defaultShaderName);
            if (defaultShader != null && defaultShader.isSupported)
                return defaultShader;

            Shader fallbackLit = Shader.Find(FallbackLitShaderName);
            if (fallbackLit != null && fallbackLit.isSupported)
                return fallbackLit;

            Shader standard = Shader.Find("Standard");
            if (standard != null)
                return standard;

            return Shader.Find("Sprites/Default");
        }
    }
}
