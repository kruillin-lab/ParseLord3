using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Commands;
using RotationSolver.IPC;
using RotationSolver.UI.HighlightTeachingMode;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.Updaters;

internal static class MajorUpdater
{
    private static TimeSpan _timeSinceUpdate = TimeSpan.Zero;

    // Gating and state for segmented updates
    private static bool _shouldRunThisCycle;
    private static bool _isValidThisCycle;
    private static bool _isActivatedThisCycle;
    private static bool _rotationsLoaded;

    // Manual target override tracking
    private static ulong _lastKnownTargetId = 0;
    private static bool _parseLordIsSettingTarget = false;

    /// <summary>
    /// Call this before ParseLord sets a target to prevent it from being detected as a manual change.
    /// </summary>
    internal static void BeginParseLordTargetChange()
    {
        _parseLordIsSettingTarget = true;
    }

    /// <summary>
    /// Call this after ParseLord sets a target.
    /// </summary>
    internal static void EndParseLordTargetChange()
    {
        _parseLordIsSettingTarget = false;
        _lastKnownTargetId = Svc.Targets.Target?.GameObjectId ?? 0;
    }

    /// <summary>
    /// Checks if the current target differs from the last known target set by ParseLord.
    /// If so, the player manually changed their target.
    /// </summary>
    private static void UpdateManualTargetOverride()
    {
        if (!Service.Config.RespectManualTarget)
        {
            DataCenter.ManualTargetOverride = false;
            return;
        }

        ulong currentTargetId = Svc.Targets.Target?.GameObjectId ?? 0;

        // If ParseLord is currently setting a target, don't treat this as manual
        if (_parseLordIsSettingTarget)
        {
            return;
        }

        // If target changed and ParseLord didn't do it, it's a manual change
        if (currentTargetId != _lastKnownTargetId && currentTargetId != 0)
        {
            // Verify the new target is a valid hostile target
            var newTarget = Svc.Targets.Target;
            if (newTarget is IBattleChara bc && bc.IsEnemy())
            {
                DataCenter.ManualTargetOverride = true;
                DataCenter.ManualTargetId = currentTargetId;
                DataCenter.ManualTargetTime = DateTime.Now;
                PluginLog.Debug($"Manual target override detected: {newTarget.Name}");
            }
        }

        // Clear manual override if:
        // 1. The manually selected target died or is no longer valid
        // 2. The player cleared their target (currentTargetId == 0)
        // 3. Combat ended
        if (DataCenter.ManualTargetOverride)
        {
            bool shouldClear = false;

            // Target was cleared
            if (currentTargetId == 0)
            {
                shouldClear = true;
            }
            // Target changed to something else (player selected a different target)
            else if (currentTargetId != DataCenter.ManualTargetId)
            {
                // Update to the new manual target
                var newTarget = Svc.Targets.Target;
                if (newTarget is IBattleChara bc && bc.IsEnemy())
                {
                    DataCenter.ManualTargetId = currentTargetId;
                    DataCenter.ManualTargetTime = DateTime.Now;
                }
                else
                {
                    shouldClear = true;
                }
            }
            // Manual target died or became untargetable
            else
            {
                var manualTarget = Svc.Objects.SearchById(DataCenter.ManualTargetId);
                if (manualTarget == null || !manualTarget.IsTargetable ||
                    (manualTarget is IBattleChara bc && bc.CurrentHp == 0))
                {
                    shouldClear = true;
                }
            }

            // Combat ended
            if (!DataCenter.InCombat && DataCenter.NotInCombatDelay)
            {
                shouldClear = true;
            }

            if (shouldClear)
            {
                DataCenter.ManualTargetOverride = false;
                DataCenter.ManualTargetId = 0;
                DataCenter.ManualTargetTime = DateTime.MinValue;
                PluginLog.Debug("Manual target override cleared");
            }
        }

        _lastKnownTargetId = currentTargetId;
    }

    public static bool IsValid
    {
        get
        {
            if (!Player.Available)
            {
                _rotationsLoaded = false;
                return false;
            }

            // Consider the game valid when not transitioning or logging out.
            if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51] || Svc.Condition[ConditionFlag.LoggingOut])
            {
                _rotationsLoaded = false;
                return false;
            }

            return true;
        }
    }

    private static Exception? _threadException;

    public static void Enable()
    {
        ActionSequencerUpdater.Enable(Svc.PluginInterface.ConfigDirectory.FullName + "\\Conditions");

        Svc.Framework.Update += ParseLordGateUpdate;
        Svc.Framework.Update += ParseLordTeachingClearUpdate;
        Svc.Framework.Update += ParseLordInvalidUpdate;
        Svc.Framework.Update += ParseLordActivatedCoreUpdate;
        Svc.Framework.Update += ParseLordActivatedHighlightUpdate;
        Svc.Framework.Update += ParseLordCommonUpdate;
        Svc.Framework.Update += ParseLordCleanupUpdate;
        Svc.Framework.Update += ParseLordRotationAndStateUpdate;
        Svc.Framework.Update += ParseLordMiscAndTargetFreelyUpdate;
        Svc.Framework.Update += ParseLordResetUpdate;
    }

    private static void ParseLordGateUpdate(IFramework framework)

    {
        try
        {
            // Throttle by MinUpdatingTime
            _timeSinceUpdate += framework.UpdateDelta;
            if (Service.Config.MinUpdatingTime > 0 && _timeSinceUpdate < TimeSpan.FromSeconds(Service.Config.MinUpdatingTime))
            {
                _shouldRunThisCycle = false;
                return;
            }

            _timeSinceUpdate = TimeSpan.Zero;
            _isValidThisCycle = IsValid;
            _isActivatedThisCycle = DataCenter.IsActivated();
            _shouldRunThisCycle = true;

            // Opportunistically load rotations if not yet loaded
            if (_isValidThisCycle && !_rotationsLoaded)
            {
                RotationUpdater.LoadBuiltInRotations();
                _rotationsLoaded = true;
            }
        }
        catch (Exception ex)
        {
            LogOnce("GateUpdate Exception", ex);
        }
    }

    private static void ParseLordTeachingClearUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        if (Service.Config.TeachingMode)
        {
            try
            {
                HotbarHighlightManager.HotbarIDs.Clear();
            }
            catch (Exception ex)
            {
                LogOnce("HotbarHighlightManager.HotbarIDs.Clear Exception", ex);
            }
        }
    }

    private static void ParseLordInvalidUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        if (!_isValidThisCycle)
        {
            try
            {
                RSCommands.UpdateRotationState();
                ActionUpdater.ClearNextAction();
                MiscUpdater.UpdateEntry();
                ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
            }
            catch (Exception ex)
            {
                LogOnce("RSRInvalidUpdate Exception", ex);
            }

            // Do not run the rest of the cycle
            _shouldRunThisCycle = false;
        }
    }

    private static void ParseLordActivatedCoreUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        var autoOnEnabled = (Service.Config.StartOnAllianceIsInCombat2
            || Service.Config.StartOnAttackedBySomeone2
            || Service.Config.StartOnFieldOpInCombat2
            || Service.Config.StartOnPartyIsInCombat2) && !DataCenter.IsInDutyReplay();

        try
        {
            if (autoOnEnabled)
            {
                TargetUpdater.UpdateTargets();
            }
            if (!_isActivatedThisCycle)
                return;

            bool canDoAction = ActionUpdater.CanDoAction();
            MovingUpdater.UpdateCanMove(canDoAction);

            if (canDoAction)
            {
                RSCommands.DoAction();
            }

            MacroUpdater.UpdateMacro();

            TargetUpdater.UpdateTargets();

            StateUpdater.UpdateState();

            ActionUpdater.UpdateNextAction();

            // In Target-Only mode, update the player's target from the computed next action without executing it.
            if (DataCenter.IsTargetOnly)
            {
                RSCommands.UpdateTargetFromNextAction();
            }

            ActionSequencerUpdater.UpdateActionSequencerAction();
            Wrath_IPCSubscriber.DisableAutoRotation();
        }
        catch (Exception ex)
        {
            LogOnce("RSRUpdate DC Exception", ex);
        }
    }

    private static void ParseLordActivatedHighlightUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle || !_isActivatedThisCycle)
            return;

        // Handle Teaching Mode Highlighting
        if (Service.Config.TeachingMode && ActionUpdater.NextAction is not null)
        {
            try
            {
                IAction nextAction = ActionUpdater.NextAction;
                HotbarID? hotbar = null;
                if (nextAction is IBaseItem item)
                {
                    hotbar = new HotbarID(HotbarSlotType.Item, item.ID);
                }
                else if (nextAction is IBaseAction baseAction)
                {
                    hotbar = baseAction.Action.ActionCategory.RowId is 10 or 11
                            ? GetGeneralActionHotbarID(baseAction)
                            : new HotbarID(HotbarSlotType.Action, baseAction.AdjustedID);
                }

                if (hotbar.HasValue)
                {
                    _ = HotbarHighlightManager.HotbarIDs.Add(hotbar.Value);
                }
            }
            catch (Exception ex)
            {
                LogOnce("Hotbar Highlighting Exception", ex);
            }
        }

        // Apply reddening of disabled actions on hotbars alongside highlight
        if (Service.Config.ReddenDisabledHotbarActions)
        {
            try
            {
                HotbarDisabledColor.ApplyFrame();
            }
            catch (Exception ex)
            {
                LogOnce("Hotbar Disabled Redden Exception", ex);
            }
        }
    }

    private static void ParseLordCommonUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        try
        {
            // Update various combat tracking parameters,
            ActionUpdater.UpdateCombatInfo();

            // Update timing tweaks
            ActionManagerEx.Instance.UpdateTweaks();

            // Update displaying the additional UI windows
            RotationSolverPlugin.UpdateDisplayWindow();
        }
        catch (Exception ex)
        {
            LogOnce("CommonUpdate Exception", ex);
        }
    }

    private static void ParseLordCleanupUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        try
        {
            // Handle system warnings
            if (DataCenter.SystemWarnings.Count > 0)
            {
                DateTime now = DateTime.Now;
                List<string> keysToRemove = [];
                foreach (KeyValuePair<string, DateTime> kvp in DataCenter.SystemWarnings)
                {
                    if (kvp.Value + TimeSpan.FromMinutes(10) < now)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (string key in keysToRemove)
                {
                    _ = DataCenter.SystemWarnings.Remove(key);
                }
            }

            // Clear old VFX data
            if (!DataCenter.VfxDataQueue.IsEmpty)
            {
                while (DataCenter.VfxDataQueue.TryPeek(out var vfx) && vfx.TimeDuration > TimeSpan.FromSeconds(6))
                {
                    _ = DataCenter.VfxDataQueue.TryDequeue(out _);
                }
            }
        }
        catch (Exception ex)
        {
            LogOnce("CleanupUpdate Exception", ex);
        }
    }

    private static void ParseLordRotationAndStateUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        try
        {
            // Change loaded rotation based on job
            RotationUpdater.UpdateRotation();

            // Change RS state
            RSCommands.UpdateRotationState();

            if (Service.Config.TeachingMode)
            {
                try
                {
                    HotbarHighlightManager.UpdateSettings();
                }
                catch (Exception ex)
                {
                    LogOnce("HotbarHighlightManager.UpdateSettings Exception", ex);
                }
            }
        }
        catch (Exception ex)
        {
            LogOnce("RotationAndStateUpdate Exception", ex);
        }
    }

    private static void ParseLordMiscAndTargetFreelyUpdate(IFramework framework)
    {
        if (!_shouldRunThisCycle)
            return;

        try
        {
            MiscUpdater.UpdateMisc();

            // Update manual target override detection
            UpdateManualTargetOverride();

            if (Service.Config.TargetFreely && !DataCenter.IsPvP && DataCenter.State)
            {
                // Skip auto-targeting if manual override is active
                if (DataCenter.ManualTargetOverride)
                    return;

                IAction? nextAction2 = ActionUpdater.NextAction;
                if (nextAction2 == null)
                {
                    if (Player.Object != null && Svc.Targets.Target == null)
                    {
                        // Try to find the closest enemy and target it
                        IBattleChara? closestEnemy = null;
                        float minDistance = float.MaxValue;

                        // Check Priority Targets first
                        IBattleChara? priorityEnemy = null;
                        int maxPriority = int.MinValue;

                        foreach (var enemy in DataCenter.AllHostileTargets)
                        {
                            if (enemy == null || !enemy.IsEnemy() || enemy == Player.Object)
                                continue;

                            // Priority Logic
                            if (Service.Config.PriorityTargets.Count > 0)
                            {
                                foreach (var pConfig in Service.Config.PriorityTargets)
                                {
                                    if (!pConfig.Enabled) continue;

                                    bool match = false;
                                    
                                    // Check by ID if provided
                                    if (pConfig.ObjectId != 0)
                                    {
                                        if (enemy.BaseId == pConfig.ObjectId || enemy.GameObjectId == pConfig.ObjectId)
                                            match = true;
                                    }
                                    // Check by Name if provided
                                    else if (!string.IsNullOrEmpty(pConfig.Name))
                                    {
                                        string enemyName = enemy.Name.ToString();
                                        if (pConfig.FullMatch)
                                        {
                                            if (enemyName.Equals(pConfig.Name, StringComparison.OrdinalIgnoreCase))
                                                match = true;
                                        }
                                        else
                                        {
                                            if (enemyName.Contains(pConfig.Name, StringComparison.OrdinalIgnoreCase))
                                                match = true;
                                        }
                                    }

                                    if (match)
                                    {
                                        if (pConfig.Priority > maxPriority)
                                        {
                                            maxPriority = pConfig.Priority;
                                            priorityEnemy = enemy;
                                        }
                                        else if (pConfig.Priority == maxPriority)
                                        {
                                            // Tie-breaker: closest distance
                                            float distP = Vector3.Distance(Player.Object.Position, enemy.Position);
                                            float distExisting = priorityEnemy != null ? Vector3.Distance(Player.Object.Position, priorityEnemy.Position) : float.MaxValue;
                                            if (distP < distExisting)
                                            {
                                                priorityEnemy = enemy;
                                            }
                                        }
                                    }
                                }
                            }

                            float distance = Vector3.Distance(Player.Object.Position, enemy.Position);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestEnemy = enemy;
                            }
                        }

                        // Use priority enemy if found, otherwise closest
                        var finalTarget = priorityEnemy ?? closestEnemy;

                        if (finalTarget != null)
                        {
                            if (!Service.Config.TargetDelayEnable)
                            {
                                BeginParseLordTargetChange();
                                Svc.Targets.Target = finalTarget;
                                EndParseLordTargetChange();
                            }
                            // Respect TargetDelay before auto-targeting the closest enemy
                            if (Service.Config.TargetDelayEnable)
                            {
                                RSCommands.SetTargetWithDelay(finalTarget);
                            }
                            PluginLog.Information($"Targeting {finalTarget}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogOnce("Secondary ParseLordUpdate Exception", ex);
        }
    }

    private static void ParseLordResetUpdate(IFramework framework)

    {
        if (!_shouldRunThisCycle)
            return;

        _shouldRunThisCycle = false;
    }

    private static HotbarID? GetGeneralActionHotbarID(IBaseAction baseAction)
    {
        Lumina.Excel.ExcelSheet<GeneralAction> generalActions = Svc.Data.GetExcelSheet<GeneralAction>();
        if (generalActions == null)
        {
            return null;
        }

        foreach (GeneralAction gAct in generalActions)
        {
            if (gAct.Action.RowId == baseAction.ID)
            {
                return new HotbarID(HotbarSlotType.GeneralAction, gAct.RowId);
            }
        }

        return null;
    }

    private static void LogOnce(string context, Exception ex)
    {
        if (_threadException == ex)
        {
            return;
        }

        _threadException = ex;
        PluginLog.Error($"{context}: {ex.Message}");
        if (Service.Config.InDebug)
        {
            _ = BasicWarningHelper.AddSystemWarning(context);
        }
    }

    public static void Dispose()
    {
        Svc.Framework.Update -= ParseLordGateUpdate;
        Svc.Framework.Update -= ParseLordTeachingClearUpdate;
        Svc.Framework.Update -= ParseLordInvalidUpdate;
        Svc.Framework.Update -= ParseLordActivatedCoreUpdate;
        Svc.Framework.Update -= ParseLordActivatedHighlightUpdate;
        Svc.Framework.Update -= ParseLordCommonUpdate;
        Svc.Framework.Update -= ParseLordCleanupUpdate;
        Svc.Framework.Update -= ParseLordRotationAndStateUpdate;
        Svc.Framework.Update -= ParseLordMiscAndTargetFreelyUpdate;
        Svc.Framework.Update -= ParseLordResetUpdate;


        MiscUpdater.Dispose();
        ActionSequencerUpdater.SaveFiles();
        ActionUpdater.ClearNextAction();
    }
}
