# ParseLord3 Optimization Task Plan

## Goal

Optimize the ParseLord3 plugin for FFXIV, strictly adhering to the B.L.A.S.T. methodology and safe Dalamud plugin practices.

## Phases

### Phase 1: Blueprint

- [x] Discovery Questions Answered
- [ ] Update `gemini.md` with final Data Schema / Payload format
- [ ] Research optimization bottlenecks

### Phase 2: Link

- [ ] Verify `dotnet build` executes cleanly.
- [ ] Verify output directory `C:\Users\kruil\AppData\Roaming\XIVLauncher\devPlugins\ParseLord3` is reachable.

### Phase 3: Architect

- [ ] Investigate `ActionManager`, `RotationManager`, and job-specific rotations for allocation/FPS bottlenecks.
- [ ] Propose Architecture improvements in `architecture/`.
- [ ] Write Tools/Scripts for deterministic optimization testing.

### Phase 4: Stylize

- [ ] Validate any UI/Config changes.

### Phase 5: Trigger

- [ ] Final Build and deploy to `devPlugins`.
- [ ] Update maintenance log.
