# Findings

## Constraints & Context

- Project: ParseLord3
- Target: FFXIV Patch 7.4 / Dalamud API 14 / .NET 10.
- Output Payload: The compiled DLLs (AutoRotationPlugin.dll / ParseLord3.dll) and JSON manifest sent to `C:\Users\kruil\AppData\Roaming\XIVLauncher\devPlugins\ParseLord3`.
- The user referenced "dalamaud dev skill". The file `Obsidian\Dalamud-Patterns.md` exists and contains cross-project Dalamud patterns, which we will use as the strict behavioral framework. (Error loading `.claude/skills/dalamud-dev.md` -> it may not exist, relying on `Dalamud-Patterns.md`).
