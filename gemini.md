# Gemini CLI - ParseLord3 Project Context

This file provides guidance to the Gemini CLI agent when working with the ParseLord3 codebase.

## Project Overview

**ParseLord3** is a high-performance Dalamud plugin for FFXIV (Patch 7.4 / Dalamud API 14) that automates combat rotations with a focus on clean execution and modern UX.

- **Framework**: .NET 10 (C#) / Dalamud API 14 / Windows 10 SDK
- **Core Library**: `RotationSolver.Basic` (Interfaces, Action logic, Mitigation helpers)
- **Plugin Entry**: `RotationSolver` (Lifecycle, UI, IPC)
- **Target Platform**: win32 / FFXIV

## Build & Test Commands

Always verify changes with a build. Use `dotnet build` with the `Release` configuration.

```powershell
# Build full solution
dotnet build RotationSolver.sln -c Release

# Build Core Library only
dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release

# Build Plugin only
dotnet build RotationSolver/RotationSolver.csproj -c Release

# Run Tests
dotnet test -c Release
```

## Repository Structure & Key Locations

| Component                  | Path                                                    |
| -------------------------- | ------------------------------------------------------- |
| **Job Rotations (Reborn)** | `RotationSolver/RebornRotations/{Role}/{Job}_Reborn.cs` |
| **Base Rotations**         | `RotationSolver.Basic/Rotations/Basic/{Job}Rotation.cs` |
| **Action Definitions**     | `RotationSolver.Basic/Actions/`                         |
| **UI Windows**             | `RotationSolver/UI/`                                    |
| **Configuration**          | `RotationSolver.Basic/Configuration/Configs.cs`         |
| **Game Data**              | `RotationSolver.GameData/`                              |

## Rotation Logic Flow

Rotations typically implement three priority blocks:

1.  **`EmergencyAbility`**: High-priority oGCDs (Mitigation, Interrupts, Invulns).
2.  **`GeneralGCD`**: The main GCD combo loop.
3.  **`AttackAbility`**: Offensive oGCDs for weaving.

## Development Guidelines

- **Style**: 4 spaces, file-scoped namespaces.
- **Null Safety**: Nullability is enabled. Use nullable types (`?`) and avoid the null-forgiving operator (`!`).
- **Logging**: Use `PluginLog.Error(ex, "message")` or `PluginLog.Debug("message")`.
- **Dalamud API**:
  - Use `ActionID` and `StatusID` enums for consistency.
  - Check player character using `obj is IPlayerCharacter`.
- **Performance**: Avoid allocations in `Draw()` and `Update()` loops.
- **Verification**: ALWAYS run `dotnet build RotationSolver.sln -c Release` after modifications to ensure everything compiles and post-build tasks (copying to dev folder) succeed.

## Common Tasks

- **Adding a Job**: Implement `{Job}Rotation` in `RotationSolver.Basic` and `{Job}_Reborn` in `RotationSolver`.
- **Fixing Mitigation**: Check `RotationSolver.Basic/Helpers/MitigationHelper.cs`.
- **UI Tweaks**: Look into `RotationSolver/UI/`.

## B.L.A.S.T. Data Schema / Payload Definition

As per the optimization objective, this project does not pass JSON payloads to external web APIs. Instead, the final payload is the compiled assembly module.

- **Input Data**: In-game player state, target state, UI configurations, hotbar layouts (read via Dalamud API / FFXIVClientStructs).
- **Output Payload**:
  - `AutoRotationPlugin.dll` / `ParseLord3.dll`
  - Delivery path: `C:\Users\kruil\AppData\Roaming\XIVLauncher\devPlugins\ParseLord3`
- **Invariants**:
  - No heap allocations in `OnFrameworkUpdate` (zero-allocation main loop).
  - All status/gauge checks must be explicitly null-safe.
  - Adhere strictly to the cross-project `Dalamud-Patterns`.
