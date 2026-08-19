# Quickstart & Validation Guide: Procedural Terrain Generation via Perlin Noise

**Feature**: Procedural Terrain Generation via Perlin Noise (`001-terrain-generation`)  
**Status**: Complete  
**Date**: 2026-08-19

---

## 1. Prerequisites & Environment Setup

- Unity Editor version: **Unity 6 (6000.5.5f1)**
- Project root: `ProjectTwoUnity/`
- Target Assembly: `ProjectTwo.Terrain.Runtime` (and `ProjectTwo.Terrain.Editor` for custom inspectors)
- Automated Test Runner: Unity Test Runner (EditMode / PlayMode) / NUnit

---

## 2. Automated Test Execution

### Run Pure C# Unit Tests (EditMode)
To verify core noise math, determinism, mesh LOD construction, and serialization without starting Play mode:

1. Open Unity Test Runner: `Window` → `General` → `Test Runner`.
2. Select the **EditMode** tab.
3. Run the test fixture: `ProjectTwo.Terrain.Tests.Core`.
4. **Expected Result**: All tests pass in $< 2$ seconds:
   - `PerlinNoiseGeneratorTests.Noise_IsDeterministic_ForSameSeed`
   - `PerlinNoiseGeneratorTests.Noise_ClampsToValidRange`
   - `TerrainMeshBuilderTests.LOD_ReducesVertexCount_Correctly`
   - `TerrainMeshBuilderTests.MeshEdges_AlignSeamlessly_AcrossLODs`
   - `ChunkStorageTests.SaveAndLoad_PreservesElevationData`

---

## 3. Visual In-Editor Validation (Quick Designer Workflow)

1. **Create Configuration Asset**:
   - In Unity Project view, right-click in `Assets/Settings/` → `Create` → `Terrain` → `Configuration Preset`.
   - Name it `DefaultTerrainConfig.asset`.
   - Configure default parameters:
     - `ChunkSize`: `240`
     - `ChunkResolution`: `120`
     - `Noise Scale`: `50`
     - `Octaves`: `4`
     - `Persistence`: `0.5`
     - `Lacunarity`: `2.0`
     - `HeightMultiplier`: `30`
     - `Regions`: Add Water (`0.3`, Blue), Sand (`0.35`, Yellow), Grass (`0.7`, Green), Rock (`0.9`, Grey), Snow (`1.0`, White).
2. **Add Generator to Scene**:
   - Create an empty GameObject in the active Scene, name it `TerrainManager`.
   - Add the `TerrainGenerator` component to it.
   - Assign `DefaultTerrainConfig.asset` to the `Configuration` field on `TerrainGenerator`.
3. **Test Inspector Generation**:
   - In the `TerrainGenerator` Inspector, click **Generate Terrain Preview**.
   - **Expected Result**: A 3D continuous procedural landscape mesh immediately appears in the Scene view with colored elevation regions.
   - Enable **Auto Update** checkbox, then drag the **Noise Scale** slider.
   - **Expected Result**: Scene view terrain regenerates smoothly in real-time as the slider moves.

---

## 4. Infinite Streaming & Runtime Validation (Quick Play Mode Workflow)

1. **Assign Viewer Target**:
   - Ensure the Main Camera or Player GameObject has a Transform and is assigned as the `Viewer` in `TerrainGenerator`.
2. **Enter Play Mode**:
   - Click the **Play** button in Unity.
   - Move the camera/player across chunk boundaries.
3. **Verify Performance & Multi-LOD Streaming**:
   - Open Unity Stats / Profiler window.
   - **Expected Result**:
     - Frame rate remains at **60+ FPS** with zero hitches or stalls during chunk transitions.
     - Nearby chunks display LOD 0 with active colliders; distant chunks display simplified LODs.
     - Out-of-range chunks are disabled and pooled without memory growth.
4. **Verify Persistence**:
   - Travel 10 chunks away, then return to coordinate `(0, 0)`.
   - **Expected Result**: The initial terrain reloads instantly from cache storage with identical geometry.

---

## 5. Downstream Integration Test (Simulating World Placers)

1. Attach a mock test script `TerrainPlacementTest` implementing `ITerrainProvider` subscription:
   ```csharp
   void Start() {
       var provider = FindAnyObjectByType<TerrainGenerator>();
       provider.OnChunkLoaded += chunk => {
           Debug.Log($"Chunk [{chunk.ChunkX}, {chunk.ChunkZ}] loaded. Height at center: {provider.GetHeight(chunk.WorldOrigin.x, chunk.WorldOrigin.z)}");
       };
   }
   ```
2. Enter Play mode and confirm event logs fire cleanly as chunks spawn.
