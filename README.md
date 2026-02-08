# ParseLord3 (FFXIV Patch 7.4 / Dalamud API 14)

![ParseLord Icon](https://raw.githubusercontent.com/kruil/ParseLord3/main/Images/Logo.png)

ParseLord3 is a high-performance Dalamud rotation engine focused on clean execution, granular configuration, and modern UX polish. It mixes the reliability of an action queue with teaching overlays so you can practice or fully automate depending on the situation.

## Feature Highlights
- **Manual Target Respect** – Advanced target tracking prevents the engine from overriding deliberate target swaps.
- **Teaching & Overlay Mode** – Highlight the recommended action, tint disabled hotbars, and step through rotations without firing abilities.
- **Action Timeline Debugger** – Inspect queued GCD/oGCD sequences with sub-100ms timing detail to diagnose clipping.
- **Target Priority Chains** – Configure fallbacks such as Mouseover → Focus → Lowest HP for both heals and damage.
- **Burst & Utility Toggles** – Every supported job exposes per-action gates for burst, movement, healing, mitigation, and downtime logic.
- **Rotation Sequencer** – Script deterministic opener or mitigation plans that live alongside dynamic priorities.

## Supported Jobs
- Dragoon
- Paladin
- White Mage

Additional jobs can be authored through the ParseLord.Basic interface and loaded at runtime.

## Commands
| Command | Description |
| --- | --- |
| `/rotation` | Main command prefix (legacy compatibility). |
| `/parselord` | Shorthand alias that opens the configuration window. |
| `/pl` | Quick alias for window + toggles. |
| `/rotation auto` | Enable automated execution. |
| `/rotation off` | Disable execution and reset state. |

## Installation (Dev Build)
1. Install the Dalamud dev hooks (XIVLauncher → Settings → Dalamud → Dev).
2. Clone this repository to `C:\Users\kruil\Documents\Projects\Parselord3`.
3. Open `RotationSolver.sln` with Visual Studio 2026 (or `dotnet build` from CLI).
4. Build in **Release | x64**.
5. The post-build target copies the following into `%APPDATA%\XIVLauncher\devPlugins\ParseLord3\`:
   - `ParseLord3.dll`
   - `ParseLord3.json`
   - `RotationSolver.Basic.dll`
   - `ECommons.dll`
6. Add the dev plugin directory to Dalamud (`/xlsettings → Experimental → Dev Plugins`).
7. Launch FFXIV and enable ParseLord3 from the Dev Tools tab.

## Configuration Tips
- Enable **Teaching Mode** to display overlays without firing actions.
- Use the **Manual Target Override** toggle if you want ParseLord3 to pause when you swap targets mid-fight.
- The **Action Timeline** window requires both "Teaching Mode" and "Show Action Timeline" toggles.
- Debug trace logging can be enabled from the Auto-Rotation tab for deep timing analysis (output to `%APPDATA%\XIVLauncher\dalamud.log`).

## Development Notes
- Target framework: `net10.0-windows10.0.26100.0`
- SDK: `Dalamud.NET.Sdk/14.0.1`
- Build tooling: `dotnet build RotationSolver.csproj -c Release`
- Primary projects:
  - `RotationSolver` (ParseLord3 plugin entry & UI)
  - `RotationSolver.Basic` (rotation/runtime primitives)
  - `RotationSolver.SourceGenerators` (analyzers/helpers)

### Repository Layout
```
RotationSolver/           # Plugin entry, UI, IPC, and updater loops
RotationSolver.Basic/     # Core interfaces, data models, targeting + rotation helpers
RotationSolver.SourceGenerators/
Resources/                # Priority tables and localized data
new_docs/                 # Technical documentation (ParseLord Demystified, etc.)
Images/                   # Branding assets used by the manifest + README
```

## Contributing
1. Fork the repository.
2. Create a feature branch.
3. Follow the existing code style (nullable enabled, aggressive analyzers, no `@ts-ignore`-style hacks).
4. Add or update rotation tests where possible and run `dotnet build` before pushing.
5. Submit a PR with a clear description of logic changes and verification steps.

## Disclaimer
This plugin automates combat actions and thus violates the FFXIV Terms of Service. Use at your own risk. The maintainers are not responsible for any disciplinary action taken by Square Enix.
