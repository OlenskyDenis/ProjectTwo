namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration for non-linear elevation remapping and terrace step quantization.
    /// Used for flattening valleys, steepening mountain peaks, or creating mesa plateaus.
    /// </summary>
    [Serializable]
    public class HeightCurveSettings
    {
        [Tooltip("Enable non-linear height curve remapping.")]
        public bool UseCurve = false;

        [Tooltip("Normalized elevation remapping curve (X: input [0..1], Y: output [0..1]).")]
        public AnimationCurve ElevationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Power exponent applied to input height before evaluation (1.0 = linear).")]
        [Range(0.2f, 5f)]
        public float PowerExponent = 1.0f;

        [Tooltip("Number of plateau / terrace elevation steps (0 = smooth continuous).")]
        [Range(0, 32)]
        public int TerraceSteps = 0;

        public static HeightCurveSettings Default => new HeightCurveSettings
        {
            UseCurve = false,
            ElevationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
            PowerExponent = 1.0f,
            TerraceSteps = 0
        };

        public void Validate()
        {
            if (PowerExponent < 0.05f) PowerExponent = 0.05f;
            if (TerraceSteps < 0) TerraceSteps = 0;
            if (ElevationCurve == null || ElevationCurve.length == 0)
            {
                ElevationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
        }
    }
}
