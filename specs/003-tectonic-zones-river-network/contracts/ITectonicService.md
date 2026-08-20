# Contract: ITectonicService & ITectonicShaper

```csharp
namespace ProjectTwo.Terrain.Core.Contracts
{
    using ProjectTwo.Terrain.Core.Models;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Thread-safe service for evaluating global tectonic macro-plates and boundary uplift.
    /// </summary>
    public interface ITectonicService
    {
        /// <summary>
        /// Generates or samples tectonic plates and boundary lines for a given seed and scale.
        /// </summary>
        void GenerateTectonicPartition(
            TectonicSettings settings,
            out NativeList<TectonicPlate> plates,
            out NativeList<TectonicBoundary> boundaries,
            Allocator allocator);

        /// <summary>
        /// Calculates the tectonic height modifier (uplift or rift depression) at world coordinates (x, z).
        /// </summary>
        float SampleTectonicUplift(
            float worldX,
            float worldZ,
            TectonicSettings settings,
            in NativeArray<TectonicBoundary> boundaries);
    }
}
```
