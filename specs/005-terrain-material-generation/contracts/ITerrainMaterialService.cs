namespace ProjectTwo.Terrain.Runtime.Materials
{
    using System;
    using UnityEngine;
    using ProjectTwo.Terrain.Runtime.Config;

    /// <summary>
    /// Service contract responsible for generating, configuring, caching, and updating terrain and water materials.
    /// </summary>
    public interface ITerrainMaterialService : IDisposable
    {
        /// <summary>
        /// Retrieves or creates a shared Material instance configured according to the provided terrain visual profile.
        /// </summary>
        /// <param name="profile">The visual profile describing shader and surface settings.</param>
        /// <returns>A valid, ready-to-render Material instance.</returns>
        Material GetOrCreateTerrainMaterial(TerrainVisualProfileSO profile);

        /// <summary>
        /// Retrieves or creates a shared Material instance configured according to the provided water visual profile.
        /// </summary>
        /// <param name="waterProfile">The water visual profile describing colors, shader, and flow parameters.</param>
        /// <returns>A valid, ready-to-render water Material instance.</returns>
        Material GetOrCreateWaterMaterial(WaterVisualProfileSO waterProfile);

        /// <summary>
        /// Updates shader properties on active cached materials in response to live profile modifications without recreating material instances.
        /// </summary>
        /// <param name="profile">The modified terrain profile.</param>
        void UpdateTerrainMaterialProperties(TerrainVisualProfileSO profile);

        /// <summary>
        /// Updates shader properties on active cached water materials in response to live water profile modifications.
        /// </summary>
        /// <param name="waterProfile">The modified water profile.</param>
        void UpdateWaterMaterialProperties(WaterVisualProfileSO waterProfile);

        /// <summary>
        /// Clears and disposes all generated material instances.
        /// </summary>
        void ClearCache();
    }
}
