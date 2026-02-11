using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ExcelServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters
{
    public static class ActionQueueManager
    {
        // Action Manager Hook for intercepting user input
        private static Hook<UseActionDelegate>? _useActionHook;

        // Delegates for ActionManager functions
        private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);

        public static void Enable()
        {
            // Initialize hooks
            InitializeActionHooks();
        }

        public static void Disable()
        {
            // Dispose hooks
            DisposeActionHooks();
        }

        public static ActionID[] BlackListedInterceptActions { get; } =
        [
            // Ninja mudra actions
            ActionID.TenPvE,
            ActionID.TenPvE_18805,
            ActionID.ChiPvE,
            ActionID.ChiPvE_18806,
            ActionID.JinPvE,
            ActionID.JinPvE_18807,

            // Dancer dance steps
            ActionID.StandardStepPvE,
            ActionID.TechnicalStepPvE,
            ActionID.EmboitePvE,
            ActionID.EntrechatPvE,
            ActionID.JetePvE,
            ActionID.PirouettePvE,
            ActionID.StandardFinishPvE,
            ActionID.TechnicalFinishPvE,

            // Sage Eukrasian actions
            ActionID.EukrasiaPvE,
            ActionID.EukrasianDosisPvE,
            ActionID.EukrasianDosisIiPvE,
            ActionID.EukrasianDosisIiiPvE,
            ActionID.EukrasianDyskrasiaPvE,
            ActionID.EukrasianPrognosisPvE,
            ActionID.EukrasianPrognosisIiPvE,
        ];

        private static bool BlackListedInterceptActionsContains(ActionID id)
        {
            var arr = BlackListedInterceptActions;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == id) return true;
            }
            return false;
        }

        private static unsafe void InitializeActionHooks()
        {
            try
            {
                var useActionAddress = ActionManager.Addresses.UseAction.Value;

                _useActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>(useActionAddress, UseActionDetour);

                _useActionHook?.Enable();

                PluginLog.Debug("[ActionQueueManager] Action interception hooks initialized");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Failed to initialize action hooks: {ex}");
            }
        }

        private static void DisposeActionHooks()
        {
            try
            {
                _useActionHook?.Disable();
                _useActionHook?.Dispose();
                _useActionHook = null;

                PluginLog.Debug("[ActionQueueManager] Action interception hooks disposed");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Failed to dispose action hooks: {ex}");
            }
        }

        private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            // Action Intercept System (from original source)
            if (Player.Available && Service.Config.InterceptAction2 && DataCenter.State && DataCenter.InCombat && !DataCenter.IsPvP)
            {
                try
                {
                    if (actionType == 1 && (useType != 2 || Service.Config.InterceptMacro) && !StatusHelper.PlayerHasStatus(false, StatusHelper.RotationLockoutStatus))
                    {
                        uint adjustedActionId = Service.GetAdjustedActionId(actionID);

                        if (adjustedActionId == 7419 && _useActionHook?.Original != null)
                        {
                            return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
                        }

                        if (ShouldInterceptAction(adjustedActionId))
                        {
                            var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
                            var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];
                            var matchingAction = ((ActionID)adjustedActionId).GetActionFromID(false, rotationActions, dutyActions);

                            if (matchingAction != null && !BlackListedInterceptActionsContains((ActionID)matchingAction.ID))
                            {
                                if (matchingAction.IsIntercepted && ((ActionUpdater.NextAction != null && matchingAction != ActionUpdater.NextAction) || ActionUpdater.NextAction == null))
                                {
                                    if (matchingAction.EnoughLevel && CanInterceptAction(matchingAction))
                                    {
                                        HandleInterceptedAction(matchingAction, actionID);
                                        return false; // Block the original action
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"[ActionQueueManager] Error in UseActionDetour: {ex}");
                }
            }

            if (_useActionHook?.Original != null)
            {
                return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
            }

            return true;
        }

        private static bool ShouldInterceptAction(uint actionId)
        {
            if (ActionUpdater.NextAction != null && actionId == ActionUpdater.NextAction.AdjustedID)
                return false;

            var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            if (actionSheet == null) return false;

            var action = actionSheet.GetRow(actionId);
            var type = ActionHelper.GetActionCate(action);

            if (type == ActionCate.None || type == ActionCate.Autoattack) return false;
            if (!Service.Config.InterceptSpell2 && type == ActionCate.Spell) return false;
            if (!Service.Config.InterceptWeaponskill2 && type == ActionCate.Weaponskill) return false;
            if (!Service.Config.InterceptAbility2 && type == ActionCate.Ability) return false;

            return true;
        }

        private static bool CanInterceptAction(IAction action)
        {
            if (Service.Config.InterceptCooldown || action.Cooldown.CurrentCharges > 0) return true;

            var gcdTotal = DataCenter.DefaultGCDTotal;
            if (gcdTotal <= 0) return false;

            var gcdCount = (byte)Math.Floor(Service.Config.InterceptActionTime / gcdTotal);
            if (gcdCount < 1) gcdCount = 1;

            return action is IBaseAction baseAction && baseAction.Cooldown.CooldownCheck(false, gcdCount);
        }

        private static void HandleInterceptedAction(IAction matchingAction, uint actionID, float? expiration = null)
        {
            try
            {
                // Track intercepted actions so UI can display current & previous intercepted actions
                try
                {
                    DataCenter.CurrentInterceptedAction = matchingAction;
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"[ActionQueueManager] Failed to set intercepted action tracking: {ex}");
                }

                var expirationTime = expiration ?? Service.Config.InterceptActionTime;
                RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
                DataCenter.AddCommandAction(matchingAction, expirationTime);

                PluginLog.Debug($"[ActionQueueManager] Intercepted and queued action: {matchingAction.Name} (OriginalID: {actionID}, AdjustedID: {matchingAction.AdjustedID}, Window: {expirationTime}s)");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Error handling intercepted action {actionID}: {ex}");
            }
        }
    }
}
