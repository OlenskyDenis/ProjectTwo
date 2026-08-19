namespace ProjectTwo.Terrain.Editor
{
    using UnityEditor;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Components;

    /// <summary>
    /// Custom Inspector for TerrainGenerator providing on-demand generation and live auto-updating in Edit mode.
    /// </summary>
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TerrainGenerator terrainGen = (TerrainGenerator)target;

            if (DrawDefaultInspector())
            {
                if (terrainGen.AutoUpdate)
                {
                    terrainGen.Regenerate();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Generate Terrain Preview", GUILayout.Height(30)))
            {
                terrainGen.Regenerate();
            }

            if (GUILayout.Button("Clear Generated Preview", GUILayout.Height(24)))
            {
                terrainGen.ClearAllChunks();
            }
        }
    }
}
