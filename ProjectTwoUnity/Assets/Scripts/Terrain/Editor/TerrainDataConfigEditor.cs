namespace ProjectTwo.Terrain.Editor
{
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Presentation.Config;

    /// <summary>
    /// Custom Inspector for TerrainDataConfig ScriptableObject.
    /// Provides smart grid snapping, seamless border validation, and a reorderable biome list with color pickers.
    /// </summary>
    [CustomEditor(typeof(TerrainDataConfig))]
    public class TerrainDataConfigEditor : Editor
    {
        private SerializedProperty _chunkSizeProp;
        private SerializedProperty _chunkResolutionProp;
        private SerializedProperty _noiseSettingsProp;
        private SerializedProperty _lodTiersProp;
        private SerializedProperty _maxViewDistanceProp;
        private SerializedProperty _regionsProp;
        private SerializedProperty _terrainMaterialProp;
        private SerializedProperty _enablePersistenceProp;

        private ReorderableList _regionsList;

        private void OnEnable()
        {
            _chunkSizeProp = serializedObject.FindProperty("ChunkSize");
            _chunkResolutionProp = serializedObject.FindProperty("ChunkResolution");
            _noiseSettingsProp = serializedObject.FindProperty("NoiseSettings");
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
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Elevation Regions / Biomes (Sorted by Height)", EditorStyles.boldLabel);
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = _regionsProp.GetArrayElementAtIndex(index);
                    SerializedProperty nameProp = element.FindPropertyRelative("Name");
                    SerializedProperty thresholdProp = element.FindPropertyRelative("HeightThreshold");
                    SerializedProperty colorProp = element.FindPropertyRelative("ColorTint");

                    rect.y += 2;
                    float fieldHeight = EditorGUIUtility.singleLineHeight;
                    float spacing = 6f;

                    // Allocate widths
                    float totalWidth = rect.width;
                    float nameWidth = totalWidth * 0.32f;
                    float thresholdWidth = totalWidth * 0.38f;
                    float colorWidth = totalWidth - nameWidth - thresholdWidth - (spacing * 2);

                    Rect nameRect = new Rect(rect.x, rect.y, nameWidth, fieldHeight);
                    Rect thresholdRect = new Rect(rect.x + nameWidth + spacing, rect.y, thresholdWidth, fieldHeight);
                    Rect colorRect = new Rect(rect.x + nameWidth + thresholdWidth + (spacing * 2), rect.y, colorWidth, fieldHeight);

                    EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
                    thresholdProp.floatValue = EditorGUI.Slider(thresholdRect, thresholdProp.floatValue, 0f, 1f);
                    EditorGUI.PropertyField(colorRect, colorProp, GUIContent.none);
                },

                onAddCallback = (ReorderableList list) =>
                {
                    int newIndex = list.serializedProperty.arraySize;
                    list.serializedProperty.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(newIndex);

                    element.FindPropertyRelative("Name").stringValue = $"Region {newIndex + 1}";
                    element.FindPropertyRelative("HeightThreshold").floatValue = 1.0f;
                    element.FindPropertyRelative("ColorTint").colorValue = Color.white;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            TerrainDataConfig config = (TerrainDataConfig)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Chunk Grid Setup (Seamless Snapped)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int rawChunkSize = EditorGUILayout.IntSlider(
                new GUIContent("Chunk Size (Units)", "Physical width/length of each chunk in world units. Automatically snaps to multiples of 12."),
                _chunkSizeProp.intValue, 24, 480);

            if (EditorGUI.EndChangeCheck())
            {
                _chunkSizeProp.intValue = Mathf.RoundToInt(rawChunkSize / 12f) * 12;
            }

            EditorGUI.BeginChangeCheck();
            int rawResolution = EditorGUILayout.IntSlider(
                new GUIContent("Chunk Resolution (Segments)", "Number of segments per edge. Automatically snaps to multiples of 12 (divisible by LODs 1, 2, 4, 6) for guaranteed zero seams."),
                _chunkResolutionProp.intValue, 24, 240);

            if (EditorGUI.EndChangeCheck())
            {
                _chunkResolutionProp.intValue = Mathf.RoundToInt(rawResolution / 12f) * 12;
            }

            EditorGUILayout.HelpBox(
                $"✓ Seamless Grid Guaranteed: {config.ChunkResolution} segments = {config.ChunkResolution + 1}x{config.ChunkResolution + 1} vertices. Divisible by LOD steps (1, 2, 4, 6).",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Noise Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_noiseSettingsProp, true);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("LOD & Streaming", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_maxViewDistanceProp);
            EditorGUILayout.PropertyField(_lodTiersProp, true);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Visuals & Persistence", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_terrainMaterialProp);
            EditorGUILayout.PropertyField(_enablePersistenceProp);

            EditorGUILayout.Space(12);
            _regionsList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Default Biomes"))
            {
                Undo.RecordObject(target, "Reset Default Biomes");
                config.Regions = TerrainRegion.CreateDefaultRegions();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }

            if (GUILayout.Button("Validate & Snap Config"))
            {
                config.Validate();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
