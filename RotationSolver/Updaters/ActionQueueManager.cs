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

        private static IGameObject? GetFirstMemberByJob(Job job)
        {
            if (Svc.Party.Length > 0)
            {
                foreach (var member in Svc.Party)
                {
                    if (member.GameObject is IBattleChara bc && bc.ClassJob.RowId == (uint)job)
                        return bc;
                }
            }
            return null;
        }

        private static IGameObject? GetLowestHpMember(JobRole role)
        {
            IGameObject? best = null;
            float minHp = float.MaxValue;
            
            // Check Party
            if (Svc.Party.Length > 0)
            {
                foreach (var member in Svc.Party)
                {
                    if (member.GameObject is IBattleChara bc)
                    {
                        if (role != JobRole.None)
                        {
                            if (role == (JobRole)100) // DPS placeholder
                            {
                                if (!bc.IsJobCategory(JobRole.Melee) && !bc.IsJobCategory(JobRole.RangedPhysical) && !bc.IsJobCategory(JobRole.RangedMagical)) continue;
                            }
                            else if (!bc.IsJobCategory(role)) 
                            {
                                continue;
                            }
                        }

                        float hp = (float)bc.CurrentHp / bc.MaxHp;
                        if (hp < minHp)
                        {
                            minHp = hp;
                            best = bc;
                        }
                    }
                }
            }
            else
            {
                // Solo check (Player)
                if (Player.Object is IBattleChara p && (role == JobRole.None || p.IsJobCategory(role)))
                    best = p;
            }
            return best;
        }

        private static IGameObject? GetNearestMember(bool party, bool furthest = false)
        {
            IGameObject? best = null;
            float bestDist = furthest ? float.MinValue : float.MaxValue;
            if (Player.Object == null) return null;
            Vector2 pPos = new Vector2(Player.Object.Position.X, Player.Object.Position.Z);

            if (party)
            {
                foreach (var member in Svc.Party)
                {
                    if (member.GameObject == null || member.GameObject == Player.Object) continue;
                    float dist = Vector2.Distance(pPos, new Vector2(member.GameObject.Position.X, member.GameObject.Position.Z));
                    bool update = furthest ? dist > bestDist : dist < bestDist;
                    if (update)
                    {
                        bestDist = dist;
                        best = member.GameObject;
                    }
                }
            }
            else // Enemy
            {
                // Use DataCenter.HostileTargets?
                foreach (var obj in Svc.Objects)
                {
                    if (obj is IBattleChara bc && bc.IsEnemy() && !bc.IsDead)
                    {
                        float dist = Vector2.Distance(pPos, new Vector2(bc.Position.X, bc.Position.Z));
                        bool update = furthest ? dist > bestDist : dist < bestDist;
                        if (update)
                        {
                            bestDist = dist;
                            best = bc;
                        }
                    }
                }
            }
            return best;
        }

        private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            // Action Stacks Logic (ReactionEx Style)
            if (Service.Config.ActionStacks.Count > 0 && actionType == 1 && Player.Available)
            {
                uint adjusted = Service.GetAdjustedActionId(actionID);
                var stack = Service.Config.ActionStacks.FirstOrDefault(s => s.TriggerActionId == adjusted || s.TriggerActionId == actionID);
                if (stack != null)
                {
                    foreach (var item in stack.Items)
                    {
                        if (!item.Enabled) continue;

                        IGameObject? target = null;
                        switch (item.Target)
                        {
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Target: 
                                // Resolve from original target ID
                                if (targetObjectID != 0 && targetObjectID != 3758096384UL) // INVALID_TARGET_ID?
                                    target = Svc.Objects.SearchById(targetObjectID);
                                else
                                    target = Svc.Targets.Target; // Fallback?
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Self: target = Player.Object; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Focus: target = Svc.Targets.FocusTarget; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Mouseover: target = Svc.Targets.MouseOverTarget; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.TargetOfTarget: 
                                target = Svc.Targets.Target?.TargetObject; 
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.UITarget: 
                                target = Svc.Targets.Target; 
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Tank:
                                if (Svc.Party.Length > 0)
                                {
                                    // Find first tank that isn't me? Or any tank?
                                    // Usually "Tank" implies "Main Tank" or "Co-Tank".
                                    // I'll pick the first tank found in party list.
                                    foreach (var member in Svc.Party)
                                    {
                                        if (member.GameObject is IBattleChara tankChara && tankChara.IsJobCategory(JobRole.Tank))
                                        {
                                            target = tankChara;
                                            break;
                                        }
                                    }
                                }
                                break;
                            // Party 1-8
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party1: if (Svc.Party.Length > 0) target = Svc.Party[0]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party2: if (Svc.Party.Length > 1) target = Svc.Party[1]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party3: if (Svc.Party.Length > 2) target = Svc.Party[2]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party4: if (Svc.Party.Length > 3) target = Svc.Party[3]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party5: if (Svc.Party.Length > 4) target = Svc.Party[4]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party6: if (Svc.Party.Length > 5) target = Svc.Party[5]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party7: if (Svc.Party.Length > 6) target = Svc.Party[6]?.GameObject; break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Party8: if (Svc.Party.Length > 7) target = Svc.Party[7]?.GameObject; break;

                            // Advanced Types
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LowestHpParty:
                                target = GetLowestHpMember(JobRole.None);
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LowestHpTank:
                                target = GetLowestHpMember(JobRole.Tank);
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LowestHpHealer:
                                target = GetLowestHpMember(JobRole.Healer);
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LowestHpDps:
                                target = GetLowestHpMember((JobRole)100);
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.NearestParty:
                                target = GetNearestMember(true); // True = Party
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.FurthestParty:
                                target = GetNearestMember(true, true); // Party, Furthest
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.NearestEnemy:
                                target = GetNearestMember(false); // False = Enemy
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.FurthestEnemy:
                                target = GetNearestMember(false, true); // Enemy, Furthest
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Owner:
                                if (Player.Object is IBattleChara pChara) target = pChara.OwnerId != 3758096384UL ? Svc.Objects.SearchById(pChara.OwnerId) : null;
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Pet:
                                // Finding pet is tricky without PetManager, but basic BattleChara might have Minion/Pet ID?
                                // Usually we search objects for BattleNpc owned by Player.
                                target = Svc.Objects.FirstOrDefault(o => o is IBattleChara bc && bc.OwnerId == Player.Object.GameObjectId);
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LastTarget:
                                // Placeholder: Needs tracking logic. Using Target for now to avoid null.
                                target = Svc.Targets.Target; 
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LastEnemy:
                                // Placeholder
                                target = Svc.Targets.Target;
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.LastAttacker:
                                // Placeholder (Need memory or Dalamud API if available)
                                target = Svc.Targets.Target; 
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.SoftTarget:
                                target = Svc.Targets.SoftTarget;
                                break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.Companion:
                                target = Svc.Buddies.PetBuddy?.GameObject;
                                break;
                            
                            // Jobs
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.PLD: target = GetFirstMemberByJob(Job.PLD); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.WAR: target = GetFirstMemberByJob(Job.WAR); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.DRK: target = GetFirstMemberByJob(Job.DRK); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.GNB: target = GetFirstMemberByJob(Job.GNB); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.WHM: target = GetFirstMemberByJob(Job.WHM); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.SCH: target = GetFirstMemberByJob(Job.SCH); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.AST: target = GetFirstMemberByJob(Job.AST); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.SGE: target = GetFirstMemberByJob(Job.SGE); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.MNK: target = GetFirstMemberByJob(Job.MNK); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.DRG: target = GetFirstMemberByJob(Job.DRG); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.NIN: target = GetFirstMemberByJob(Job.NIN); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.SAM: target = GetFirstMemberByJob(Job.SAM); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.RPR: target = GetFirstMemberByJob(Job.RPR); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.VPR: target = GetFirstMemberByJob(Job.VPR); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.BRD: target = GetFirstMemberByJob(Job.BRD); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.MCH: target = GetFirstMemberByJob(Job.MCH); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.DNC: target = GetFirstMemberByJob(Job.DNC); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.BLM: target = GetFirstMemberByJob(Job.BLM); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.SMN: target = GetFirstMemberByJob(Job.SMN); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.RDM: target = GetFirstMemberByJob(Job.RDM); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.PCT: target = GetFirstMemberByJob(Job.PCT); break;
                            case RotationSolver.Basic.Configuration.ActionStackTargetType.BLU: target = GetFirstMemberByJob(Job.BLU); break;
                        }

                        if (target == null) continue;

                        // HP Check
                        if (item.HpRatio < 1.0f && target is IBattleChara bc)
                        {
                            if (bc.CurrentHp == 0 || (float)bc.CurrentHp / bc.MaxHp > item.HpRatio) continue;
                        }

                        // Status Check
                        if (item.StatusId != 0 && target is IBattleChara bc2)
                        {
                            bool has = bc2.HasStatus(true, (StatusID)item.StatusId) || bc2.HasStatus(false, (StatusID)item.StatusId);
                            if (item.MissingStatus && has) continue;
                            if (!item.MissingStatus && !has) continue;
                        }

                        // Execute Stack Item
                        uint newActionID = item.ActionId != 0 ? item.ActionId : actionID;
                        ulong newTargetID = target.GameObjectId;

                        // Check Range
                        if (stack.CheckRange && ActionManager.GetActionInRangeOrLoS(newActionID, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)Player.Object.Address, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target.Address) != 0) continue;

                        // Check Cooldown
                        if (stack.CheckCooldown)
                        {
                            // 0 = Ready.
                            if (ActionManager.Instance()->GetActionStatus(ActionType.Action, newActionID, newTargetID) != 0) continue;
                        }

                        // Check Cooldown
                        if (stack.CheckCooldown)
                        {
                            // 0 = Ready.
                            if (ActionManager.Instance()->GetActionStatus(ActionType.Action, newActionID, newTargetID) != 0) continue;
                        }

                        PluginLog.Debug($"[ActionQueueManager] Stack Triggered: {actionID} -> {newActionID} on {newTargetID:X}");

                        if (_useActionHook?.Original != null)
                        {
                            bool result = _useActionHook.Original(actionManager, actionType, newActionID, newTargetID, param, useType, pvp, isGroundTarget);
                            if (result) return true; // Action executed
                            
                            // If action failed and BlockOriginalOnFail is false, continue loop? No, stack logic says "Try this".
                            // If we tried and it failed (returned false), should we try next item?
                            // ReactionEx: "Fail if..." means "Skip item if condition fail". We already checked conditions (Range, CD).
                            // So if we are here, we attempt to use it.
                            
                            // If BlockOriginalOnFail is set, we return false (block original) even if this failed?
                            // No, if we successfully *triggered* the hook, we are done.
                            return true;
                        }
                        return true;
                    }
                    
                    // If we exit loop without finding a valid item
                    if (stack.BlockOriginalOnFail)
                    {
                        return false; // Block original action
                    }
                }
            }

            if (Player.Available && Service.Config.InterceptAction2 && DataCenter.State && DataCenter.InCombat && !DataCenter.IsPvP)
            {
                try
                {
                    if (actionType == 1 && (useType != 2 || Service.Config.InterceptMacro) && !StatusHelper.PlayerHasStatus(false, StatusHelper.RotationLockoutStatus)) // ActionType.Action == 1
                    {
                        // Always compute adjusted ID first to keep logic consistent
                        uint adjustedActionId = Service.GetAdjustedActionId(actionID);

                        if (adjustedActionId == 7419 && _useActionHook?.Original != null)
                        {
                            return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
                        }

                        if (ShouldInterceptAction(adjustedActionId))
                        {
                            // More efficient action lookup - avoid creating new collections
                            var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
                            var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

                            PluginLog.Debug($"[ActionQueueManager] Detected player input: ID={actionID}, AdjustedID={adjustedActionId}");

                            var matchingAction = ((ActionID)adjustedActionId).GetActionFromID(false, rotationActions, dutyActions);

                            if (matchingAction != null && !BlackListedInterceptActionsContains((ActionID)matchingAction.ID))
                            {
                                PluginLog.Debug($"[ActionQueueManager] Matching action decided: {matchingAction.Name} (ID: {matchingAction.ID}, AdjustedID: {matchingAction.AdjustedID})");

                                if (matchingAction.IsIntercepted && ((ActionUpdater.NextAction != null && matchingAction != ActionUpdater.NextAction) || ActionUpdater.NextAction == null))
                                {
                                    if (!matchingAction.EnoughLevel)
                                    {
                                        PluginLog.Debug($"[ActionQueueManager] Not intercepting: insufficient level for {matchingAction.Name}.");
                                    }
                                    else if (!CanInterceptAction(matchingAction))
                                    {
                                        PluginLog.Debug($"[ActionQueueManager] Not intercepting: cooldown/window check failed for {matchingAction.Name}.");
                                    }
                                    else
                                    {
                                        HandleInterceptedAction(matchingAction, actionID);
                                        return false; // Block the original action
                                    }
                                }
                                else
                                {
                                    PluginLog.Debug($"[ActionQueueManager] Not intercepting: {matchingAction.Name} is not marked for interception.");
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

            // Call original function if available, otherwise return true (allow action)
            if (_useActionHook?.Original != null)
            {
                return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
            }

            // Return true to allow the action to proceed if hook is unavailable
            return true;
        }

        private static bool ShouldInterceptAction(uint actionId)
        {
            // Note: actionId is expected to be the adjusted ID
            if (ActionUpdater.NextAction != null && actionId == ActionUpdater.NextAction.AdjustedID)
                return false;

            var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            if (actionSheet == null) return false;

            var action = actionSheet.GetRow(actionId);
            var type = ActionHelper.GetActionCate(action);

            if (type == ActionCate.None)
            {
                return false;
            }

            if (type == ActionCate.Autoattack)
            {
                return false;
            }

            if (!Service.Config.InterceptSpell2 && type == ActionCate.Spell)
            {
                return false;
            }

            if (!Service.Config.InterceptWeaponskill2 && type == ActionCate.Weaponskill)
            {
                return false;
            }

            if (!Service.Config.InterceptAbility2 && type == ActionCate.Ability)
            {
                return false;
            }

            return true;
        }

        private static bool CanInterceptAction(IAction action)
        {
            if (Service.Config.InterceptCooldown || action.Cooldown.CurrentCharges > 0) return true;

            // Guard against invalid GCD totals to avoid division by zero
            var gcdTotal = DataCenter.DefaultGCDTotal;
            if (gcdTotal <= 0)
                return false;

            // We check if the skill will fit inside the intercept action time window
            var gcdCount = (byte)Math.Floor(Service.Config.InterceptActionTime / gcdTotal);
            if (gcdCount < 1) gcdCount = 1;

            return action is IBaseAction baseAction && baseAction.Cooldown.CooldownCheck(false, gcdCount);
        }

        private static void HandleInterceptedAction(IAction matchingAction, uint actionID)
        {
            try
            {
                // Abandoned idea
                //if (matchingAction is IBaseAction baseAction && baseAction.Setting.SpecialType == SpecialActionType.HostileMovingForward)
                //{
                //    RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
                //    DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);
                //    return; // Do not queue the original action; open the special window instead
                //}

                // Use DataCenter.AddCommandAction directly instead of going through RSCommands.DoActionCommand
                // This avoids the string parsing overhead and potential format issues
                RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
                DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);

                PluginLog.Debug($"[ActionQueueManager] Intercepted and queued action: {matchingAction.Name} (OriginalID: {actionID}, AdjustedID: {matchingAction.AdjustedID})");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Error handling intercepted action {actionID}: {ex}");
            }
        }
    }
}