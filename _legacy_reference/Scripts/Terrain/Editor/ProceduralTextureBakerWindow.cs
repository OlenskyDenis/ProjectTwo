namespace ProjectTwo.Terrain.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Config;
    using ProjectTwo.Terrain.Presentation.Materials;

    /// <summary>
    /// Interactive Editor Window for procedurally synthesizing and baking seamless tileable PBR textures into PNG assets.
    /// </summary>
    public class ProceduralTextureBakerWindow : EditorWindow
    {
        private const string TexturesDirectory = "Assets/Textures/Terrain";

        private ProceduralTextureGenerator.SurfacePreset _selectedPreset = ProceduralTextureGenerator.SurfacePreset.Grass;
        private ProceduralTextureGenerator.TextureGenerationParams _params;
        private Texture2D _previewAlbedo;
        private Texture2D _previewNormal;
        private bool _needsPreviewUpdate = true;
        private Vector2 _scrollPos;

        [MenuItem("Window/ProjectTwo/Procedural Texture Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProceduralTextureBakerWindow>("Texture Baker");
            window.minSize = new Vector2(480, 620);
            window.Show();
        }

        [InitializeOnLoadMethod]
        public static void EnsureDefaultTexturesExist()
        {
            string grassPath = Path.Combine(Application.dataPath, "Textures/Terrain/Grass_Albedo.png");
            if (!File.Exists(grassPath))
            {
                BakeAllDefaultTextures();
            }
        }

        private void OnEnable()
        {
            LoadPreset(_selectedPreset);
        }

        private void OnDisable()
        {
            CleanupPreviews();
        }

        private void CleanupPreviews()
        {
            if (_previewAlbedo != null)
            {
                DestroyImmediate(_previewAlbedo);
                _previewAlbedo = null;
            }
            if (_previewNormal != null)
            {
                DestroyImmediate(_previewNormal);
                _previewNormal = null;
            }
        }

        private void LoadPreset(ProceduralTextureGenerator.SurfacePreset preset)
        {
            _selectedPreset = preset;
            switch (preset)
            {
                case ProceduralTextureGenerator.SurfacePreset.Grass:
                    _params = ProceduralTextureGenerator.TextureGenerationParams.CreateGrass();
                    break;
                case ProceduralTextureGenerator.SurfacePreset.Rock:
                    _params = ProceduralTextureGenerator.TextureGenerationParams.CreateRock();
                    break;
                case ProceduralTextureGenerator.SurfacePreset.Sand:
                    _params = ProceduralTextureGenerator.TextureGenerationParams.CreateSand();
                    break;
                case ProceduralTextureGenerator.SurfacePreset.Snow:
                    _params = ProceduralTextureGenerator.TextureGenerationParams.CreateSnow();
                    break;
                case ProceduralTextureGenerator.SurfacePreset.Dirt:
                    _params = ProceduralTextureGenerator.TextureGenerationParams.CreateDirt();
                    break;
            }
            _needsPreviewUpdate = true;
        }

        private void UpdatePreviews()
        {
            CleanupPreviews();
            // Generate low-res preview for fast interactive responsiveness
            var previewParams = _params;
            previewParams.Resolution = 256;

            _previewAlbedo = ProceduralTextureGenerator.GenerateSeamlessAlbedo(previewParams);
            _previewNormal = ProceduralTextureGenerator.GenerateSeamlessNormalMap(previewParams);
            _needsPreviewUpdate = false;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎨 Процедурний генератор та запікач текстур (Texture Baker)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Налаштуйте параметри шуму для генерації безшовних PBR-текстур (Albedo + Normal) та збережіть їх у форматі .PNG у папку Assets/Textures/Terrain/.", MessageType.Info);

            EditorGUILayout.Space(8);

            // Preset Selection
            EditorGUILayout.LabelField("🌟 Готові шаблони поверхонь:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌿 Трава", GUILayout.Height(28))) LoadPreset(ProceduralTextureGenerator.SurfacePreset.Grass);
            if (GUILayout.Button("🪨 Скеля", GUILayout.Height(28))) LoadPreset(ProceduralTextureGenerator.SurfacePreset.Rock);
            if (GUILayout.Button("🏖️ Пісок", GUILayout.Height(28))) LoadPreset(ProceduralTextureGenerator.SurfacePreset.Sand);
            if (GUILayout.Button("❄️ Сніг", GUILayout.Height(28))) LoadPreset(ProceduralTextureGenerator.SurfacePreset.Snow);
            if (GUILayout.Button("🟫 Земля", GUILayout.Height(28))) LoadPreset(ProceduralTextureGenerator.SurfacePreset.Dirt);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Parameter Controls with ChangeCheck
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("⚙️ Параметри процедурного шуму:", EditorStyles.boldLabel);
            _params.Resolution = EditorGUILayout.IntPopup("Роздільна здатність (Bake Res)", _params.Resolution,
                new string[] { "256x256", "512x512", "1024x1024", "2048x2048" },
                new int[] { 256, 512, 1024, 2048 });

            _params.Scale = EditorGUILayout.Slider("Масштаб шуму (Scale)", _params.Scale, 1f, 32f);
            _params.Octaves = EditorGUILayout.IntSlider("Октави деталей (Octaves)", _params.Octaves, 1, 8);
            _params.Persistence = EditorGUILayout.Slider("Стійкість (Persistence)", _params.Persistence, 0.1f, 0.9f);
            _params.Lacunarity = EditorGUILayout.Slider("Лакунарність (Lacunarity)", _params.Lacunarity, 1.2f, 3.5f);
            _params.Contrast = EditorGUILayout.Slider("Контраст (Contrast)", _params.Contrast, 0.5f, 3.0f);
            _params.NormalStrength = EditorGUILayout.Slider("Сила рельєфу (Normal Strength)", _params.NormalStrength, 0.5f, 8.0f);
            _params.UseVoronoi = EditorGUILayout.Toggle("Клітинний шум (Voronoi/Cracks)", _params.UseVoronoi);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎨 Колірна палітра шару:", EditorStyles.boldLabel);
            _params.HighlightColor = EditorGUILayout.ColorField("Світлий відтінок (Highlight)", _params.HighlightColor);
            _params.BaseColor = EditorGUILayout.ColorField("Базовий колір (Base)", _params.BaseColor);
            _params.ShadowColor = EditorGUILayout.ColorField("Тіньовий колір (Shadow)", _params.ShadowColor);

            if (EditorGUI.EndChangeCheck())
            {
                _needsPreviewUpdate = true;
            }

            if (_needsPreviewUpdate || _previewAlbedo == null)
            {
                UpdatePreviews();
            }

            EditorGUILayout.Space(12);

            // Live Preview Visuals
            EditorGUILayout.LabelField("👁️ Інтерактивне прев'ю (Live 2D Preview):", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (_previewAlbedo != null)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                EditorGUILayout.LabelField("Albedo (Колір)", EditorStyles.miniBoldLabel);
                Rect rectA = GUILayoutUtility.GetRect(190, 190);
                EditorGUI.DrawPreviewTexture(rectA, _previewAlbedo);
                EditorGUILayout.EndVertical();
            }

            if (_previewNormal != null)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                EditorGUILayout.LabelField("Normal Map (Рельєф)", EditorStyles.miniBoldLabel);
                Rect rectN = GUILayoutUtility.GetRect(190, 190);
                EditorGUI.DrawPreviewTexture(rectN, _previewNormal);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(14);

            // Baking Actions
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f);
            if (GUILayout.Button($"💾 Запекти {_selectedPreset} у PNG ({_params.Resolution}x{_params.Resolution})", GUILayout.Height(36)))
            {
                BakeSinglePreset(_selectedPreset, _params);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            if (GUILayout.Button("🌟 Запекти ВСІ 5 стандартних текстур у PNG (1-Click All)", GUILayout.Height(32)))
            {
                BakeAllDefaultTextures();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();
        }

        public static void BakeSinglePreset(ProceduralTextureGenerator.SurfacePreset preset, ProceduralTextureGenerator.TextureGenerationParams p)
        {
            EnsureDirectoryExists(TexturesDirectory);

            string name = preset.ToString();
            string albedoRelPath = $"{TexturesDirectory}/{name}_Albedo.png";
            string normalRelPath = $"{TexturesDirectory}/{name}_Normal.png";

            EditorUtility.DisplayProgressBar("Запікання текстур", $"Генерація {name} Albedo...", 0.3f);
            Texture2D albedo = ProceduralTextureGenerator.GenerateSeamlessAlbedo(p);
            ProceduralTextureGenerator.SaveTextureToPng(albedo, albedoRelPath);
            DestroyImmediate(albedo);

            EditorUtility.DisplayProgressBar("Запікання текстур", $"Генерація {name} Normal Map...", 0.7f);
            Texture2D normal = ProceduralTextureGenerator.GenerateSeamlessNormalMap(p);
            ProceduralTextureGenerator.SaveTextureToPng(normal, normalRelPath);
            DestroyImmediate(normal);

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
            ConfigureTextureImporter(normalRelPath, true);
            ConfigureTextureImporter(albedoRelPath, false);

            Debug.Log($"[ProceduralTextureBaker] Успішно збережено текстури: {albedoRelPath} та {normalRelPath}");
        }

        public static void BakeAllDefaultTextures()
        {
            EnsureDirectoryExists(TexturesDirectory);

            var presets = new (ProceduralTextureGenerator.SurfacePreset preset, ProceduralTextureGenerator.TextureGenerationParams p)[]
            {
                (ProceduralTextureGenerator.SurfacePreset.Grass, ProceduralTextureGenerator.TextureGenerationParams.CreateGrass()),
                (ProceduralTextureGenerator.SurfacePreset.Rock, ProceduralTextureGenerator.TextureGenerationParams.CreateRock()),
                (ProceduralTextureGenerator.SurfacePreset.Sand, ProceduralTextureGenerator.TextureGenerationParams.CreateSand()),
                (ProceduralTextureGenerator.SurfacePreset.Snow, ProceduralTextureGenerator.TextureGenerationParams.CreateSnow()),
                (ProceduralTextureGenerator.SurfacePreset.Dirt, ProceduralTextureGenerator.TextureGenerationParams.CreateDirt())
            };

            for (int i = 0; i < presets.Length; i++)
            {
                var item = presets[i];
                float progress = (float)i / presets.Length;
                EditorUtility.DisplayProgressBar("Запікання стандартних текстур", $"Генерація {item.preset}...", progress);
                BakeSinglePreset(item.preset, item.p);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // Auto-assign to default profile
            AssignTexturesToDefaultProfile();
        }

        private static void AssignTexturesToDefaultProfile()
        {
            string profilePath = "Assets/Settings/DefaultTerrainVisualProfile.asset";
            TerrainVisualProfileSO profile = AssetDatabase.LoadAssetAtPath<TerrainVisualProfileSO>(profilePath);

            Texture2D grassAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Grass_Albedo.png");
            Texture2D grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Grass_Normal.png");

            Texture2D rockAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Rock_Albedo.png");
            Texture2D rockNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Rock_Normal.png");

            Texture2D sandAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Sand_Albedo.png");
            Texture2D sandNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Sand_Normal.png");

            Texture2D snowAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Snow_Albedo.png");
            Texture2D snowNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDirectory}/Snow_Normal.png");

            if (profile != null)
            {
                Undo.RecordObject(profile, "Auto Assign Baked Textures");
                Shader triplanarShader = Shader.Find("ProjectTwo/Terrain/TriplanarLit");
                if (triplanarShader != null)
                {
                    profile.CustomTerrainShader = triplanarShader;
                }

                if (profile.BiomeBands != null)
                {
                    for (int i = 0; i < profile.BiomeBands.Count; i++)
                    {
                        var band = profile.BiomeBands[i];
                        if (band.Name.Contains("Grass") || band.Name.Contains("Forest"))
                        {
                            band.AlbedoTexture = grassAlbedo;
                            band.NormalMap = grassNormal;
                        }
                        else if (band.Name.Contains("Rock") || band.SlopeThreshold > 10f)
                        {
                            band.AlbedoTexture = rockAlbedo;
                            band.NormalMap = rockNormal;
                        }
                        else if (band.Name.Contains("Sand") || band.Name.Contains("Beach") || band.Name.Contains("Water"))
                        {
                            band.AlbedoTexture = sandAlbedo;
                            band.NormalMap = sandNormal;
                        }
                        else if (band.Name.Contains("Snow") || band.HeightThreshold >= 0.95f)
                        {
                            band.AlbedoTexture = snowAlbedo;
                            band.NormalMap = snowNormal;
                        }
                        profile.BiomeBands[i] = band;
                    }
                }

                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log("[ProceduralTextureBaker] Успішно підключено всі згенеровані текстури до DefaultTerrainVisualProfile!");
            }

            // Also auto-assign into TerrainConfig.asset's Regions
            string configPath = "Assets/TerrainConfig.asset";
            TerrainDataConfig config = AssetDatabase.LoadAssetAtPath<TerrainDataConfig>(configPath);
            if (config != null && config.Regions != null)
            {
                Undo.RecordObject(config, "Auto Assign Baked Textures to Config Regions");
                for (int i = 0; i < config.Regions.Length; i++)
                {
                    var reg = config.Regions[i];
                    if (reg.Name.Contains("Grass") || reg.Name.Contains("Forest"))
                    {
                        reg.AlbedoTexture = grassAlbedo;
                        reg.NormalMap = grassNormal;
                    }
                    else if (reg.Name.Contains("Rock") || reg.SlopeThreshold > 10f)
                    {
                        reg.AlbedoTexture = rockAlbedo;
                        reg.NormalMap = rockNormal;
                    }
                    else if (reg.Name.Contains("Sand") || reg.Name.Contains("Beach") || reg.Name.Contains("Water"))
                    {
                        reg.AlbedoTexture = sandAlbedo;
                        reg.NormalMap = sandNormal;
                    }
                    else if (reg.Name.Contains("Snow") || reg.HeightThreshold >= 0.95f)
                    {
                        reg.AlbedoTexture = snowAlbedo;
                        reg.NormalMap = snowNormal;
                    }
                    config.Regions[i] = reg;
                }

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("[ProceduralTextureBaker] Успішно заповнено шари TerrainConfig.Regions згенерованими текстурами!");
            }
        }

        private static void ConfigureTextureImporter(string relativeAssetPath, bool isNormalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(relativeAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureDirectoryExists(string relativeDir)
        {
            string fullPath = Path.Combine(Application.dataPath, relativeDir.Replace("Assets/", "").Replace("Assets\\", ""));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }
    }
}
