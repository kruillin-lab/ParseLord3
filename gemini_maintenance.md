## Maintenance Log - ParseLord3

**Date:** March 10, 2026
**Architect:** System Pilot (A.N.T. Protocol)

### Objectives Completed:

1. **Zero-Allocation Execution Setup:** Refactored high-frequency tick loops (`MajorUpdater.cs`, `Watcher.cs`, `ActionQueueManager.cs`) to eliminate `foreach` enumeration boxing, `HashSet` runtime allocations, and getter overheads. Moved required data structures to static indexed pools, stabilizing the FFXIV `Framework.Update` loop.
2. **Upstream Research & Cactbot Analysis:** Evaluated the modern `FFXIV-CombatReborn` fork. Determined that bulk PR merges would reverse our zero-allocation progress. Identified the built-in experimental Cactbot WebSocket integration (`CactbotTimelineBridge.cs`) and warned against using it until its `JObject.Parse` logic is manually rewritten to standard zero-allocation metrics (`Utf8JsonReader`).

**Next recommended action (if any):** Monitor PLD / AST for Passage of Arms / Collective Unconscious queuing bugs. If they occur, backport _only_ the boolean logic fix from upstream PR #1178.
