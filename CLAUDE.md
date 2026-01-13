# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Parse Lord is a Dalamud plugin for FFXIV that provides automated combat rotations. It combines RSR-style high-performance action execution with Wrath Combo-style granular configuration UI. The plugin targets FFXIV Patch 7.4 / Dalamud API 14 / .NET 10.

## Build Commands

```bash
# Build (Release)
dotnet build AutoRotationPlugin.csproj -c Release

# Or use the build script
./build.sh
```

The PostBuild target automatically copies the compiled DLL to `%APPDATA%\XIVLauncher\devPlugins\ParseLord\`.

## Architecture

### Core Flow
1. **Plugin.cs** - Entry point. Registers commands (`/pl`, `/parselord`), hooks into `Framework.Update`, initializes services
2. **RotationManager.cs** - Main loop. On each frame update: checks custom stacks first, then falls back to job-specific rotation
3. **IRotation** - Interface for job rotations. Implementations return `ActionInfo?` for the next action
4. **ActionManager.cs** - Singleton (unsafe) that wraps `FFXIVClientStructs.ActionManager`. Handles action execution, cooldowns, weaving, status checks

### Rotation Decision Flow
```
Framework.Update → RotationManager.OnFrameworkUpdate
  → Check CustomStacks (reaction-style triggers with target chains)
  → If no custom action: call IRotation.GetNextAction()
    → Emergency (self-heals)
    → Opener sequence (if enabled)
    → oGCD weaving (if CanWeave())
    → AoE or Single Target GCDs
```

### Key Patterns

**Target Resolution** (`RotationManager.ResolveTarget`): Converts `TargetTag` enums (Self, Tank, LowestHpPartyMember, Mouseover, etc.) to actual game objects.

**Weaving Logic** (`ActionManager`):
- `CanWeave()` - checks if GCD remaining >= 0.7s and not animation locked
- `CanDoubleWeave()` - checks if GCD remaining >= 1.25s
- Actions execute only when the weave window is open

**Job Gauge Reading** (`JobGaugeReader.cs`): Static accessors for DRG (Eye count, LOTD timer), PLD (Oath gauge), WHM (Lily/Blood Lily counts).

**Game State** (`GameState.cs`): Centralized, null-safe accessors for player state, target state, party members, status effects. Isolates Dalamud API calls.

### Job Rotations
Located in `Rotations/`:
- `DragoonRotation.cs` - Full combo chains, dragon gauge management, burst alignment
- `PaladinRotation.cs` - Physical/Magic phase rotation, oath gauge
- `WhiteMageRotation.cs` - Healing priority with lily management, DPS when safe

### Configuration
`Configuration.cs` contains 100+ granular toggles organized by job and feature (ST combo, AoE, buffs, oGCDs, defensive, utility). Each healing ability can have its own target priority chain via `HealTargetPriority` lists.

Custom reaction-style triggers (`CustomTrigger`) support conditions like HP thresholds, status checks, gauge values, and target chains for fall-through targeting.

## Key Dependencies
- **Dalamud API 14** - Plugin framework
- **FFXIVClientStructs** - Direct game memory access for action execution
- **Lumina** - Game data access
- **DalamudPackager** - Build packaging (Release mode only)

## File Naming
- Assembly: `AutoRotationPlugin`
- Manifest: `AutoRotationPlugin.json`
- Display name: "Parse Lord"
- Commands: `/pl`, `/parselord`

## Important Notes
- The `ActionManager` bypasses hotbar checks (status code 574) to execute actions directly
- Combo state is tracked via `ActionManager.ComboAction` and `ComboTimer`
- Status IDs and Action IDs are hardcoded constants in rotation files
- Plugin uses `IObjectTable.LocalPlayer` (not deprecated `IClientState.LocalPlayer`)
