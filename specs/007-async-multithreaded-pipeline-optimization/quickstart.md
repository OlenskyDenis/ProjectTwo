# Quickstart Validation Guide: Asynchronous Multithreaded Pipeline

**Feature**: `007-async-multithreaded-pipeline-optimization`
**Date**: 2026-08-22

## Prerequisites
- Unity 6 (URP) Editor
- ProjectTwo solution opened

## Validation Scenarios

### Scenario 1: Automated Contract Reflection Test Suite
Verify that all core interfaces (`ITerrainShaper`, `HeightMapBuilder`) use the clean `TerrainShaperContext` without loose parameter lists.

**Run Command**:
Execute EditMode tests in Unity Test Runner:
`ProjectTwo.Terrain.Tests.EditMode.ContractReflectionTests`

**Expected Outcome**:
- All reflection tests pass.
- `ITerrainShaper.CalculateElevation` has 3 parameters: `(float worldX, float worldZ, in TerrainShaperContext context)`.
- `ITerrainShaper.GenerateHeightMap` has 6 parameters: `(float startX, float startZ, float size, int resolution, in TerrainShaperContext context, float[,] outputBuffer)`.

---

### Scenario 2: Off-Thread Payload Generation Test
Verify that background worker tasks compute visual `TerrainMeshData`, collision `TerrainMeshData`, and `RiverWaterMeshData` off the Main Thread.

**Run Command**:
`ProjectTwo.Terrain.Tests.EditMode.DomainDecouplingTests`

**Expected Outcome**:
- `TerrainMeshData` and `RiverWaterMeshData` are fully populated and non-null prior to any GameObject instantiation.

---

### Scenario 3: Realtime 60+ FPS Streaming & Frame Budget Validation
Verify that spawning a 36-chunk world at PlayMode start produces zero frame drops below 60 FPS and respects the $\le 2.0\text{ms}$ Main Thread activation budget.

**Steps**:
1. Open `SampleScene.unity`.
2. Press Play.
3. Observe FPS counter overlay (F3).
4. Verify initial frame rate stays $\ge 60\text{ FPS}$ with zero stutters.
