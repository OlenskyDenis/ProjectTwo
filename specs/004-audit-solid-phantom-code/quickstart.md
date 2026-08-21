# Quickstart & Verification Guide: Codebase SOLID Audit and Contract Testing

## Prerequisites & Setup

1. **.NET SDK & Unity Project**:
   - .NET SDK (supports C# 9.0 / .NET Standard 2.1)
   - Working Directory: `e:\ProjectTwo`

## Build & Test Commands

### 1. Build Entire Solution
Verify that all 6 project assemblies compile cleanly without warnings:
```bash
dotnet build ProjectTwoUnity\ProjectTwoUnity.slnx
```
Expected output:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Execute Test Suites (including Contract Reflection Tests)
Run automated unit, integration, and contract tests:
```bash
dotnet test ProjectTwoUnity\ProjectTwo.Terrain.Tests.csproj
```
Expected output:
```text
All test suites passed (0% regressions).
ContractReflectionTests:
  [PASS] ITerrainShaper_ContainsOnlyAuthoritativePipelineOverloads
  [PASS] HeightMapBuilder_DoesNotExposeDirectNoiseBypass
  [PASS] DomainContracts_AdhereToConstitutionPrincipleVI
```

---

## Validation Scenarios

### Scenario 1: Verify Single Authoritative Pipeline
1. Run `ContractReflectionTests`.
2. Ensure `ITerrainShaper` contains strictly 2 methods: `CalculateElevation` and `GenerateHeightMap` with complete 12-parameter tectonic/hydrology contexts.
3. Confirm no 8-parameter or 11-parameter overloads exist in `ProjectTwo.Terrain.Core.Contracts`.

### Scenario 2: Verify Elimination of Stale Fallback Constructors
1. Check `HeightMapBuilder.cs`.
2. Confirm `HeightMapBuilder` requires explicit injection of `ITerrainShaper` and does not silently instantiate concrete dependencies without caller knowledge.

### Scenario 3: Verify Zero Regressions in Terrain Calculations
1. Run all EditMode domain tests (`ProceduralTerrainShaperTests`, `TectonicServiceTests`, `HydrologyServiceTests`, `TerrainMeshBuilderTests`).
2. Confirm 100% tests pass and heightmaps match deterministic expectations.
