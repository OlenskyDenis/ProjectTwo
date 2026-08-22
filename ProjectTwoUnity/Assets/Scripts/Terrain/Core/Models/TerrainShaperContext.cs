namespace ProjectTwo.Terrain.Core.Models
{
    /// <summary>
    /// Immutable domain context structure encapsulating all procedural terrain generation parameters,
    /// macro masks, tectonics, curves, water bodies, and hydrology settings.
    /// Eliminates the Long Parameter List antipattern in compliance with SOLID and Constitution Principle I & VI.
    /// </summary>
    public readonly struct TerrainShaperContext
    {
        public readonly NoiseSettings Noise;
        public readonly MacroMaskSettings Macro;
        public readonly TectonicSettings Tectonics;
        public readonly TectonicBoundary[] TectonicBoundaries;
        public readonly HeightCurveSettings HeightCurve;
        public readonly WaterSettings Water;
        public readonly RiverSettings River;
        public readonly HydrologySettings Hydrology;
        public readonly RiverGraph RiverGraph;
        public readonly FalloffSettings Falloff;

        public TerrainShaperContext(
            NoiseSettings noise,
            MacroMaskSettings macro,
            TectonicSettings tectonics,
            TectonicBoundary[] tectonicBoundaries,
            HeightCurveSettings heightCurve,
            WaterSettings water,
            RiverSettings river,
            HydrologySettings hydrology,
            RiverGraph riverGraph,
            FalloffSettings falloff)
        {
            Noise = noise;
            Macro = macro;
            Tectonics = tectonics;
            TectonicBoundaries = tectonicBoundaries;
            HeightCurve = heightCurve;
            Water = water;
            River = river;
            Hydrology = hydrology;
            RiverGraph = riverGraph ?? RiverGraph.Empty;
            Falloff = falloff;
        }

        public static TerrainShaperContext CreateDefault()
        {
            return new TerrainShaperContext(
                NoiseSettings.Default,
                MacroMaskSettings.Default,
                TectonicSettings.Default,
                null,
                HeightCurveSettings.Default,
                WaterSettings.Default,
                RiverSettings.Default,
                HydrologySettings.Default,
                RiverGraph.Empty,
                FalloffSettings.Default);
        }
    }
}
