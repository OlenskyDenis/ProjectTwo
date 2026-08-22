# Implementation Plan: Advanced Hydrology, Waterfall Dynamics & Continuous River Networks

**Branch**: `006-advanced-hydrology` | **Date**: 2026-08-21 | **Spec**: [spec.md](file:///e:/ProjectTwo/specs/006-advanced-hydrology/spec.md)

## Summary

Implement realistic continuous hydrology featuring adaptive cliff-conforming waterfalls (1–2m vertical steps), basin depression saddle spillover (multi-tier lake cascades), hydraulic momentum to prevent flow dead-ends, tributary confluence (Strahler order channel widening), and lowland river bifurcation / coastal delta formation.

## Technical Context

**Language/Version**: C# 9.0 / .NET Standard 2.1  
**Primary Dependencies**: Unity 6 (6000.5.5f1), Universal Render Pipeline (URP)  
**Architecture**: Clean Architecture (Pure C# domain core in `ProjectTwo.Terrain.Core`, Unity presentation & mesh building in `ProjectTwo.Terrain.Runtime`)  
**Testing**: NUnit EditMode unit & integration tests (`ProjectTwo.Terrain.Tests`)  
**Target Platform**: PC / Standalone (DirectX 11/12, Vulkan, Metal)  
**Performance Goals**: River graph + lake cascade generation < 25ms; 0 garbage collection during chunk stream traversal; 60+ FPS in Play Mode  
**Constraints**: Zero orphan/floating water segments; bit-exact deterministic river slicing across chunk borders; zero guessing rule compliant  

## Constitution Check

- **Principle I: Architectural Layer Separation** (Pure Core vs Unity Presentation) -> PASS (Hydrology algorithms in `Core`, Mesh building in `Presentation`).
- **Principle II: Zero-Guessing / Strict Uncertainty Handling** -> PASS (Explicit clarification protocol followed).
- **Principle III: Seamless Chunk Streaming & Determinism** -> PASS (Global deterministic river graph cached before chunk slicing).
- **Principle IV: Test-Driven Quality Gates** -> PASS (NUnit tests for continuity, clamping, and spillover).

## Implementation Phases

### Phase 0: Research & Mathematical Foundations (COMPLETED)
- Analyzed 3D surface-aligned binormal frame calculation for vertical waterfalls.
- Designed analytical saddle point detection for lake cascades.
- Validated Strahler stream order channel scaling.

### Phase 1: Data Contracts & Interface Design (COMPLETED)
- Defined `IHydrologyService.cs` and `IRiverMeshBuilder.cs` contracts.
- Defined domain entities: `RiverNode`, `RiverSegment`, `LakeBasin`, `HydrologySettings`.
- Authored `quickstart.md` validation guide.

### Phase 2: Implementation & Task Generation
- Next Step: Run `/speckit-tasks` to decompose implementation into actionable, dependency-ordered tasks.
