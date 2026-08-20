# Quickstart Validation Guide: TerrainConfig Interface & Generation Expansion

**Feature Branch**: `002-terrain-config-ui-expansion`  
**Date**: 2026-08-20  
**Spec**: [spec.md](spec.md)

---

## 1. Prerequisites

- Unity 6 Editor (or Unity 2022 LTS+) opened to `ProjectTwoUnity`.
- Scene `TerrainDemo` open or a new GameObject with `TerrainGenerator` component attached.
- `TerrainConfig.asset` selected in Project view.

---

## 2. Validation Scenarios

### Scenario 1: Enhanced Inspector Ergonomics & Validation Snapping
1. Select `Assets/TerrainConfig.asset` in Unity Project window.
2. Verify all parameter sections are neatly categorized into collapsible foldouts (`Grid & Chunk Metrics`, `Macro & Mountain Masks`, `Noise & Topography`, `Rivers & Water Bodies`, `Biome Texture Layers`, `LOD & Streaming`, `Preset Library`).
3. Drag the `Chunk Resolution` slider to an arbitrary number (e.g. 57) and release.
4. **Expected**: Resolution automatically snaps to the nearest valid multiple of 12 (60) and an info box displays: `"✓ Seamless Grid Guaranteed (Divisible by LODs 1, 2, 4, 6)"`.

### Scenario 2: Noise Topography Modes & Macro Mountain Masking
1. In the `Noise & Topography` section, switch `Noise Type` from `PerlinFbm` to `RidgedMultifractal`.
2. Observe Scene View: High mountain ridges form sharp rocky crests.
3. Enable `Macro Mountain Mask`, increase `Mountain Amplification` to `3.0`, and set `Valley Damping` to `0.2`.
4. **Expected**: Valleys become flat and smooth, while mountain clusters elevate into towering ranges without raising the lowland ground.

### Scenario 3: Procedural River Carving & Sea Level Basins
1. Enable `Water & Sea Level` and set `Sea Level` to `15.0`.
2. Enable `Procedural Rivers`, adjust `Carve Depth` to `10.0` and `Riverbed Width` to `8.0`.
3. **Expected**: River channels carve smooth depression paths into the terrain down to sea level across chunk borders without vertex seams or visual gaps.

### Scenario 4: Hybrid Biome Texturing & Slope Blending
1. Expand the `Biome Texture Layers` section.
2. In the `Mountain` layer, set `Slope Threshold` to `40°` and assign a rock normal and albedo texture.
3. **Expected**: Flat surfaces retain grass texture, while steep cliff walls automatically blend into rock texture.

### Scenario 5: Presets & Live Slider Dragging Safety
1. Open the `Preset Library` section and click on `Alpine Mountains`.
2. **Expected**: All parameters update in one click and the 3D scene immediately renders the alpine landscape.
3. Rapidly drag the `Noise Scale` slider back and forth for 15 seconds.
4. **Expected**: Editor remains responsive at $\ge 60\text{ FPS}$ with draft low-res preview during motion, settling cleanly into high-res mesh upon release with 0 memory leaks.

---

## 3. Automated Test Verification

Run all domain tests in Unity Test Runner:
```powershell
# Open Test Runner in Unity Editor: Window -> General -> Test Runner
# Run all EditMode unit tests for ProjectTwo.Terrain.Tests
```
All unit tests in `ProjectTwo.Terrain.Tests` must pass cleanly ($100\%$).
