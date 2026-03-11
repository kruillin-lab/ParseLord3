# Optimizer SOP (Layer 1 Architecture)

## Goals

Identify and remove runtime heap allocations in ParseLord3's `Framework.Update` loop, particularly in `MajorUpdater.cs`, `ActionQueueManager.cs`, and `Watcher.cs`. Ensure FFXIV performance remains completely uninterrupted.

## Inputs

- .NET 10 CLR Profiling & Code Analysis contexts.
- Dalamud API 14 data structures and lifecycle bindings.

## Logic / Rules

1. **Zero Allocations in `OnFrameworkUpdate`:**
   - Avoid `foreach` over `IReadOnlyList` or `IEnumerable` where it boxing enumerators. Use `for` loops on arrays/Lists where possible.
   - Do not instantiate `new HashSet<T>()` or `new List<T>()` inside the update tick. Pool them or clear/reuse static instances.
2. **Null Safety:** Ensure all player and target resolutions are guarded.
3. **Target Resolution:** `TargetFreely` loops in `MajorUpdater` should cache list limits and avoid enumerator allocations.
4. **Dalumaud Patterns:** Adhere to `Dalamud-Patterns.md` strictly (e.g., `ClientState.LocalPlayer is not null`).
