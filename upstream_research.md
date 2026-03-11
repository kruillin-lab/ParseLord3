# Upstream Updates & Cactbot Analysis

You asked whether the recent upstream RotationSolverReborn updates add value to ParseLord3, or if the differences in ParseLord3's architecture negate them. You also asked about Cactbot integration.

Here is the analysis:

## 1. Do the Upstream Commits Add Value?

**Verdict: Mostly Negated, but with One Critical Exception.**

Because ParseLord3 is heavily optimized for zero-allocation and strict GCD-first priority, dropping in upstream code directly is dangerous. Upstream RotationSolverReborn often uses LINQ and heavy `IEnumerable` allocations that ParseLord3 specifically stripped out (e.g., in `MajorUpdater.cs` and `Watcher.cs`).

- **DNC Potion Fix / Variant Raise Logic / Beiruta Rotations:** These are minor job-specific logic tweaks. ParseLord3's custom (`_Reborn.cs`) job files are already divergent. Merging these would require manual porting and rewriting them to avoid introducing the heap allocations we just removed. **Not worth the effort unless you specifically play these jobs and notice bugs.**
- **AST/PLD Action Locking Hotfix (Passage of Arms / Collective Unconscious):** **High Value.** If you play Astrologian or Paladin, upstream discovered a bug where these channeled abilities were getting interrupted or locking the queue. ParseLord3 _does_ share the core ActionQueueManager logic. If you encounter queue lockups on these jobs, we should manually port ONLY the boolean check that fixes this, ensuring it remains allocation-free.

## 2. What about Cactbot Integration?

**Verdict: Already Built-in, but Disabled & Experimental.**

I searched the ParseLord3 codebase for `cactbot`. It turns out ParseLord3 **already has** the Cactbot integration merged from an older upstream PR (PR #994 from Sept 2025).

- **Location:** `RotationSolver\Helpers\CactbotTimelineBridge.cs`
- **How it Works:** It connects to the `OverlayPlugin` WebSocket (`ws://127.0.0.1:10501/ws`) and listens for broadcast messages from Cactbot timelines (like `"raidwide"`, `"tankbuster"`, `"knockback"`). It then forces the RotationSolver into `SpecialCommandType` modes (like auto-mitigating or preventing cast clipping).
- **Current State:** It is completely disabled by default. Upstream noted in their commit history: _"Disabled cactbot integration for now while I work on it"_ because it was occasionally bricking the rotation solver.
- **How to Enable:** In the ParseLord3 config UI, there is a toggle: **"Enable cactbot timeline integration (Extremely experimental)"**.

### Recommendation

I do **not** recommend enabling the Cactbot integration or pulling down massive upstream PRs right now. We just achieved a highly stable, zero-allocation build. The Cactbot bridge uses `Newtonsoft.Json.Linq` parsing (`JObject.Parse`) on every single WebSocket broadcast, which will cause massive garbage collection spikes during raids, entirely defeating our optimization work.

If you want to use the Cactbot bridge in the future, we would need to rewrite `CactbotTimelineBridge.cs` to use `System.Text.Json` with a zero-allocation `Utf8JsonReader`.

---

**Summary:** The project is in a great, stable state. I recommend we stop here, keep the zero-allocation build as-is, and only cherry-pick the PLD/AST hotfix later if you actually experience issues on those specific jobs.
