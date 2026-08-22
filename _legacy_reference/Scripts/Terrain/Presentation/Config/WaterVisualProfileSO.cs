namespace ProjectTwo.Terrain.Presentation.Config
{
    using System;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Reusable visual profile asset defining shaders, colors, and flow parameters for rivers and water surfaces.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWaterVisualProfile", menuName = "ProjectTwo/Terrain/Water Visual Profile", order = 11)]
    public class WaterVisualProfileSO : ScriptableObject
    {
        [Header("Shader & Material")]
        [Tooltip("Base shader used for water surface rendering (defaults to ProjectTwo/WaterSimple).")]
        public Shader CustomWaterShader;

        [Tooltip("Optional direct water material override.")]
        public Material DirectWaterMaterialOverride;

        [Header("Colors & Transparency")]
        [Tooltip("Deep water color / base tint.")]
        public Color DeepWaterColor = new Color(0.12f, 0.38f, 0.68f, 0.85f);

        [Tooltip("Shallow / shoreline water color.")]
        public Color ShallowWaterColor = new Color(0.35f, 0.65f, 0.90f, 0.60f);

        [Header("Wave & Flow Dynamics")]
        [Tooltip("Water flow animation speed multiplier.")]
        [Range(0f, 5f)]
        public float FlowSpeed = 1.0f;

        [Tooltip("Wave height or normal perturbation strength.")]
        [Range(0f, 1f)]
        public float WaveHeight = 0.2f;

        /// <summary>
        /// Event fired whenever water profile parameters are modified in editor or runtime.
        /// </summary>
        public event Action OnProfileChanged;

        public void NotifyProfileChanged()
        {
            OnProfileChanged?.Invoke();
        }

        private void OnValidate()
        {
            NotifyProfileChanged();
        }

        /// <summary>
        /// Returns an abstract descriptor representing this water profile.
        /// </summary>
        public MaterialDescriptor GetDescriptor()
        {
            int hash = 17;
            hash = hash * 31 + (CustomWaterShader != null ? CustomWaterShader.name.GetHashCode() : 0);
            hash = hash * 31 + DeepWaterColor.GetHashCode();
            hash = hash * 31 + ShallowWaterColor.GetHashCode();
            hash = hash * 31 + FlowSpeed.GetHashCode();
            return new MaterialDescriptor(name, name, hash);
        }

        /// <summary>
        /// Creates a default baseline water visual profile.
        /// </summary>
        public static WaterVisualProfileSO CreateDefaultProfile()
        {
            WaterVisualProfileSO profile = CreateInstance<WaterVisualProfileSO>();
            profile.name = "DefaultWaterVisualProfile";
            profile.DeepWaterColor = new Color(0.12f, 0.38f, 0.68f, 0.85f);
            profile.ShallowWaterColor = new Color(0.35f, 0.65f, 0.90f, 0.60f);
            profile.FlowSpeed = 1.0f;
            profile.WaveHeight = 0.2f;
            return profile;
        }
    }
}
