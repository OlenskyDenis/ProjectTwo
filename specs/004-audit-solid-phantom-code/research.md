# Technical Research: Codebase SOLID Audit, Single Pipeline Governance & Contract Reflection Tests

## 1. Architectural Baseline & SOLID Evaluation

### Context & Findings in Active Codebase
- **Single Responsibility Principle (SRP)**:
  - `HeightMapBuilder.cs`: Handled both simple 2D Perlin noise heightmap extraction AND compound multi-layered procedural terrain shaping with bounds calculation.
  - `ProceduralTerrainShaper.cs`: Pure calculation engine, well structured, but carries legacy overload signatures that bypass tectonic and hydrological layers.
  - `TerrainGenerator.cs`: Responsible for chunk streaming, LOD updates, and gizmos. Services are injected internally in `InitializeServices()`.
- **Open/Closed Principle (OCP)**:
  - Domain models (`NoiseSettings`, `MacroMaskSettings`, `TectonicSettings`, `HydrologySettings`) use structs and validated domain presets, allowing extension without modifying existing shaper algorithms.
- **Liskov Substitution Principle (LSP)**:
  - All implementations (`ProceduralTerrainShaper`, `TectonicService`, `HydrologyService`, `RiverMeshBuilder`, `MemoryChunkStorage`) cleanly implement their respective contracts (`ITerrainShaper`, `ITectonicService`, `IHydrologyService`, `IRiverMeshBuilder`, `IChunkStorage`) without mutating base contract guarantees.
- **Interface Segregation Principle (ISP)**:
  - `ITerrainProvider` cleanly segregates event subscriptions from spatial queries (`GetHeight`, `GetNormal`, `GetSlope`, `GetBiomeAt`, `IsPositionLoaded`).
  - `ITerrainShaper` had bloated legacy overloads that forced callers to choose between partial (legacy) vs authoritative parameter lists.
- **Dependency Inversion Principle (DIP)**:
  - `HeightMapBuilder.cs` contained dual fallback constructors with hardcoded `new PerlinNoiseGenerator()` and `new ProceduralTerrainShaper()`, violating pure dependency injection.
- **Constitution Principle VI (Zero Stale Contract Tolerance & Single Pipeline)**:
  - **Critical Finding**: Legacy overloads in `ITerrainShaper` (`CalculateElevation` with 8 parameters and `GenerateHeightMap` with 11 parameters) bypass tectonic plates, boundaries, and hydrology river graphs.
  - In `HeightMapBuilder`, `GenerateHeightMap(int, int, NoiseSettings, ChunkCoordinate)` delegates directly to `INoiseGenerator`, bypassing all macro, curve, tectonic, and river shaping logic.

---

## 2. Phantom & Dead Code Inventory Strategy

### Identification Criteria
1. **Obsolete Overloads**: Methods providing subsets of domain parameters that result in incomplete or bifurcated generation pipelines.
2. **Unused / Phantom Parameters**: Configuration parameters present on ScriptableObjects or structs that are never read by shaper services or mesh builders.
3. **Dead Private / Internal Symbols**: Unreferenced helper functions or static fields not used in active runtime or test assemblies.
4. **Orphaned Serialized Fields**: Inspector fields that have no effect on runtime behavior.

### Confirmed Remediation Actions
1. Remove stale `ITerrainShaper.CalculateElevation` (8-parameter) and `ITerrainShaper.GenerateHeightMap` (11-parameter) overloads from contract and implementations.
2. Unify all tests and callers to invoke the authoritative full pipeline (or use a dedicated parameter struct / context object).
3. Remove redundant `HeightMapBuilder.GenerateHeightMap` bypass and dual fallback constructors.
4. Ensure all exposed `TerrainDataConfig` parameters are validated and mapped end-to-end.

---

## 3. Automated Contract Reflection Tests (CI Guard)

### Decision
Implement automated **Contract Reflection Tests** in `ProjectTwo.Terrain.Tests` using NUnit and .NET reflection.

### Rationale
- Unity C# projects compile against `.NET Standard 2.1`.
- Reflection tests dynamically inspect all public types in `ProjectTwo.Terrain.Core.Contracts` and ensure:
  1. No deprecated or obsolete calculation overloads exist.
  2. Every domain contract method accepting elevation or heightmap generation includes all required context dependencies (Tectonics, Hydrology, Macro, Curves, Water, Rivers, Falloff).
  3. Every public service in `ProjectTwo.Terrain.Core.Services` implements its authoritative contract cleanly without introducing unofficial bypass methods.
  4. Public API surface is asserted via type reflection, preventing accidental drift or silent introduction of parallel calculation pathways.

### Alternatives Considered
- **Roslyn Analyzer Package (NuGet)**: Requires external packaging, custom analyzer DLL build setup, and complex integration into Unity's internal compiler pipeline.
- **DocFX / Swagger UI**: Generates static HTML documentation but does not actively break automated CI builds when a developer introduces a stale overload.
- **Contract Reflection Tests (Selected)**: Executes instantly in `dotnet test` and Unity Test Runner, zero extra dependencies, provides immediate build failure on contract drift.
