namespace ProjectTwo.Terrain.Core.Models
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Defines resolution reduction rules for distance-based Level of Detail tiers.
    /// </summary>
    [Serializable]
    public struct LODInfo
    {
        [Tooltip("LOD Index (0 = highest detail).")]
        public int LodIndex;

        [Tooltip("Maximum distance from viewer to display this LOD level.")]
        public float VisibleDistanceThreshold;

        [Tooltip("Vertex increment step factor (1 = full detail, 2 = half resolution, 4 = quarter).")]
        [Range(1, 12)]
        public int MeshResolutionStep;

        [Tooltip("Whether physical collision mesh should be generated and enabled for this LOD.")]
        public bool HasCollider;

        public LODInfo(int lodIndex, float visibleDistanceThreshold, int meshResolutionStep, bool hasCollider)
        {
            LodIndex = lodIndex;
            VisibleDistanceThreshold = visibleDistanceThreshold;
            MeshResolutionStep = meshResolutionStep < 1 ? 1 : meshResolutionStep;
            HasCollider = hasCollider;
        }

        public static LODInfo[] CreateDefaultTiers(float maxViewDistance = 600f)
        {
            return new[]
            {
                new LODInfo(0, maxViewDistance * 0.25f, 1, true),
                new LODInfo(1, maxViewDistance * 0.50f, 2, false),
                new LODInfo(2, maxViewDistance * 0.75f, 4, false),
                new LODInfo(3, maxViewDistance, 6, false)
            };
        }
    }
}
