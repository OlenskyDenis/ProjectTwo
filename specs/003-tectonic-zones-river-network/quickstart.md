# Quickstart Validation Guide: Macro-Tectonic Zoning & Hydrological River Graph System

**Feature**: `003-tectonic-zones-river-network`
**Date**: 2026-08-20

---

## 1. Prerequisites
- Unity 2022.3 LTS or newer.
- Package dependencies: `com.unity.collections`, `com.unity.mathematics`, `com.unity.burst`.

---

## 2. Automated Test Execution

Run the NUnit test suites verifying tectonic partitioning, continuous ridge continuity, hydrological river graph pathfinding, and chunk seam alignment:

```powershell
# Run Unit and Integration Tests via Unity CLI (or inside Unity Test Runner window)
"C:\Program Files\Unity\Hub\Editor\2022.3.x\Editor\Unity.exe" -batchmode -runTests -projectPath "E:\ProjectTwo\ProjectTwoUnity" -testPlatform EditMode -testResults "test-results.xml"
```

### Key Test Scenarios:
1. `TectonicPartition_GeneratesContinuousBoundaries_NoGaps()`: Verifies that Voronoi plate boundaries create contiguous line networks without isolated disconnected segments.
2. `ConvergentBoundary_ProducesContinuousMountainChain()`: Samples height along convergent fault lines and verifies elevation $> 80\%$ of peak height across contiguous chunks.
3. `RiverPathfinding_ReachesTerminalOcean_StrictlyDescending()`: Traces 50 random river sources and verifies that every river terminates in water and has monotonically non-increasing water surface elevation ($h_{i+1} \le h_i$).
4. `RiverCarve_MaintainsSeamFreeChunkBorders()`: Compares adjacent chunk edge vertices along carved riverbeds to ensure height difference $< 0.001$.

---

## 3. Interactive Editor Validation

1. Open the project in Unity Editor.
2. Select or create a `TerrainConfig` asset in `Assets/Settings/Terrain/`.
3. In the Inspector:
   - Navigate to the **Tectonics** foldout. Toggle **Enable Tectonics**. Adjust `Plate Count` (e.g., 16) and `Mountain Uplift` (e.g., 80m).
   - In Scene View, enable **Show Tectonic Boundaries** Gizmos. Observe colored boundary lines (Red = Convergent, Blue = Divergent, Green = Transform).
   - Navigate to the **Hydrology & Rivers** foldout. Toggle **Enable River Graph**. Set `Source Count` (e.g., 20) and `Base Carve Depth` (e.g., 15m).
   - Observe cyan river splines connecting mountain sources down to blue ocean water.
4. Click **Generate / Refresh Preview**. Observe mountain ranges formed along fault lines with carved river valleys and continuous water ribbon meshes.
