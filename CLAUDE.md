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

## jCodeMunch MCP — REQUIRED Code Navigation Tool

**IMPORTANT: jcodemunch-mcp is installed and indexed for this repo. You MUST use it instead of reading files directly whenever possible. Do NOT use `Read`, `Grep`, or `Glob` on `.cs` files until you have first used jcodemunch to locate what you need.**

The repo index ID is: `local/ParseLord3-3fe5a21f`

### Mandatory Usage Rules

**ALWAYS use jcodemunch first in these situations:**

| Situation | Required jcodemunch tool |
|---|---|
| Looking for a class, method, or function by name | `search_symbols` |
| Need to read a specific method or class body | `get_symbol` |
| Searching for a string, pattern, or usage across the codebase | `search_text` |
| Need to understand what's in a file before reading it | `get_file_outline` |
| Starting work on an unfamiliar file or subsystem | `get_file_outline` first, then `get_symbol` for specific members |

**Only fall back to `Read`/`Grep` when:**
- jcodemunch returns no results for a symbol (may be auto-generated or macro-expanded)
- You need to read a non-`.cs` file (`.json`, `.csproj`, `.md`, etc.)
- You need surrounding context that `get_symbol` doesn't capture (e.g. file-level usings, attributes above a class)

**After large edits, re-index:**
```
index_folder(path="/home/user/ParseLord3", incremental=true)
```

### Setup Reference
- **Installed**: `jcodemunch-mcp` v1.2.5 at `/usr/local/bin/jcodemunch-mcp`
- **Config**: `.mcp.json` at repo root
- **Index**: `~/.code-index/` — 296 C# files, 3,712 symbols

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
