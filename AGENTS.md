# AGENTS.md

> **AI AGENT INSTRUCTIONS**
> This file contains the operational parameters for AI agents working on ParseLord3.
> For historical context and work logs, see `AGENTS_HISTORY.md`.

## 1. Project Overview

**ParseLord3** is a Dalamud plugin (FFXIV) for automated combat rotations.
- **Language**: C# (.NET 10 / `net10.0-windows10.0.26100.0`)
- **Core Library**: `RotationSolver.Basic` (Base classes, Action logic)
- **Plugin UI**: `RotationSolver` (ImGui, Entry point, Rotations)
- **Rotations**: `RotationSolver/RebornRotations/{Role}/{Job}_Reborn.cs`
- **Output**: `%APPDATA%\XIVLauncher\devPlugins\ParseLord3\`

## 2. Build & Test Commands

**Build (Release)** - Run this after **ANY** change to verify syntax:
```bash
dotnet build RotationSolver/RotationSolver.csproj -c Release
```

**Build Core Only** (Faster for logic-only changes):
```bash
dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release
```

**Run All Tests**:
```bash
dotnet test -c Release
```

**Run Single Test**:
```bash
dotnet test --filter "FullyQualifiedName~TestMethodName" -c Release
```

**Build Solution**:
```bash
dotnet build RotationSolver.sln -c Release
```

## 3. Code Style & Conventions

**Formatting**:
- **Namespace**: File-scoped (`namespace RotationSolver.UI;`)
- **Braces**: Allman style (opening brace on new line)
- **Indentation**: 4 Spaces (No tabs)
- **Line Length**: Keep under 120 characters when possible
- **Regions**: Use `#region Name` / `#endregion` for grouping large blocks

**Naming**:
- **Classes/Methods**: `PascalCase` (e.g., `ActionQueueManager`)
- **Private Fields**: `_camelCase` with underscore prefix (e.g., `_useActionHook`)
- **Locals/Params**: `camelCase` (e.g., `actionManager`)
- **Constants**: `PascalCase` (e.g., `BlackListedInterceptActions`)
- **Interfaces**: `I` prefix (e.g., `IGameObject`)

**Imports (Usings)**:
- Sort order: System -> Microsoft -> ThirdParty (Dalamud, ECommons) -> Project
- Global usings are enabled (see `Directory.Build.props`)
- Common global usings: `System.Numerics`, `System.Reflection`
- Project global usings in `.csproj` files

**Types & Nullability**:
- Nullable reference types enabled globally (`<Nullable>enable</Nullable>`)
- Use `?` for nullable types (e.g., `string?`, `ActionID?`)
- **Avoid** `!` (null-forgiving) unless absolutely necessary
- Prefer `var` when type is obvious from right-hand side

**Error Handling**:
- **Never** use empty `catch {}`
- Always log errors via `PluginLog`:
  ```csharp
  try { ... } 
  catch (Exception ex) 
  { 
      PluginLog.Error(ex, "Failed to initialize action hooks"); 
  }
  ```
- Use `PluginLog.Debug/Info/Warning/Error` from `ECommons.Logging`

## 4. Agent Operational Rules

1. **Verify Before Commit**: ALWAYS run `dotnet build` before finishing a task
2. **No Hallucinations**: Do not import libraries not already in `.csproj`
3. **Scoped Changes**: If fixing a bug, do not refactor unrelated code
4. **UI Changes**:
   - Keep `Draw()` methods fast (no heavy logic/allocations)
   - Use `ImGuiEx` helpers where possible
5. **Rotation Logic**:
   - Modifying `CanUse(out act)` is the primary way to queue actions
   - Status checks: `HasStatus(true, StatusID.X)` (Self) vs `HasStatus(false)` (Any)

## 5. Key File Locations

| Component | Path Pattern |
|-----------|--------------|
| **Rotations** | `RotationSolver/RebornRotations/{Role}/{Job}_Reborn.cs` |
| **Action Logic** | `RotationSolver.Basic/Actions/` |
| **Config UI** | `RotationSolver/UI/RotationConfigWindow*.cs` |
| **Action Queue** | `RotationSolver/Updaters/ActionQueueManager.cs` |
| **Global Config** | `RotationSolver.Basic/Configuration/Configs.cs` |
| **Action IDs** | `RotationSolver.Basic/Data/ActionID.cs` |
| **Status IDs** | `RotationSolver.Basic/Data/StatusID.cs` |
| **Tests** | `RotationSolver.Tests/` (xUnit framework) |

## 6. Common Issues / Troubleshooting

- **LSP Errors**: Editor may report 100+ "false positive" errors (missing references). **Trust `dotnet build` output.** If build succeeds, ignore LSP red lines
- **Animation Lock**: Use `ActionManager.Instance()->GetActionStatus` to check status `574` (AnimLock)
- **Action IDs**: Use `ActionID.Name` enum. Be careful with PvP vs PvE IDs (PvP often ~29000+)
- **Target Filtering**:
  - `IsPlayer` is NOT available on `IGameObject`
  - Use `obj is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter` instead
- **GetActionStatus**:
  - Use `0xE0000000` for generic “can I use this at all?” checks.
  - For target-dependent actions (e.g., buffs/cards), pass the resolved target’s ID to avoid false “not available”.

## 7. Current Context

See `AGENTS_HISTORY.md` for detailed work logs.
- **Target**: FFXIV Patch 7.4 Optimization
- **All Jobs Optimized**: PCT, DNC, SMN, RDM, BLM, GNB, PLD, DRK, WAR, WHM, AST, SCH, SGE, NIN, DRG, RPR, SAM, MNK, VPR, MCH, BRD

## 8. Agent Workflow Protocol

1. **Context**: Read `AGENTS_HISTORY.md` to understand recent changes
2. **Search**: Locate relevant files using `find` or `grep`. Do NOT rely on memory
3. **Analyze**: Read code to understand logic flow
4. **Implement**: Make focused, minimal changes
5. **Verify**: Run `dotnet build` and ensure success
6. **Log**: Update `AGENTS_HISTORY.md` with your changes

## 10. PromptMaxer
If the user asks to “prompt max/maxer/maximize”, follow `PromptMaxer.md` (Return mode vs Execute mode).

## 9. Specific Implementation Details

### Action Stacks
- Used in `ActionQueueManager.cs`
- Allows overriding an action (Trigger) with sequence of other actions (Stack)
- Supports target types: `Target`, `Tank`, `PlayerTarget`, etc.

### Debugging
- Enable "Debug Trace" in UI for detailed logs
- Logs appear in `%APPDATA%\XIVLauncher\dalamud.log` with `[ParseLord3]` prefix
- Key loggers: `[ActionQueueManager]`, `[ActionUpdater]`, `[MajorUpdater]`

### Game Data
- Use `Service.GetAdjustedActionId(id)` for job gauge adjustments (e.g., AST cards)
- PvP action IDs typically start at ~29000

### Testing
- Uses **xUnit** framework
- Tests located in `RotationSolver.Tests/`
- Run single test with: `dotnet test --filter "FullyQualifiedName~MethodName"`
