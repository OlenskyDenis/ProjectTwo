namespace ProjectTwo.Terrain.Editor
{
    using System;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Presentation.Components;
    using ProjectTwo.Terrain.Presentation.Config;

    /// <summary>
    /// Premium 3-Tab Custom Inspector for TerrainDataConfig.
    /// Features 1-click presets, live scene update, and dedicated reset-to-default actions for all modes and parameters.
    /// </summary>
    [CustomEditor(typeof(TerrainDataConfig))]
    public class TerrainDataConfigEditor : Editor
    {
        private SerializedProperty _chunkSizeProp;
        private SerializedProperty _chunkResolutionProp;
        private SerializedProperty _noiseSettingsProp;
        private SerializedProperty _macroSettingsProp;
        private SerializedProperty _tectonicSettingsProp;
        private SerializedProperty _heightCurveSettingsProp;
        private SerializedProperty _falloffSettingsProp;
        private SerializedProperty _waterSettingsProp;
        private SerializedProperty _riverSettingsProp;
        private SerializedProperty _hydrologySettingsProp;
        private SerializedProperty _lodTiersProp;
        private SerializedProperty _maxViewDistanceProp;
        private SerializedProperty _regionsProp;
        private SerializedProperty _terrainMaterialProp;
        private SerializedProperty _enablePersistenceProp;

        private ReorderableList _regionsList;

        // Navigation Tab State (0 = Express, 1 = Biomes, 2 = Pro)
        private static int _selectedTab = 0;
        private static readonly string[] TabTitles = { "🌟 Швидкий режим", "🎨 Біоми й Кольори", "🛠️ Pro / Детально" };

        // Live Auto-Update State
        private static bool _liveAutoUpdateScene = true;

        // Pro Sub-foldout States
        private static bool _proGridFold = false;
        private static bool _proNoiseFold = true;
        private static bool _proMacroFold = false;
        private static bool _proTectonicsFold = false;
        private static bool _proCurvesFold = false;
        private static bool _proHydrologyFold = false;
        private static bool _proFalloffFold = false;
        private static bool _proLodFold = false;
        private static bool _proExportFold = false;

        private void OnEnable()
        {
            _chunkSizeProp = serializedObject.FindProperty("ChunkSize");
            _chunkResolutionProp = serializedObject.FindProperty("ChunkResolution");
            _noiseSettingsProp = serializedObject.FindProperty("NoiseSettings");
            _macroSettingsProp = serializedObject.FindProperty("MacroSettings");
            _tectonicSettingsProp = serializedObject.FindProperty("TectonicSettings");
            _heightCurveSettingsProp = serializedObject.FindProperty("HeightCurveSettings");
            _falloffSettingsProp = serializedObject.FindProperty("FalloffSettings");
            _waterSettingsProp = serializedObject.FindProperty("WaterSettings");
            _riverSettingsProp = serializedObject.FindProperty("RiverSettings");
            _hydrologySettingsProp = serializedObject.FindProperty("HydrologySettings");
            _lodTiersProp = serializedObject.FindProperty("LodTiers");
            _maxViewDistanceProp = serializedObject.FindProperty("MaxViewDistance");
            _regionsProp = serializedObject.FindProperty("Regions");
            _terrainMaterialProp = serializedObject.FindProperty("TerrainMaterial");
            _enablePersistenceProp = serializedObject.FindProperty("EnablePersistence");

            InitializeRegionsList();
        }

        private void InitializeRegionsList()
        {
            _regionsList = new ReorderableList(serializedObject, _regionsProp, true, true, true, true)
            {
                elementHeightCallback = (int index) => EditorGUIUtility.singleLineHeight * 2 + 8,

                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Шари рельєфу (Висота → Колір → Схил)", EditorStyles.boldLabel);
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = _regionsProp.GetArrayElementAtIndex(index);
                    SerializedProperty nameProp = element.FindPropertyRelative("Name");
                    SerializedProperty thresholdProp = element.FindPropertyRelative("HeightThreshold");
                    SerializedProperty slopeProp = element.FindPropertyRelative("SlopeThreshold");
                    SerializedProperty colorProp = element.FindPropertyRelative("ColorTint");
                    SerializedProperty albedoProp = element.FindPropertyRelative("AlbedoTexture");

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float y = rect.y + 2;

                    // Row 1: Name + Height Slider + Color Picker
                    float nameW = rect.width * 0.28f;
                    float threshW = rect.width * 0.44f;
                    float colorW = rect.width - nameW - threshW - 8f;

                    EditorGUI.PropertyField(new Rect(rect.x, y, nameW, lineHeight), nameProp, GUIContent.none);
                    thresholdProp.floatValue = EditorGUI.Slider(new Rect(rect.x + nameW + 4, y, threshW, lineHeight), thresholdProp.floatValue, 0f, 1f);
                    EditorGUI.PropertyField(new Rect(rect.x + nameW + threshW + 8, y, colorW, lineHeight), colorProp, GUIContent.none);

                    y += lineHeight + 2;

                    // Row 2: Slope Threshold + Albedo Texture Slot
                    float slopeLabelW = 85f;
                    float slopeFieldW = rect.width * 0.40f;
                    EditorGUI.LabelField(new Rect(rect.x, y, slopeLabelW, lineHeight), "Схил (градуси):");
                    slopeProp.floatValue = EditorGUI.Slider(new Rect(rect.x + slopeLabelW, y, slopeFieldW - slopeLabelW, lineHeight), slopeProp.floatValue, 0f, 90f);

                    float texLabelW = 60f;
                    float texFieldW = rect.width - slopeFieldW - texLabelW - 8f;
                    EditorGUI.LabelField(new Rect(rect.x + slopeFieldW + 6, y, texLabelW, lineHeight), "Текстура:");
                    EditorGUI.PropertyField(new Rect(rect.x + slopeFieldW + texLabelW + 6, y, texFieldW, lineHeight), albedoProp, GUIContent.none);
                },

                onAddCallback = (ReorderableList list) =>
                {
                    int newIndex = list.serializedProperty.arraySize;
                    list.serializedProperty.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(newIndex);

                    element.FindPropertyRelative("Name").stringValue = $"Новий шар {newIndex + 1}";
                    element.FindPropertyRelative("HeightThreshold").floatValue = 1.0f;
                    element.FindPropertyRelative("SlopeThreshold").floatValue = 0f;
                    element.FindPropertyRelative("ColorTint").colorValue = Color.white;
                    element.FindPropertyRelative("AlbedoTexture").objectReferenceValue = null;
                    element.FindPropertyRelative("NormalMap").objectReferenceValue = null;
                    element.FindPropertyRelative("Tiling").vector2Value = new Vector2(1f, 1f);
                    element.FindPropertyRelative("BlendSoftness").floatValue = 0.1f;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            TerrainDataConfig config = (TerrainDataConfig)target;

            // 1. Top Live Sync Header (Navigation & Manual Refresh)
            DrawLiveSyncHeader(config);

            EditorGUILayout.Space(6);

            // 2. Navigation Tabs Toolbar (Pure UI state - does not trigger scene regeneration)
            DrawNavigationTabs();

            EditorGUILayout.Space(8);

            // 3. Tab Contents with explicit change tracking for serialized properties
            EditorGUI.BeginChangeCheck();

            switch (_selectedTab)
            {
                case 0:
                    DrawExpressTab(config);
                    break;
                case 1:
                    DrawBiomesTab(config);
                    break;
                case 2:
                    DrawProTab(config);
                    break;
            }

            EditorGUILayout.Space(12);

            // 4. Global Emergency Reset Button (Always available at bottom)
            DrawGlobalResetButton(config);

            bool changed = EditorGUI.EndChangeCheck();

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                config.Validate();
                EditorUtility.SetDirty(config);

                if (_liveAutoUpdateScene)
                {
                    RegenerateActiveSceneTerrain();
                }
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawLiveSyncHeader(TerrainDataConfig config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            EditorGUILayout.LabelField("⚡ Live 3D Scene", titleStyle, GUILayout.Width(110));

            _liveAutoUpdateScene = EditorGUILayout.ToggleLeft(
                new GUIContent("Авто-оновлення сцени", "Миттєво перераховує та відображає 3D-ландшафт у Scene View при русі будь-якого повзунка."),
                _liveAutoUpdateScene, GUILayout.Width(160));

            if (GUILayout.Button(new GUIContent("⚡ Оновити сцену", "Примусово перегенерувати ландшафт на поточній сцені."), GUILayout.Height(22)))
            {
                RegenerateActiveSceneTerrain();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawNavigationTabs()
        {
            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };

            _selectedTab = GUILayout.Toolbar(_selectedTab, TabTitles, tabStyle);
        }

        #region Tab 0: Express Mode

        private void DrawExpressTab(TerrainDataConfig config)
        {
            // Archetype Preset Buttons
            EditorGUILayout.LabelField("🌟 Готові світи в 1 клік:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🏔️ Альпійські Гори", GUILayout.Height(32))) ApplyAlpinePreset(config);
            if (GUILayout.Button("🏝️ Острів в Океані", GUILayout.Height(32))) ApplyArchipelagoPreset(config);
            if (GUILayout.Button("🌄 Зелені Пагорби", GUILayout.Height(32))) ApplyRollingPlainsPreset(config);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🏜️ Каньйони та Річки", GUILayout.Height(32))) ApplyCanyonPreset(config);
            if (GUILayout.Button("🌾 Безкраї Рівнини", GUILayout.Height(32))) ApplyEndlessPlainsPreset(config);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Hero Action: Randomize Seed
            SerializedProperty seedProp = _noiseSettingsProp.FindPropertyRelative("Seed");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎲 Згенерувати інший вигляд світу (New Seed)", GUILayout.Height(30)))
            {
                seedProp.intValue = UnityEngine.Random.Range(1, 999999);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🎛️ Головні налаштування форми:", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 1. Mountain Height (meters)
            SerializedProperty heightMultProp = _noiseSettingsProp.FindPropertyRelative("HeightMultiplier");
            EditorGUILayout.Slider(heightMultProp, 10f, 250f, new GUIContent("🏔️ Висота гір (метри)", "Загальна вертикальна висота найвищих вершин у метрах."));

            // 2. Feature Scale (zoom)
            SerializedProperty scaleProp = _noiseSettingsProp.FindPropertyRelative("Scale");
            EditorGUILayout.Slider(scaleProp, 40f, 400f, new GUIContent("🔭 Масштаб простору", "Більше значення = широкі величні гори. Менше значення = часті дрібні пагорби."));

            // 3. Sharpness & Roughness
            SerializedProperty persistenceProp = _noiseSettingsProp.FindPropertyRelative("Persistence");
            EditorGUILayout.Slider(persistenceProp, 0.2f, 0.75f, new GUIContent("⚡ Гострота та скелястість", "Шорсткість та деталізація скель. Більше значення робить схили гострими та кам'янистими."));

            // 4. Water Level
            SerializedProperty waterEnabledProp = _waterSettingsProp.FindPropertyRelative("Enabled");
            SerializedProperty seaLevelProp = _waterSettingsProp.FindPropertyRelative("SeaLevel");
            EditorGUILayout.BeginHorizontal();
            waterEnabledProp.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("🌊 Океан / Рівень води:", "Вмикає світовий рівень моря з формуванням берегової лінії та дна."), waterEnabledProp.boolValue, GUILayout.Width(160));
            if (waterEnabledProp.boolValue)
            {
                seaLevelProp.floatValue = EditorGUILayout.Slider(seaLevelProp.floatValue, 1f, 60f);
            }
            EditorGUILayout.EndHorizontal();

            // 5. Island Falloff
            SerializedProperty falloffModeProp = _falloffSettingsProp.FindPropertyRelative("Mode");
            SerializedProperty falloffEndProp = _falloffSettingsProp.FindPropertyRelative("FalloffEndRadius");
            bool isIsland = falloffModeProp.enumValueIndex != (int)FalloffMode.None;
            EditorGUILayout.BeginHorizontal();
            bool newIsIsland = EditorGUILayout.ToggleLeft(new GUIContent("🏝️ Форма острова (Falloff):", "Плавно опускає краї світу до рівня води, утворюючи відокремлений острів."), isIsland, GUILayout.Width(160));
            if (newIsIsland != isIsland)
            {
                falloffModeProp.enumValueIndex = newIsIsland ? (int)FalloffMode.Circular : (int)FalloffMode.None;
            }
            if (newIsIsland)
            {
                falloffEndProp.floatValue = EditorGUILayout.Slider(falloffEndProp.floatValue, 100f, 600f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("🔄 Скинути форму до збалансованої (Reset Shape)", GUILayout.Height(24)))
            {
                Undo.RecordObject(config, "Reset Shape Defaults");
                config.NoiseSettings = new NoiseSettings
                {
                    Type = NoiseType.PerlinFbm,
                    Seed = 1337,
                    Scale = 140f,
                    Octaves = 4,
                    Persistence = 0.45f,
                    Lacunarity = 2.0f,
                    HeightMultiplier = 45f,
                    Offset = Vector2.zero
                };
                config.WaterSettings = WaterSettings.Default;
                config.FalloffSettings = FalloffSettings.Default;
                config.Validate();
                EditorUtility.SetDirty(config);
                RegenerateActiveSceneTerrain();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Tab 1: Biomes & Colors

        private void DrawBiomesTab(TerrainDataConfig config)
        {
            EditorGUILayout.LabelField("🎨 Готові палітри кольорів у 1 клік:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🌲 Літо", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Apply Summer Biome");
                config.Regions = TerrainRegion.CreateDefaultRegions();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("🍂 Осінь", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Apply Autumn Biome");
                config.Regions = TerrainRegion.CreateAutumnRegions();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("❄️ Зима", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Apply Arctic Biome");
                config.Regions = TerrainRegion.CreateArcticRegions();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("🏜️ Пустеля", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Apply Desert Biome");
                config.Regions = TerrainRegion.CreateDesertRegions();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("🏝️ Тропіки", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Apply Tropical Biome");
                config.Regions = TerrainRegion.CreateTropicalRegions();
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Interactive Elevation Gradient Bar
            DrawHeightGradientBar(config);

            EditorGUILayout.Space(6);

            // Reorderable Biomes List
            _regionsList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Впорядкувати за висотою (Sort)", GUILayout.Height(24)))
            {
                Undo.RecordObject(config, "Sort Biomes");
                if (config.Regions != null)
                {
                    Array.Sort(config.Regions, (a, b) => a.HeightThreshold.CompareTo(b.HeightThreshold));
                    EditorUtility.SetDirty(config);
                }
            }
            if (GUILayout.Button("🔄 Скинути біоми до стандартних", GUILayout.Height(24)))
            {
                Undo.RecordObject(config, "Reset Biomes");
                config.Regions = TerrainRegion.CreateDefaultRegions();
                EditorUtility.SetDirty(config);
                RegenerateActiveSceneTerrain();
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 2: Pro Mode

        private void DrawProTab(TerrainDataConfig config)
        {
            EditorGUILayout.HelpBox("🛠️ Повний доступ до всіх математичних параметрів, фракталів, річок та LOD-стрімінгу.", MessageType.None);

            // 1. Grid & Resolution
            _proGridFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proGridFold, "📐 Розміри чанка та сітка (Multiples of 12)");
            if (_proGridFold)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                int rawChunkSize = EditorGUILayout.IntSlider(new GUIContent("Розмір чанка (Chunk Size)"), _chunkSizeProp.intValue, 24, 480);
                if (EditorGUI.EndChangeCheck()) _chunkSizeProp.intValue = Mathf.RoundToInt(rawChunkSize / 12f) * 12;

                EditorGUI.BeginChangeCheck();
                int rawRes = EditorGUILayout.IntSlider(new GUIContent("Роздільна здатність (Resolution)"), _chunkResolutionProp.intValue, 24, 240);
                if (EditorGUI.EndChangeCheck()) _chunkResolutionProp.intValue = Mathf.RoundToInt(rawRes / 12f) * 12;

                EditorGUILayout.HelpBox($"✓ Гарантія безшовності: {config.ChunkResolution} сегментів = {config.ChunkResolution + 1}x{config.ChunkResolution + 1} вершин. Ділиться на LOD кроки (1, 2, 4, 6).", MessageType.Info);

                if (GUILayout.Button("🔄 Скинути сітку до стандарту (240m / 120seg)"))
                {
                    _chunkSizeProp.intValue = 240;
                    _chunkResolutionProp.intValue = 120;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 2. Direct Noise Settings
            _proNoiseFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proNoiseFold, "⛰️ Математика шуму (Fractal Noise)");
            if (_proNoiseFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_noiseSettingsProp.FindPropertyRelative("Type"), new GUIContent("Тип алгоритму (Mode)"));
                EditorGUILayout.PropertyField(_noiseSettingsProp.FindPropertyRelative("Seed"), new GUIContent("Seed генератора"));
                EditorGUILayout.Slider(_noiseSettingsProp.FindPropertyRelative("Scale"), 10f, 500f, new GUIContent("Scale (Масштаб)"));
                EditorGUILayout.Slider(_noiseSettingsProp.FindPropertyRelative("HeightMultiplier"), 5f, 300f, new GUIContent("Height Multiplier"));
                EditorGUILayout.IntSlider(_noiseSettingsProp.FindPropertyRelative("Octaves"), 1, 8, new GUIContent("Octaves (Шари деталей)"));
                EditorGUILayout.Slider(_noiseSettingsProp.FindPropertyRelative("Persistence"), 0.05f, 0.95f, new GUIContent("Persistence (Шорсткість)"));
                EditorGUILayout.Slider(_noiseSettingsProp.FindPropertyRelative("Lacunarity"), 1.1f, 4.0f, new GUIContent("Lacunarity (Частота)"));
                EditorGUILayout.PropertyField(_noiseSettingsProp.FindPropertyRelative("Offset"), new GUIContent("2D Offset"));

                if (GUILayout.Button("🔄 Скинути параметри шуму"))
                {
                    config.NoiseSettings = new NoiseSettings
                    {
                        Type = NoiseType.PerlinFbm,
                        Seed = 1337,
                        Scale = 140f,
                        Octaves = 4,
                        Persistence = 0.45f,
                        Lacunarity = 2.0f,
                        HeightMultiplier = 45f,
                        Offset = Vector2.zero
                    };
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 3. Macro Mountain Mask
            _proMacroFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proMacroFold, "🏔️ Макро-маска континентів (Macro Mask)");
            if (_proMacroFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_macroSettingsProp, true);
                if (GUILayout.Button("🔄 Скинути макро-маску"))
                {
                    config.MacroSettings = MacroMaskSettings.Default;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 4. Tectonic Macro-Zoning & Mountain Belts
            _proTectonicsFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proTectonicsFold, "🌋 Тектонічні плити та хребти (Tectonics)");
            if (_proTectonicsFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_tectonicSettingsProp, true);
                if (GUILayout.Button("🔄 Скинути тектоніку"))
                {
                    config.TectonicSettings = TectonicSettings.Default;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 5. Non-Linear Curves & Terraces
            _proCurvesFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proCurvesFold, "📈 Криві висот та тераси (Height Curves)");
            if (_proCurvesFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_heightCurveSettingsProp, true);
                if (GUILayout.Button("🔄 Скинути криві (Linear)"))
                {
                    config.HeightCurveSettings = HeightCurveSettings.Default;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 6. Hydrology: Ocean, River Graph & Lakes
            _proHydrologyFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proHydrologyFold, "🌊 Гідрологія: Океан, Векторні Річки та Озера");
            if (_proHydrologyFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_waterSettingsProp, true);
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_hydrologySettingsProp, new GUIContent("Векторна мережа річок (River Graph)"), true);
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_riverSettingsProp, new GUIContent("Процедурна маска (Legacy River Mask)"), true);
                if (GUILayout.Button("🔄 Скинути воду та гідрологію"))
                {
                    config.WaterSettings = WaterSettings.Default;
                    config.HydrologySettings = HydrologySettings.Default;
                    config.RiverSettings = RiverSettings.Default;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 6. Boundary Falloff
            _proFalloffFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proFalloffFold, "🏝️ Спадання меж (Falloff Mask)");
            if (_proFalloffFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_falloffSettingsProp, true);
                if (GUILayout.Button("🔄 Скинути спадання меж (None)"))
                {
                    config.FalloffSettings = FalloffSettings.Default;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 7. LOD & Streaming
            _proLodFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proLodFold, "🌐 LOD Стрімінг та Дистанція");
            if (_proLodFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maxViewDistanceProp, new GUIContent("Дистанція видимості"));
                EditorGUILayout.PropertyField(_lodTiersProp, new GUIContent("LOD Рівні"), true);
                EditorGUILayout.PropertyField(_terrainMaterialProp, new GUIContent("Матеріал рельєфу"));
                EditorGUILayout.PropertyField(_enablePersistenceProp, new GUIContent("Кешування чанків"));
                if (GUILayout.Button("🔄 Скинути LOD-налаштування (600m / 4 tiers)"))
                {
                    config.MaxViewDistance = 600f;
                    config.LodTiers = LODInfo.CreateDefaultTiers(600f);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // 8. Preset Export
            _proExportFold = EditorGUILayout.BeginFoldoutHeaderGroup(_proExportFold, "💾 Експорт у файл пресету");
            if (_proExportFold)
            {
                EditorGUI.indentLevel++;
                if (GUILayout.Button("Зберегти як новий Preset.asset", GUILayout.Height(26)))
                {
                    ExportPresetAsset(config);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion

        #region Helper Methods & Preset Implementations

        private void DrawGlobalResetButton(TerrainDataConfig config)
        {
            GUI.backgroundColor = new Color(0.95f, 0.4f, 0.4f);
            if (GUILayout.Button("🔄 Скинути ВСІ налаштування до заводських (Factory Reset)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Скидання налаштувань",
                    "Ви впевнені, що хочете скинути ВСІ параметри конфігурації до стандартних заводських значень?",
                    "Так, скинути все",
                    "Скасувати"))
                {
                    Undo.RecordObject(config, "Factory Reset TerrainConfig");
                    config.ResetToDefaults();
                    EditorUtility.SetDirty(config);
                    RegenerateActiveSceneTerrain();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DrawHeightGradientBar(TerrainDataConfig config)
        {
            if (config.Regions == null || config.Regions.Length == 0) return;

            EditorGUILayout.LabelField("Візуальна смуга розподілу висот (0.0 → 1.0):", EditorStyles.miniBoldLabel);
            Rect barRect = GUILayoutUtility.GetRect(18, 20, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, new Color(0.12f, 0.12f, 0.12f));

            float prevThreshold = 0f;
            for (int i = 0; i < config.Regions.Length; i++)
            {
                var region = config.Regions[i];
                float currentThreshold = Mathf.Clamp01(region.HeightThreshold);
                if (currentThreshold < prevThreshold) currentThreshold = prevThreshold;

                float startX = barRect.x + prevThreshold * barRect.width;
                float width = (currentThreshold - prevThreshold) * barRect.width;

                Rect segmentRect = new Rect(startX, barRect.y + 1, width, barRect.height - 2);
                EditorGUI.DrawRect(segmentRect, region.ColorTint);

                prevThreshold = currentThreshold;
            }
        }

        private static void RegenerateActiveSceneTerrain()
        {
            TerrainGenerator generator = UnityEngine.Object.FindAnyObjectByType<TerrainGenerator>();
            if (generator != null)
            {
                generator.Regenerate();
            }
        }

        private void ApplyAlpinePreset(TerrainDataConfig config)
        {
            Undo.RecordObject(config, "Apply Alpine Peaks Preset");
            config.ChunkSize = 240;
            config.ChunkResolution = 120;
            config.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.RidgedMultifractal,
                Seed = UnityEngine.Random.Range(1, 99999),
                Scale = 160f,
                Octaves = 5,
                Persistence = 0.5f,
                Lacunarity = 2.1f,
                HeightMultiplier = 80f,
                Offset = Vector2.zero
            };
            config.MacroSettings = new MacroMaskSettings
            {
                Enabled = true,
                Seed = 777,
                Scale = 600f,
                MountainAmplification = 2.2f,
                ValleyDamping = 0.2f,
                PowerExponent = 1.5f
            };
            config.TectonicSettings = new TectonicSettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                PlateCount = 12,
                PlateScale = 1200f,
                MountainUplift = 110f,
                RiftDepth = 35f,
                BoundaryInfluenceWidth = 320f,
                RidgeSharpness = 1.7f,
                FaultNoiseWarp = 0.35f
            };
            config.HydrologySettings = new HydrologySettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                SourceCount = 24,
                MinSourceElevationRatio = 0.5f,
                BaseRiverWidth = 10f,
                WidthGrowthFactor = 1.7f,
                BaseCarveDepth = 16f,
                BankSmoothness = 0.45f,
                MeanderIntensity = 0.35f,
                LakeMinDepthThreshold = 8f
            };
            config.FalloffSettings = FalloffSettings.Default;
            config.WaterSettings = WaterSettings.Default;
            config.RiverSettings = RiverSettings.Default;
            config.HeightCurveSettings = HeightCurveSettings.Default;
            config.Regions = TerrainRegion.CreateDefaultRegions();
            config.Validate();
            EditorUtility.SetDirty(config);
            RegenerateActiveSceneTerrain();
        }

        private void ApplyArchipelagoPreset(TerrainDataConfig config)
        {
            Undo.RecordObject(config, "Apply Archipelago Preset");
            config.ChunkSize = 240;
            config.ChunkResolution = 120;
            config.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.Billow,
                Seed = UnityEngine.Random.Range(1, 99999),
                Scale = 150f,
                Octaves = 4,
                Persistence = 0.45f,
                Lacunarity = 2.0f,
                HeightMultiplier = 40f,
                Offset = Vector2.zero
            };
            config.MacroSettings = MacroMaskSettings.Default;
            config.TectonicSettings = new TectonicSettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                PlateCount = 16,
                PlateScale = 800f,
                MountainUplift = 50f,
                RiftDepth = 40f,
                BoundaryInfluenceWidth = 200f,
                RidgeSharpness = 1.4f,
                FaultNoiseWarp = 0.2f
            };
            config.HydrologySettings = new HydrologySettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                SourceCount = 10,
                MinSourceElevationRatio = 0.6f,
                BaseRiverWidth = 8f,
                WidthGrowthFactor = 1.4f,
                BaseCarveDepth = 10f,
                BankSmoothness = 0.4f,
                MeanderIntensity = 0.25f,
                LakeMinDepthThreshold = 6f
            };
            config.FalloffSettings = new FalloffSettings
            {
                Mode = FalloffMode.Circular,
                FalloffStartRadius = 120f,
                FalloffEndRadius = 320f,
                PowerExponent = 2.0f
            };
            config.WaterSettings = new WaterSettings
            {
                Enabled = true,
                SeaLevel = 12f,
                OceanFloorDepth = 10f,
                ShorelineSmoothness = 1.2f
            };
            config.RiverSettings = RiverSettings.Default;
            config.HeightCurveSettings = HeightCurveSettings.Default;
            config.Regions = TerrainRegion.CreateTropicalRegions();
            config.Validate();
            EditorUtility.SetDirty(config);
            RegenerateActiveSceneTerrain();
        }

        private void ApplyRollingPlainsPreset(TerrainDataConfig config)
        {
            Undo.RecordObject(config, "Apply Rolling Plains Preset");
            config.ChunkSize = 240;
            config.ChunkResolution = 120;
            config.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.PerlinFbm,
                Seed = UnityEngine.Random.Range(1, 99999),
                Scale = 200f,
                Octaves = 3,
                Persistence = 0.35f,
                Lacunarity = 2.0f,
                HeightMultiplier = 25f,
                Offset = Vector2.zero
            };
            config.MacroSettings = MacroMaskSettings.Default;
            config.TectonicSettings = new TectonicSettings
            {
                Enabled = false,
                Seed = 42,
                PlateCount = 8,
                PlateScale = 1500f,
                MountainUplift = 20f,
                RiftDepth = 10f,
                BoundaryInfluenceWidth = 200f,
                RidgeSharpness = 1.0f,
                FaultNoiseWarp = 0.1f
            };
            config.HydrologySettings = new HydrologySettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                SourceCount = 12,
                MinSourceElevationRatio = 0.4f,
                BaseRiverWidth = 14f,
                WidthGrowthFactor = 1.6f,
                BaseCarveDepth = 8f,
                BankSmoothness = 0.6f,
                MeanderIntensity = 0.5f,
                LakeMinDepthThreshold = 5f
            };
            config.FalloffSettings = FalloffSettings.Default;
            config.WaterSettings = WaterSettings.Default;
            config.RiverSettings = RiverSettings.Default;
            config.HeightCurveSettings = HeightCurveSettings.Default;
            config.Regions = TerrainRegion.CreateDefaultRegions();
            config.Validate();
            EditorUtility.SetDirty(config);
            RegenerateActiveSceneTerrain();
        }

        private void ApplyCanyonPreset(TerrainDataConfig config)
        {
            Undo.RecordObject(config, "Apply Desert Canyons Preset");
            config.ChunkSize = 240;
            config.ChunkResolution = 120;
            config.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.RidgedMultifractal,
                Seed = UnityEngine.Random.Range(1, 99999),
                Scale = 150f,
                Octaves = 4,
                Persistence = 0.48f,
                Lacunarity = 2.0f,
                HeightMultiplier = 50f,
                Offset = Vector2.zero
            };
            config.MacroSettings = MacroMaskSettings.Default;
            config.TectonicSettings = new TectonicSettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                PlateCount = 10,
                PlateScale = 1000f,
                MountainUplift = 40f,
                RiftDepth = 50f,
                BoundaryInfluenceWidth = 250f,
                RidgeSharpness = 2.2f,
                FaultNoiseWarp = 0.4f
            };
            config.HydrologySettings = new HydrologySettings
            {
                Enabled = true,
                Seed = UnityEngine.Random.Range(1, 99999),
                SourceCount = 16,
                MinSourceElevationRatio = 0.5f,
                BaseRiverWidth = 18f,
                WidthGrowthFactor = 1.8f,
                BaseCarveDepth = 26f,
                BankSmoothness = 0.35f,
                MeanderIntensity = 0.6f,
                LakeMinDepthThreshold = 10f
            };
            config.FalloffSettings = FalloffSettings.Default;
            config.WaterSettings = WaterSettings.Default;
            config.RiverSettings = new RiverSettings
            {
                Enabled = false,
                Seed = 1234,
                Frequency = 0.007f,
                CarveDepth = 20f,
                RiverbedWidth = 30f,
                BankSmoothness = 0.4f
            };
            config.HeightCurveSettings = new HeightCurveSettings
            {
                UseCurve = true,
                ElevationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                PowerExponent = 1.0f,
                TerraceSteps = 5
            };
            config.Regions = TerrainRegion.CreateDesertRegions();
            config.Validate();
            EditorUtility.SetDirty(config);
            RegenerateActiveSceneTerrain();
        }

        private void ApplyEndlessPlainsPreset(TerrainDataConfig config)
        {
            Undo.RecordObject(config, "Apply Endless Plains Preset");
            config.ChunkSize = 240;
            config.ChunkResolution = 120;
            config.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.PerlinFbm,
                Seed = UnityEngine.Random.Range(1, 99999),
                Scale = 280f,
                Octaves = 2,
                Persistence = 0.25f,
                Lacunarity = 2.0f,
                HeightMultiplier = 15f,
                Offset = Vector2.zero
            };
            config.MacroSettings = MacroMaskSettings.Default;
            config.FalloffSettings = FalloffSettings.Default;
            config.WaterSettings = WaterSettings.Default;
            config.RiverSettings = RiverSettings.Default;
            config.HeightCurveSettings = HeightCurveSettings.Default;
            config.Regions = TerrainRegion.CreateAutumnRegions();
            config.Validate();
            EditorUtility.SetDirty(config);
            RegenerateActiveSceneTerrain();
        }

        private static void ExportPresetAsset(TerrainDataConfig config)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Зберегти пресет ландшафту",
                "NewTerrainPreset.asset",
                "asset",
                "Вкажіть шлях для збереження пресету.");

            if (!string.IsNullOrEmpty(path))
            {
                TerrainPreset preset = CreateInstance<TerrainPreset>();
                preset.PresetName = config.name;
                preset.ChunkSize = config.ChunkSize;
                preset.ChunkResolution = config.ChunkResolution;
                preset.NoiseSettings = config.NoiseSettings;
                preset.MacroSettings = config.MacroSettings;
                preset.HeightCurveSettings = config.HeightCurveSettings;
                preset.FalloffSettings = config.FalloffSettings;
                preset.WaterSettings = config.WaterSettings;
                preset.RiverSettings = config.RiverSettings;
                preset.Regions = config.Regions;
                preset.MaxViewDistance = config.MaxViewDistance;

                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = preset;
            }
        }

        #endregion
    }
}
