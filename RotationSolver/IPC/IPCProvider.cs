using ECommons.EzIpcManager;
using ECommons.Logging;
using RotationSolver.Commands;

namespace RotationSolver.IPC
{
    /// <summary>
    /// Provides IPC methods for external plugins or tools to interact with RotationSolver.
    /// All public methods marked with <see cref="EzIPCAttribute"/> are exposed for IPC calls.
    /// </summary>
    internal class IPCProvider
    {
        /// <summary>
        /// Initializes the IPC provider and registers IPC methods with the specified prefix.
        /// </summary>
        internal IPCProvider()
        {
            _ = EzIPC.Init(this, prefix: "RotationSolverReborn");
        }

        /// <summary>
        /// IPC method for testing connectivity and logging a debug message.
        /// </summary>
        /// <param name="param">A string parameter to include in the debug log for verification.</param>
        [EzIPC]
        public void Test(string param)
        {
            PluginLog.Debug($"IPC Test! Param:{param}");
        }

        /// <summary>
        /// Adds a name ID to the prioritized list via IPC.
        /// </summary>
        /// <param name="nameId">The unique identifier to add to the prioritized name IDs collection.</param>
        [EzIPC]
        public void AddPriorityNameID(uint nameId)
        {
            if (DataCenter.PrioritizedNameIds != null)
            {
                DataCenter.PrioritizedNameIds.Add(nameId);
                PluginLog.Debug($"IPC AddPriorityNameID was called. NameID:{nameId}");
            }
            else
            {
                PluginLog.Error("DataCenter.PrioritizedNameIds is null.");
            }
        }

        /// <summary>
        /// Removes a name ID from the prioritized list via IPC.
        /// </summary>
        /// <param name="nameId">The unique identifier to remove from the prioritized name IDs collection.</param>
        [EzIPC]
        public void RemovePriorityNameID(uint nameId)
        {
            if (DataCenter.PrioritizedNameIds != null)
            {
                if (DataCenter.PrioritizedNameIds.Contains(nameId))
                {
                    _ = DataCenter.PrioritizedNameIds.Remove(nameId);
                    PluginLog.Debug($"IPC RemovePriorityNameID was called. NameID:{nameId}");
                }
                else
                {
                    PluginLog.Warning($"IPC RemovePriorityNameID was called but NameID:{nameId} was not found.");
                }
            }
            else
            {
                PluginLog.Error("DataCenter.PrioritizedNameIds is null.");
            }
        }

        /// <summary>
        /// Adds a name ID to the blacklist via IPC, preventing actions for this identifier.
        /// </summary>
        /// <param name="nameId">The unique identifier to add to the blacklist collection.</param>
        [EzIPC]
        public void AddBlacklistNameID(uint nameId)
        {
            if (DataCenter.BlacklistedNameIds != null)
            {
                DataCenter.BlacklistedNameIds.Add(nameId);
                PluginLog.Debug($"IPC AddBlacklistNameID was called. NameID:{nameId}");
            }
            else
            {
                PluginLog.Error("DataCenter.BlacklistedNameIds is null.");
            }
        }

        /// <summary>
        /// Removes a name ID from the blacklist via IPC, allowing actions for this identifier.
        /// </summary>
        /// <param name="nameId">The unique identifier to remove from the blacklist collection.</param>
        [EzIPC]
        public void RemoveBlacklistNameID(uint nameId)
        {
            if (DataCenter.BlacklistedNameIds != null)
            {
                if (DataCenter.BlacklistedNameIds.Contains(nameId))
                {
                    _ = DataCenter.BlacklistedNameIds.Remove(nameId);
                    PluginLog.Debug($"IPC RemoveBlacklistNameID was called. NameID:{nameId}");
                }
                else
                {
                    PluginLog.Warning($"IPC RemoveBlacklistNameID was called but NameID:{nameId} was not found.");
                }
            }
            else
            {
                PluginLog.Error("DataCenter.BlacklistedNameIds is null.");
            }
        }

        /// <summary>
        /// Changes the operating mode of the plugin via IPC.
        /// </summary>
        /// <param name="stateCommand">
        /// The <see cref="StateCommandType"/> value specifying the desired operating mode, such as Off, Auto, or Manual.
        /// </param>
        [EzIPC]
        public void ChangeOperatingMode(StateCommandType stateCommand)
        {
            RSCommands.UpdateState(stateCommand, (JobRole)DataCenter.Job);
            PluginLog.Debug($"IPC ChangeOperatingMode was called. StateCommand:{stateCommand}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateCommand">
        /// The <see cref="StateCommandType"/> value specifying the desired operating mode, such as Off, Auto, or Manual.
        /// </param>
        /// <param name="targetingType">
        /// The <see cref="TargetingType"/> value specifying the desired operating mode, such as Big, LowHP, or Nearest.
        /// </param>
        [EzIPC]
        public void AutodutyChangeOperatingMode(StateCommandType stateCommand, TargetingType targetingType)
        {
            RSCommands.AutodutyUpdateState(stateCommand, (JobRole)DataCenter.Job, targetingType);
            PluginLog.Debug($"IPC AutodutyChangeOperatingMode was called. StateCommand:{stateCommand} TargetingType:{targetingType}");
        }

        /// <summary>
        /// Triggers a special state in the plugin via IPC, such as healing, defense, or movement.
        /// </summary>
        /// <param name="specialCommand">
        /// The <see cref="SpecialCommandType"/> value representing the special state to activate (e.g., HealArea, DefenseSingle, Burst).
        /// </param>
        [EzIPC]
        public void TriggerSpecialState(SpecialCommandType specialCommand)
        {
            DataCenter.SpecialType = specialCommand;
            PluginLog.Debug($"IPC TriggerSpecialState was called. SpecialCommand:{specialCommand}");
        }

        /// <summary>
        /// Changes a plugin setting or triggers an auxiliary command via IPC.
        /// </summary>
        /// <param name="otherType">
        /// The <see cref="OtherCommandType"/> category of command to execute, such as DoActions, ToggleActions, Settings, NextAction, Rotations, or DutyRotations.
        /// </param>
        /// <param name="str">
        /// The string value representing the modification or parameter for the command.
        /// </param>
        [EzIPC]
        public void OtherCommand(OtherCommandType otherType, string str)
        {
            RSCommands.DoOtherCommand(otherType, str);
            PluginLog.Debug($"IPC DoOtherCommand was called. OtherCommandType:{otherType}, String:{str},");
        }

        /// <summary>
        /// Executes an action command via IPC, allowing external plugins or tools to trigger a specific action with a timing parameter.
        /// </summary>
        /// <param name="action">
        /// The name of the action to execute. This needs to be the exact name of the action.
        /// </param>
        /// <param name="time">
        /// The time value that the window to use the action is open (typcially 5 seconds).
        /// </param>
        [EzIPC]
        public void ActionCommand(string action, float time)
        {
            string combinedString = $"{action}-{time}";

            RSCommands.DoActionCommand($"{combinedString}");
            PluginLog.Debug($"IPC ActionCommand was called. Action Name:{action}, Time:{time}");
        }

        // ACT Integration IPC Methods

        /// <summary>
        /// Records a mechanic event from ACT via IPC for predictive mitigation.
        /// </summary>
        /// <param name="mechanicJson">JSON string containing mechanic event data.</param>
        [EzIPC]
        public void ACTRecordMechanic(string mechanicJson)
        {
            try
            {
                Basic.Services.ACTMitigationService.RecordMechanicFromJson(mechanicJson);
                PluginLog.Debug("IPC ACTRecordMechanic was called.");
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"IPC ACTRecordMechanic failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Records a tankbuster warning from ACT via IPC.
        /// </summary>
        /// <param name="source">The source of the tankbuster (boss name).</param>
        /// <param name="target">The target of the tankbuster (player name).</param>
        /// <param name="timeUntilImpact">Time until the tankbuster resolves (seconds).</param>
        [EzIPC]
        public void ACTTankbusterWarning(string source, string target, float timeUntilImpact)
        {
            try
            {
                var mechanic = new Basic.Data.ACTMechanicEvent
                {
                    Type = Basic.Data.MechanicType.Tankbuster,
                    Name = "Tankbuster",
                    Source = source,
                    Target = target,
                    TimeUntilImpact = timeUntilImpact,
                    DamageType = Basic.Data.MechanicDamageType.Unknown,
                    Severity = Basic.Data.MechanicSeverity.Heavy
                };
                Basic.Services.ACTMitigationService.RecordMechanic(mechanic);
                PluginLog.Debug($"IPC ACTTankbusterWarning: {source} -> {target} in {timeUntilImpact}s");
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"IPC ACTTankbusterWarning failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Records a raid-wide AoE warning from ACT via IPC.
        /// </summary>
        /// <param name="source">The source of the AoE.</param>
        /// <param name="timeUntilImpact">Time until the AoE resolves (seconds).</param>
        [EzIPC]
        public void ACTRaidWideWarning(string source, float timeUntilImpact)
        {
            try
            {
                var mechanic = new Basic.Data.ACTMechanicEvent
                {
                    Type = Basic.Data.MechanicType.RaidWide,
                    Name = "Raid-wide",
                    Source = source,
                    Target = "party",
                    TimeUntilImpact = timeUntilImpact,
                    DamageType = Basic.Data.MechanicDamageType.Unknown,
                    Severity = Basic.Data.MechanicSeverity.Moderate
                };
                Basic.Services.ACTMitigationService.RecordMechanic(mechanic);
                PluginLog.Debug($"IPC ACTRaidWideWarning: {source} in {timeUntilImpact}s");
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"IPC ACTRaidWideWarning failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all ACT mechanic events (e.g., on wipe or zone change).
        /// </summary>
        [EzIPC]
        public void ACTClearMechanics()
        {
            Basic.Services.ACTMitigationService.ClearAll();
            PluginLog.Debug("IPC ACTClearMechanics was called.");
        }
    }
}