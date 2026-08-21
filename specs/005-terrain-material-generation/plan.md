# Implementation Plan: Terrain Material Generation Module

**Branch**: `005-terrain-material-generation` | **Date**: 2026-08-21 | **Spec**: [spec.md](file:///e:/ProjectTwo/specs/005-terrain-material-generation/spec.md)

**Input**: Feature specification from `specs/005-terrain-material-generation/spec.md`

## Summary

Extract and establish a dedicated, modular **Terrain Material Generation & Provider Service** in the Presentation layer (`ProjectTwo.Terrain.Runtime.Materials`). Author visual styling parameters as reusable `ScriptableObject` profiles (`TerrainVisualProfileSO`, `WaterVisualProfileSO`), provide high-performance in-memory material caching and lifecycle management, support real-time reactive inspector updates without mesh recomputation, and decouple core domain models (`TerrainRegion`, `WaterSettings`) from engine `UnityEngine.Material` references.

## Technical Context

**Language/Version**: C# 9.0 (.NET Standard 2.1 / Unity 6000.x runtime)

**Primary Dependencies**: 
- `UnityEngine`, `UnityEngine.Rendering.Universal` (URP)
- `ProjectTwo.Terrain.Core` (domain contracts & models)
- `ProjectTwo.Terrain.Runtime` (presentation & runtime components)

**Storage**: Unity `ScriptableObject` assets (`.asset` files) for serialized profiles; in-memory cache (`IMaterialCache`) during runtime.

**Testing**: NUnit / Unity Test Framework (EditMode and PlayMode test assemblies in `ProjectTwo.Terrain.Tests`).

**Target Platform**: Desktop (Windows/macOS/Linux) and Mobile-capable via URP.

**Project Type**: Game Engine Subsystem (Unity C# Architecture).

**Performance Goals**: 
- Material resolution / retrieval < 1ms per chunk.
- 0 duplicated runtime material allocations for chunks sharing identical visual profiles.
- Zero mesh recalculation during inspector color/material tuning.

**Constraints**: 
- Memory safety: zero leaking material instances across scene transitions or domain reloads.
- Architectural boundary: 0 references to `UnityEngine.Material` in `ProjectTwo.Terrain.Core`.

**Scale/Scope**: Streaming terrain worlds with 100+ active chunks sharing unified material instances.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment | Status |
|---|---|---|
| **I. Architectural Integrity & SOLID** | Dedicated `ITerrainMaterialService` isolates material generation from mesh building (`SRP`). Profiles are open for extension (`OCP`). Abstractions decouple domain from presentation (`DIP`). | **PASS** |
| **II. Testing Standards & Test-First** | Unit tests cover material factory, caching, fallback resolution, and domain model decoupling. Integration tests verify batch chunk rendering and inspector live updates. | **PASS** |
| **III. UX Consistency** | Modular ScriptableObjects provide standardized Unity Inspector controls with tooltips, ranges, and real-time live preview feedback. | **PASS** |
| **IV. Performance & Efficiency** | Caching shared materials prevents GPU draw call proliferation and excess VRAM allocations. Zero-cost material property updates without rebuilding geometry. | **PASS** |
| **V. Simplicity & Observability** | Clear single-responsibility services with structured debug logs and error fallbacks. | **PASS** |
| **VI. Interface Integrity & Single Pipeline** | All runtime and editor chunk rendering pipelines call the single authoritative `ITerrainMaterialService`. Legacy inline fallbacks removed across the board. | **PASS** |

## Project Structure

### Documentation (this feature)

```text
specs/005-terrain-material-generation/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan
├── research.md          # Phase 0 research & technical decisions
├── data-model.md        # Phase 1 data models & class diagrams
├── quickstart.md        # Phase 1 developer validation guide
├── contracts/           # Phase 1 interface contracts
│   ├── ITerrainMaterialService.cs
│   └── IMaterialCache.cs
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
ProjectTwoUnity/Assets/Scripts/Terrain/
├── Core/
│   ├── Models/
│   │   ├── TerrainRegion.cs          # [MODIFY] Decouple Material references to abstract descriptor
│   │   ├── WaterSettings.cs          # [MODIFY] Decouple Material references to abstract descriptor
│   │   └── MaterialDescriptor.cs     # [NEW] Lightweight descriptor struct
├── Presentation/
│   ├── Config/
│   │   ├── TerrainVisualProfileSO.cs # [NEW] ScriptableObject for terrain visual styling
│   │   ├── WaterVisualProfileSO.cs   # [NEW] ScriptableObject for water visual styling
│   │   └── TerrainDataConfig.cs      # [MODIFY] Reference visual profile ScriptableObjects
│   ├── Materials/
│   │   ├── ITerrainMaterialService.cs# [NEW] Material generation contract
│   │   ├── IMaterialCache.cs         # [NEW] Material caching contract
│   │   ├── TerrainMaterialService.cs # [NEW] Material service implementation
│   │   └── TerrainMaterialCache.cs   # [NEW] In-memory lifecycle & caching implementation
│   ├── Components/
│   │   ├── TerrainGenerator.cs       # [MODIFY] Use ITerrainMaterialService for material assignment
│   │   └── TerrainChunkView.cs       # [MODIFY] Remove scattered inline fallback material instantiation
│   └── Pooling/
│       └── ChunkObjectPool.cs        # [MODIFY] Receive material from service instead of creating internal fallbacks
└── Tests/
    └── EditMode/
        ├── TerrainMaterialServiceTests.cs # [NEW] Unit tests for material generation and caching
        └── DomainDecouplingTests.cs       # [NEW] Architecture validation tests
```

**Structure Decision**: Clean presentation module addition under `Presentation/Materials` alongside existing `Config` and `Components`, with domain cleanup in `Core/Models`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| *None* | Architecture strictly satisfies Constitution gates without violations. | N/A |
