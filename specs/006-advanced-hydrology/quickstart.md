# Quickstart & Verification Guide: Advanced Hydrology & Waterfall Dynamics

## Prerequisites
- Unity 6 / .NET Standard 2.1
- Feature Branch: `006-advanced-hydrology`

## Verification Scenarios

### Scenario 1: Waterfall Surface Conformance on Alpine Slopes
1. Open Unity Scene `ProjectTwoUnity/Assets/Scenes/SampleScene.unity`.
2. Select `TerrainConfig.asset` in Inspector -> Click **«🌟 Швидкий режим»** -> Apply **«🏔️ Альпійські піки»**.
3. Inspect high-altitude river sources (>60m) descending steep slopes:
   - **Expected**: Waterfall mesh ribbon tightly adheres to the cliff face (within 0.25m) without floating boards, gaps, or inverted facets.

### Scenario 2: Lake Cascades & Saddle Spillover
1. In `TerrainConfig.asset`, set `HydrologySettings.SourceCount = 16`, `LakeMinDepthThreshold = 6m`.
2. Inspect enclosed highland valleys:
   - **Expected**: Water pools into lake basins and connects through overflow spillways into lower lakes, forming continuous multi-tier lake chains.

### Scenario 3: River Confluence & Coastal Deltas
1. Travel downstream along mountain rivers towards sea level:
   - **Expected**: Where two tributaries meet, channel width widens from ~2m to ~15m. On low-gradient coastal terrain (Slope < 5°), river branches into secondary braided delta streams.

### Automated Unit Tests
Run from terminal:
```bash
dotnet test ProjectTwoUnity/ProjectTwo.Terrain.Tests.csproj
```
- Validates 0 orphan segments, continuous stream orders, adaptive step scaling, and saddle point detection.
