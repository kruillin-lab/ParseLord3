# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ParseLord3** is a Dalamud plugin for FFXIV that automates combat rotations.
- **Framework**: .NET 10 (C#) / Dalamud API 14
- **Target**: FFXIV Patch 7.4
- **Core Logic**: `RotationSolver.Basic` (Base classes, Action logic)
- **UI/Entry**: `RotationSolver` (ImGui, Plugin lifecycle)

## Build & Test Commands

**IMPORTANT**: LSP errors in the editor are often false positives. **Trust the build output.**

```bash
# Build full solution (Release) - Run after ANY change
dotnet build RotationSolver.sln -c Release

# Build Core Library only (Faster for logic-only changes)
dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release

# Build Plugin only (UI/Updater changes)
dotnet build RotationSolver/RotationSolver.csproj -c Release

# Run Tests
dotnet test -c Release
# Run single test
dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName" -c Release
```

## Architecture

The solution is divided into 5 main projects:
1. **RotationSolver**: Main plugin entry, UI (ImGui), IPC, and Update loops.
2. **RotationSolver.Basic**: Core library containing `BaseAction`, `BaseRotation`, helpers, and game data definitions.
3. **RotationSolver.SourceGenerators**: Code generation for static resources.
4. **RotationSolver.GameData**: Static game data resources.
5. **RotationSolver.DocumentationGenerator**: Docs generation.

### Key File Locations

| Component | Path Pattern |
|-----------|--------------|
| **Job Rotations** | `RotationSolver/RebornRotations/{Role}/{Job}_Reborn.cs` |
| **Base Rotations** | `RotationSolver.Basic/Rotations/Basic/{Job}Rotation.cs` |
| **Action Logic** | `RotationSolver.Basic/Actions/` |
| **Config UI** | `RotationSolver/UI/` |
| **Global Config** | `RotationSolver.Basic/Configuration/Configs.cs` |

### Rotation Structure
Rotations inherit from job-specific base classes and implement three main logic blocks:
1. **`EmergencyAbility`**: High-priority oGCDs (Mitigation, Interrupts, Critical Buffs). Fires first.
2. **`GeneralGCD`**: The main GCD loop (Combo actions, Spells).
3. **`AttackAbility`**: Offensive oGCDs to weave between GCDs.

**Smart Mitigation System**: A centralized system in `RotationSolver.Basic/Helpers/MitigationHelper.cs` handles tank defensive cooldowns automatically.

## Code Style & Guidelines

- **Formatting**: 4 spaces indentation (no tabs). File-scoped namespaces.
- **Nullability**: Enabled globally. Use `?` for nullable types. Avoid `!` unless absolutely necessary.
- **Error Handling**: **NEVER** use empty catch blocks. Log exceptions using `PluginLog.Error(ex, "Context")`.
- **Targeting**:
  - Use `StatusID` enum for buff/debuff checks.
  - `IsPlayer` is not available on `IGameObject`; check `obj is IPlayerCharacter`.
- **Action IDs**: Use `ActionID` enum. Be aware of PvP vs PvE ID differences.

## Development Constraints

1. **Verify Before Commit**: Always run a build before finishing a task.
2. **No Hallucinations**: Do not add dependencies not already in the `.csproj`.
3. **Scoped Changes**: Focus only on the requested task; do not refactor unrelated code.
4. **UI Performance**: Keep `Draw()` methods fast; avoid allocations in the draw loop.
5. **Action Queue**: Modifying `CanUse(out act)` is the primary method to queue actions.

## Troubleshooting

- **Animation Lock**: Use `ActionManager.Instance()->GetActionStatus` to check status 574.
- **Action Stacks**: Used in `ActionQueueManager.cs` to override trigger actions with sequences.
- **Debug Trace**: Enable "Debug Trace" in the UI to log logic decisions to `%APPDATA%\XIVLauncher\dalamud.log` with prefix `[ParseLord3]`.
