# ParseLord3 - Project Summary

## Overview

**ParseLord3** is a high-performance Dalamud rotation engine for Final Fantasy XIV (FFXIV) focused on clean execution, granular configuration, and modern UX polish. It combines an action queue system with teaching overlays for practice or full automation.

| Property | Value |
|----------|-------|
| **Author** | kruil |
| **Target Game** | FFXIV Patch 7.4 (Dawntrail) |
| **Framework** | .NET 10 / Dalamud API 14 |
| **Language** | C# 12 |
| **License** | (Check COPYING files) |
| **Repository** | https://github.com/kruil/ParseLord3 |

## Description

ParseLord3 mixes the reliability of an action queue with teaching overlays so you can practice or fully automate depending on the situation. It features manual target respect, target priority chains, burst & utility toggles, and a rotation sequencer for deterministic openers.

## Feature Highlights

- **Manual Target Respect** – Advanced target tracking prevents the engine from overriding deliberate target swaps
- **Teaching & Overlay Mode** – Highlight recommended actions, tint disabled hotbars, step through rotations
- **Action Timeline Debugger** – Inspect queued GCD/oGCD sequences with sub-100ms timing detail
- **Target Priority Chains** – Configure fallbacks (Mouseover → Focus → Lowest HP) for heals and damage
- **Burst & Utility Toggles** – Per-action gates for burst, movement, healing, mitigation, downtime logic
- **Rotation Sequencer** – Script deterministic opener or mitigation plans alongside dynamic priorities

## Supported Jobs

- Dragoon
- Paladin
- White Mage

Additional jobs can be authored through the ParseLord.Basic interface and loaded at runtime.

## Project Structure

```
Parselord3/
├── README.md                      # Main documentation
├── AGENTS.md                      # AI agent instructions (157 lines)
├── AGENTS_HISTORY.md              # Work logs and history
├── manifest.json                  # Plugin manifest
├── RotationSolver.sln             # Visual Studio solution
├── Directory.Build.props          # Global build properties
├── COPYING / COPYING.LESSER       # License files
├── crowdin.yml                    # Localization config
│
├── RotationSolver/                # Main plugin (UI, entry point, rotations)
│   ├── RotationSolverPlugin.cs    # Plugin entry point
│   ├── RotationSolver.csproj      # Project file
│   ├── RotationSolver.yaml        # Plugin config
│   ├── ParseLord3.yaml            # Branding config
│   ├── Commands/                  # Slash command handlers
│   ├── UI/                        # ImGui windows and config
│   │   └── RotationConfigWindow*.cs
│   ├── Updaters/                  # Update loops
│   │   └── ActionQueueManager.cs  # Core action queuing
│   ├── RebornRotations/           # Job rotation implementations
│   │   ├── Tank/
│   │   ├── Healer/
│   │   ├── Melee/
│   │   ├── Ranged/
│   │   └── Caster/
│   ├── ActionTimeline/            # Timeline debugger
│   ├── Data/                      # Static data
│   ├── Helpers/                   # Utility classes
│   ├── IPC/                       # Inter-plugin communication
│   ├── Watcher.cs                 # State watcher
│   └── TextureItems/              # UI textures
│
├── RotationSolver.Basic/          # Core library (base classes)
│   ├── README.md
│   ├── RotationSolver.Basic.csproj
│   ├── Service.cs                 # Service locator
│   ├── DataCenter.cs              # Game state data
│   ├── Actions/                   # Action logic
│   ├── Configuration/             # Config system
│   │   └── Configs.cs             # Global config
│   ├── Data/                      # Enums and constants
│   │   ├── ActionID.cs            # Action IDs
│   │   └── StatusID.cs            # Status IDs
│   ├── Rotations/                 # Base rotation classes
│   ├── Helpers/                   # Core helpers
│   ├── Services/                  # Dalamud services wrapper
│   ├── Traits/                    # Job traits
│   ├── Tweaks/                    # Game tweaks
│   └── Attributes/                # C# attributes
│
├── RotationSolver.SourceGenerators/  # Code generators
├── RotationSolver.GameData/       # Game data extraction
├── RotationSolver.DocumentationGenerator/  # Docs generator
├── RotationSolver.Tests/          # xUnit test suite
│
├── Resources/                     # Priority tables, localized data
├── Images/                        # Branding assets (Logo.png)
├── new_docs/                      # Technical documentation
├── memory/                        # AI memory/context
├── .opencode/                     # AI agent workspace
├── .beads/                        # Beads workspace
├── .config/                       # Configuration files
├── .github/                       # GitHub workflows
└── .tldr/                         # TLDR pages
```

## Commands

| Command | Alias | Description |
|---------|-------|-------------|
| `/rotation` | - | Main command prefix (legacy) |
| `/parselord` | `/pl` | Open configuration window |
| `/rotation auto` | - | Enable automated execution |
| `/rotation off` | - | Disable execution and reset state |

## Key Components

### Action Queue Manager
- Core queuing system in `Updaters/ActionQueueManager.cs`
- Handles GCD/oGCD sequencing
- Timing analysis with sub-100ms precision

### Rotation System
- Base classes in `RotationSolver.Basic/Rotations/`
- Job implementations in `RotationSolver/RebornRotations/{Role}/`
- Naming pattern: `{Job}_Reborn.cs`

### Configuration
- Global config: `RotationSolver.Basic/Configuration/Configs.cs`
- UI windows: `RotationSolver/UI/RotationConfigWindow*.cs`

### Action/Status IDs
- Actions: `RotationSolver.Basic/Data/ActionID.cs`
- Statuses: `RotationSolver.Basic/Data/StatusID.cs`
- PvP IDs typically start at ~29000

## Build System

### Requirements
- Visual Studio 2022+ (or VS Code with C# extension)
- .NET 10 SDK
- Dalamud dev environment (XIVLauncher)

### Build Commands

```bash
# Full solution build
dotnet build RotationSolver.sln -c Release

# Plugin only
dotnet build RotationSolver/RotationSolver.csproj -c Release

# Core library only (faster)
dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release

# Run tests
dotnet test -c Release

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName" -c Release
```

### Post-Build
Build automatically copies to:
```
%APPDATA%\XIVLauncher\devPlugins\ParseLord3\
```

Files copied:
- `ParseLord3.dll`
- `ParseLord3.json`
- `RotationSolver.Basic.dll`
- `ECommons.dll`

## Code Style

### Formatting
- **Namespace**: File-scoped (`namespace RotationSolver.UI;`)
- **Braces**: Allman style (opening brace on new line)
- **Indentation**: 4 spaces (no tabs)
- **Line Length**: Keep under 120 characters
- **Regions**: Use `#region Name` / `#endregion` for grouping

### Naming
- **Classes/Methods**: `PascalCase`
- **Private Fields**: `_camelCase` with underscore prefix
- **Locals/Params**: `camelCase`
- **Constants**: `PascalCase`
- **Interfaces**: `I` prefix

### Imports
Sort order: System → Microsoft → ThirdParty (Dalamud, ECommons) → Project

### Nullability
- Nullable reference types enabled globally
- Use `?` for nullable types
- **Avoid** `!` (null-forgiving) unless necessary
- Prefer `var` when type is obvious

### Error Handling
```csharp
try { ... } 
catch (Exception ex) 
{ 
    PluginLog.Error(ex, "Failed to initialize action hooks"); 
}
```

## Configuration Tips

- Enable **Teaching Mode** to display overlays without firing actions
- Use **Manual Target Override** to pause on target swaps
- **Action Timeline** requires both "Teaching Mode" and "Show Action Timeline" toggles
- Debug trace logging outputs to `%APPDATA%\XIVLauncher\dalamud.log`

## Current Status (Patch 7.4)

All jobs optimized:
- **Tanks**: GNB, PLD, DRK, WAR
- **Healers**: WHM, AST, SCH, SGE
- **Melee**: NIN, DRG, RPR, SAM, MNK, VPR
- **Ranged**: MCH, BRD, DNC
- **Casters**: PCT, SMN, RDM, BLM

## Troubleshooting

### LSP Errors
Editor may report 100+ "false positive" errors. **Trust `dotnet build` output.** If build succeeds, ignore red lines.

### Animation Lock
Use `ActionManager.Instance()->GetActionStatus` to check status `574` (AnimLock).

### Target Filtering
- `IsPlayer` is NOT available on `IGameObject`
- Use `obj is IPlayerCharacter` instead

### GetActionStatus
- Use `0xE0000000` for generic "can I use this at all?" checks
- For target-dependent actions, pass the resolved target's ID

## External Resources

- [Dalamud Documentation](https://dalamud.dev/)
- [FFXIV Client Structs](https://github.com/aers/FFXIVClientStructs)

## Disclaimer

This plugin automates combat actions and violates the FFXIV Terms of Service. Use at your own risk.

---

*Generated: March 2026*
*Plugin Manifest: manifest.json*
