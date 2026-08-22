# Implementation Plan: Asynchronous Multithreaded Pipeline & Streaming Optimization

**Branch**: `007-async-multithreaded-pipeline-optimization` | **Date**: 2026-08-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from [`/specs/007-async-multithreaded-pipeline-optimization/spec.md`](./spec.md)

## Summary

This plan optimizes the entire terrain and river generation pipeline by eliminating all geometry and physics cooking bottlenecks on the Main Thread. It shifts 100% of heightmap, visual mesh, collision mesh, and river ribbon mesh generation into background worker threads (`Task.Run`), pre-bakes PhysX collision data off-thread, introduces a hybrid time-sliced frame budget ($\le 2.0\text{ms}$ / max 2 chunks per frame) in `TerrainGenerator`, and refactors the 12-15 parameter method signatures into a clean `TerrainShaperContext` struct.

## Technical Context

**Language/Version**: C# 9.0 / .NET Standard 2.1
**Primary Dependencies**: Unity 6 (URP 17.x), NUnit Test Framework, PhysX 4.x (`Physics.BakeMesh`)
**Storage**: In-memory chunk storage (`MemoryChunkStorage`)
**Testing**: Unity Test Runner (NUnit EditMode, PlayMode, Integration, and Contract Reflection suites)
**Target Platform**: Windows / macOS / Linux (Desktop multi-core)
**Project Type**: Unity 3D Engine Subsystem
**Performance Goals**:
- Main Thread chunk ingestion budget: $\le 2.0\text{ms}$ per frame
- PlayMode startup framerate: stable $\ge 60\text{ FPS}$ (0 drops to 3-4 FPS)
- Off-thread geometry construction: 100% off Main Thread
**Constraints**:
- Zero visual seam regressions on chunk boundaries (X and Z axes)
- 100% mathematical determinism across seeds

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Principle I (Architectural Integrity & SOLID)**: Clean parameter encapsulation via `TerrainShaperContext` eliminates the Long Parameter List antipattern.
- [x] **Principle II (Comprehensive Testing Standards)**: All changes accompanied by automated Contract Reflection and Decoupling tests.
- [x] **Principle IV (Performance & Resource Efficiency)**: Strict $\le 2.0\text{ms}$ time-slicing budget and off-thread mesh generation ensure consistent 60+ FPS.
- [x] **Principle VI (Single Pipeline & Interface Integrity)**: Single authoritative calculation pipeline maintained with zero legacy overloads.

## Project Structure

### Documentation (this feature)

```text
specs/007-async-multithreaded-pipeline-optimization/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Phase 0 architectural decisions
├── data-model.md        # Phase 1 data models and sequence flow
├── quickstart.md        # Phase 1 validation guide
├── contracts/           # Phase 1 interface contracts
│   ├── ITerrainShaper.md
│   └── IChunkStreamingPipeline.md
└── checklists/
    └── requirements.md  # Specification quality checklist
```

### Source Code Changes

```text
ProjectTwoUnity/Assets/Scripts/Terrain/
├── Core/
│   ├── Models/
│   │   └── TerrainShaperContext.cs            # [NEW] Immutable parameter context struct
│   ├── Contracts/
│   │   └── ITerrainShaper.cs                  # [MODIFY] Refactor signatures to use in TerrainShaperContext
│   └── Services/
│       ├── ProceduralTerrainShaper.cs         # [MODIFY] Implement context-based evaluation
│       ├── HeightMapBuilder.cs                # [MODIFY] Forward context directly
│       └── HydrologyService.cs                # [MODIFY] Forward context directly
├── Presentation/
│   └── Components/
│       ├── ChunkGenerationPayload.cs          # [NEW] Full background transfer payload
│       ├── TerrainChunkView.cs                # [MODIFY] Direct buffer assignment & pre-baked collider hook
│       └── TerrainGenerator.cs                # [MODIFY] Off-thread generation & time-sliced budget loop
└── Tests/
    └── EditMode/
        ├── ContractReflectionTests.cs         # [MODIFY] Assert clean context parameter signatures
        └── DomainDecouplingTests.cs           # [MODIFY] Verify off-thread full-payload generation
```

## Proposed Implementation Phases

### Phase 1: Core Domain Parameter Refactoring
1. Create `TerrainShaperContext.cs` in `Core/Models/`.
2. Refactor `ITerrainShaper.cs`, `ProceduralTerrainShaper.cs`, and `HeightMapBuilder.cs` to consume `in TerrainShaperContext`.
3. Update `HydrologyService.cs` and `TectonicService.cs` to use context.
4. Update `ContractReflectionTests.cs` to validate the new clean signatures.

### Phase 2: Full-Payload Off-Thread Mesh Generation
1. Create `ChunkGenerationPayload.cs` containing `HeightMap`, visual `TerrainMeshData`, collision `TerrainMeshData`, and `RiverWaterMeshData`.
2. Update `TerrainGenerator.RequestChunkGeneration` to build the entire payload inside `Task.Run`.
3. Integrate `Physics.BakeMesh` in background for LOD 0 collision meshes.

### Phase 3: Time-Sliced Main Thread Activation & Validation
1. Refactor `TerrainGenerator.ProcessCompletedChunks` with `Stopwatch` budget ($\le 2.0\text{ms}$ / max 2 chunks per frame).
2. Update `TerrainChunkView.cs` to apply pre-computed payloads directly without recalculating geometry.
3. Run all test suites and measure PlayMode frame pacing in `SampleScene.unity`.
