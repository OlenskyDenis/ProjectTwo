# Contract: `ITerrainPresetService`

**Namespace**: `ProjectTwo.Terrain.Core.Contracts`  
**Purpose**: Contract for managing, applying, exporting, and serializing terrain archetype presets.

---

## Interface Definition

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using System.Collections.Generic;
    using ProjectTwo.Terrain.Presentation.Config;

    /// <summary>
    /// Service for querying, applying, and exporting terrain presets.
    /// </summary>
    public interface ITerrainPresetService
    {
        /// <summary>
        /// Retrieves all available built-in and user-saved presets.
        /// </summary>
        IReadOnlyList<TerrainPreset> GetAvailablePresets();

        /// <summary>
        /// Applies a preset configuration onto a target TerrainDataConfig asset.
        /// </summary>
        /// <param name="targetConfig">Configuration to mutate.</param>
        /// <param name="preset">Preset archetype to apply.</param>
        void ApplyPreset(TerrainDataConfig targetConfig, TerrainPreset preset);

        /// <summary>
        /// Exports the current TerrainDataConfig state into a new reusable preset asset.
        /// </summary>
        /// <param name="sourceConfig">Source configuration to capture.</param>
        /// <param name="presetName">Name of the preset.</param>
        /// <param name="savePath">Project-relative asset path.</param>
        /// <returns>Created TerrainPreset asset.</returns>
        TerrainPreset SaveAsPreset(TerrainDataConfig sourceConfig, string presetName, string savePath);
    }
}
```
