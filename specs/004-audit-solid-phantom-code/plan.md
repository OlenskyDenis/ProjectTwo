# Implementation Plan: Codebase SOLID Audit and Phantom Code Elimination

**Branch**: `004-audit-solid-phantom-code` | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-audit-solid-phantom-code/spec.md`

## Summary

Perform a comprehensive SOLID architectural audit across the `ProjectTwo.Terrain` codebase, identify and eliminate phantom/dead code elements (specifically legacy overloads in `ITerrainShaper` and `HeightMapBuilder` violating Constitution Principle VI), refactor components to adhere to SOLID principles (SRP, DIP, ISP), and establish an automated **Contract Reflection Test Suite** in `ProjectTwo.Terrain.Tests` to permanently freeze domain contracts against stale signatures in CI.

## Technical Context

**Language/Version**: C# 9.0 (.NET Standard 2.1)

**Primary Dependencies**: Unity Engine 6000.5.5f1, UnityEngine.CoreModule, UnityEngine.UIElements, NUnit Framework

**Storage**: In-memory caching (`MemoryChunkStorage`), Unity ScriptableObject configuration presets (`TerrainDataConfig`, `TerrainPreset`)

**Testing**: NUnit (Unity Test Runner / `dotnet test` on `ProjectTwo.Terrain.Tests.csproj`)

**Target Platform**: Unity Standalone Windows 64-bit / Cross-platform C# runtime

**Project Type**: Modular Unity Framework & Procedural Generation Library (Core, Runtime, Editor, Tests)

**Performance Goals**: Zero overhead from reflection tests during runtime (tests run in EditMode test suite), 60+ FPS streaming in runtime generation

**Constraints**: 0% regression on existing unit/integration test suites, 0 compiler warnings, 0 obsolete method overloads in domain contracts

**Scale/Scope**: 4 primary assemblies (`ProjectTwo.Terrain.Core`, `ProjectTwo.Terrain.Runtime`, `ProjectTwo.Terrain.Editor`, `ProjectTwo.Terrain.Tests`), ~45 source files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I: Architectural Integrity & SOLID Design**: PASS. (Refactoring eliminates dual-purpose `HeightMapBuilder` and enforces pure constructor injection DIP).
- **Principle II: Comprehensive Testing Standards & Test-First**: PASS. (Adds automated Contract Reflection Test suite to CI gate).
- **Principle III: User Experience Consistency & Accessibility**: PASS. (Editor inspector parameter exposure verified without broken or phantom fields).
- **Principle IV: Performance & Resource Efficiency**: PASS. (Single authoritative pipeline eliminates redundant calculation overhead).
- **Principle V: Maintainability, Simplicity & Observability**: PASS. (Eliminates dead legacy branches and simplifies cognitive model).
- **Principle VI: Interface Integrity & Single Pipeline Governance**: PASS. (Eliminates all stale 8-parameter and 11-parameter overloads in `ITerrainShaper` and `HeightMapBuilder`).

## Project Structure

### Documentation (this feature)

```text
specs/004-audit-solid-phantom-code/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── TerrainCoreContracts.md
├── checklists/
│   └── requirements.md
├── spec.md              # Active feature specification
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository layout)

```text
ProjectTwoUnity/Assets/Scripts/Terrain/
├── Core/
│   ├── Contracts/
│   │   ├── ITerrainProvider.cs
│   │   ├── ITerrainShaper.cs        # Authoritative 12-parameter pipeline
│   │   ├── ITectonicService.cs
│   │   ├── IHydrologyService.cs
│   │   ├── IRiverMeshBuilder.cs
│   │   ├── INoiseGenerator.cs
│   │   └── IChunkStorage.cs
│   ├── Models/
│   │   ├── ChunkCoordinate.cs
│   │   ├── HeightMap.cs
│   │   ├── NoiseSettings.cs
│   │   ├── MacroMaskSettings.cs
│   │   ├── TectonicSettings.cs
│   │   ├── HydrologySettings.cs
│   │   ├── HeightCurveSettings.cs
│   │   ├── WaterSettings.cs
│   │   ├── RiverSettings.cs
│   │   ├── FalloffSettings.cs
│   │   └── TerrainPreset.cs
│   └── Services/
│       ├── ProceduralTerrainShaper.cs  # Unified single pipeline implementation
│       ├── HeightMapBuilder.cs         # Refactored for pure DIP & compound maps
│       ├── TectonicService.cs
│       ├── HydrologyService.cs
│       ├── RiverMeshBuilder.cs
│       ├── TerrainMeshBuilder.cs
│       └── PerlinNoiseGenerator.cs
├── Presentation/
│   ├── Components/
│   │   ├── TerrainGenerator.cs
│   │   ├── TerrainChunkView.cs
│   │   ├── FPSCounter.cs
│   │   └── FreeFlyCameraController.cs
│   └── Config/
│       └── TerrainDataConfig.cs
├── Editor/
│   ├── TerrainDataConfigEditor.cs
│   └── TerrainGeneratorEditor.cs
└── Tests/
    ├── EditMode/
    │   ├── ContractReflectionTests.cs   # NEW: CI gate for zero stale overloads
    │   ├── ProceduralTerrainShaperTests.cs
    │   ├── HeightMapBuilderTests.cs
    │   ├── TectonicServiceTests.cs
    │   ├── HydrologyServiceTests.cs
    │   └── TerrainMeshBuilderTests.cs
    └── Integration/
        ├── TectonicRidgeContinuityTests.cs
        └── RiverChunkSeamTests.cs
```

**Structure Decision**: Unity C# modular assembly architecture (`ProjectTwo.Terrain.Core`, `ProjectTwo.Terrain.Runtime`, `ProjectTwo.Terrain.Editor`, `ProjectTwo.Terrain.Tests`). All refactoring and contract reflection tests reside in their designated assembly boundaries.

## Complexity Tracking

> **No violations requiring constitutional justification.**
