# Data Model: Advanced Hydrology & Continuous River Networks

## Domain Entities (ProjectTwo.Terrain.Core.Models)

### 1. RiverNodeType (Enum)
```csharp
public enum RiverNodeType
{
    Source,
    Waterfall,
    Rapids,
    LakeInflow,
    LakeOutflow,
    Confluence,
    Bifurcation,
    DeltaMouth,
    OceanMouth
}
```

### 2. RiverNode (Struct / Immutable Record)
```csharp
public readonly struct RiverNode
{
    public int Id { get; }
    public Vector3 Position { get; }
    public RiverNodeType Type { get; }
    public float Elevation { get; }
    public float FlowAccumulation { get; }
    public int StreamOrder { get; }
    public float SlopeAngle { get; }
}
```

### 3. RiverSegment (Struct / Immutable Record)
```csharp
public readonly struct RiverSegment
{
    public int Id { get; }
    public int FromNodeId { get; }
    public int ToNodeId { get; }
    public Vector3 StartPosition { get; }
    public Vector3 ControlPoint { get; }
    public Vector3 EndPosition { get; }
    public float StartWidth { get; }
    public float EndWidth { get; }
    public float FlowSpeed { get; }
    public int StreamOrder { get; }
    public bool IsWaterfall { get; }
}
```

### 4. LakeBasin (Class / Immutable Record)
```csharp
public class LakeBasin
{
    public int Id { get; }
    public Vector3 Center { get; }
    public float WaterLevel { get; }
    public float Capacity { get; }
    public List<Vector3> PerimeterPoints { get; }
    public int InflowCount { get; }
    public int? OutflowNodeId { get; }
    public bool IsTerminalLake { get; }
}
```

### 5. HydrologySettings (Struct / Serializable Settings)
```csharp
[System.Serializable]
public struct HydrologySettings
{
    public bool Enabled;
    public int Seed;
    public int SourceCount;
    public float MinSourceElevationRatio;
    public float BaseRiverWidth;
    public float WidthGrowthFactor;
    public float BaseCarveDepth;
    public float BankSmoothness;
    public float MeanderIntensity;
    public float LakeMinDepthThreshold;
    public float WaterfallStepSize;
    public float HydraulicMomentum;
    public float DeltaBranchingChance;
}
```
