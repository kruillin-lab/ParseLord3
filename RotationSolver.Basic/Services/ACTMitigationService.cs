using System.Collections.Concurrent;
using ECommons.Logging;
using RotationSolver.Basic.Data;
using RotationSolver.Basic.Helpers;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver.Basic.Services;

/// <summary>
/// Service that processes ACT mechanic events and integrates with Smart Mitigation
/// to enable predictive defensive cooldown usage.
/// </summary>
public static class ACTMitigationService
{
    private static readonly ConcurrentDictionary<string, ACTMechanicEvent> _activeMechanics = new();
    private static readonly ConcurrentQueue<ACTMechanicEvent> _recentMechanics = new();

    /// <summary>
    /// Records a mechanic event from ACT.
    /// </summary>
    public static void RecordMechanic(ACTMechanicEvent mechanic)
    {
        if (!Service.Config.EnableActCallouts)
        {
            return;
        }

        var key = $"{mechanic.Name}_{mechanic.Source}_{mechanic.ReceivedAt:HHmmss}";
        _activeMechanics[key] = mechanic;
        _recentMechanics.Enqueue(mechanic);

        // Clean up old mechanics
        CleanupOldMechanics();

        if (Service.Config.EnableDebugTrace)
        {
            Service.LogDebug($"[ParseLord3] ACT Mitigation: Recorded {mechanic.Type} '{mechanic.Name}' " +
                           $"targeting {mechanic.Target} in {mechanic.TimeUntilImpact}s");
        }
    }

    /// <summary>
    /// Gets the most relevant active mechanic for mitigation purposes.
    /// </summary>
    public static ACTMechanicEvent? GetActiveMechanicForMitigation()
    {
        if (!Service.Config.EnableActCallouts)
        {
            return null;
        }

        CleanupOldMechanics();

        var playerName = Player.Object?.Name.ToString() ?? "";

        // Priority 1: Tankbuster targeting the player
        var tankbuster = _activeMechanics.Values
            .Where(m => m.Type == MechanicType.Tankbuster &&
                       m.IsValid &&
                       m.RemainingTime > 1.0f &&
                       m.RemainingTime < 6.0f &&
                       (m.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase) ||
                        m.Target.Equals("tank", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.RemainingTime)
            .FirstOrDefault();

        if (tankbuster != null)
        {
            return tankbuster;
        }

        // Priority 2: Targeted AoE on player
        var targetedAoE = _activeMechanics.Values
            .Where(m => m.Type == MechanicType.TargetedAoE &&
                       m.IsValid &&
                       m.RemainingTime > 1.0f &&
                       m.RemainingTime < 4.0f &&
                       m.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.RemainingTime)
            .FirstOrDefault();

        if (targetedAoE != null)
        {
            return targetedAoE;
        }

        // Priority 3: Raid-wide / Party AoE
        var raidWide = _activeMechanics.Values
            .Where(m => (m.Type == MechanicType.RaidWide || m.Type == MechanicType.PartyAoE) &&
                       m.IsValid &&
                       m.RemainingTime > 0.5f &&
                       m.RemainingTime < 5.0f)
            .OrderBy(m => m.RemainingTime)
            .FirstOrDefault();

        return raidWide;
    }

    /// <summary>
    /// Checks if a tankbuster is incoming for the player.
    /// </summary>
    public static bool IsTankbusterIncoming(float withinSeconds = 5.0f)
    {
        if (!Service.Config.EnableActCallouts)
        {
            return false;
        }

        var playerName = Player.Object?.Name.ToString() ?? "";

        return _activeMechanics.Values.Any(m =>
            m.Type == MechanicType.Tankbuster &&
            m.IsValid &&
            m.RemainingTime > 1.0f &&
            m.RemainingTime <= withinSeconds &&
            (m.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase) ||
             m.Target.Equals("tank", StringComparison.OrdinalIgnoreCase) ||
             string.IsNullOrEmpty(m.Target)));
    }

    /// <summary>
    /// Checks if party-wide AoE damage is incoming.
    /// </summary>
    public static bool IsPartyAoEIncoming(float withinSeconds = 5.0f)
    {
        if (!Service.Config.EnableActCallouts)
        {
            return false;
        }

        return _activeMechanics.Values.Any(m =>
            (m.Type == MechanicType.RaidWide || m.Type == MechanicType.PartyAoE) &&
            m.IsValid &&
            m.RemainingTime > 0.5f &&
            m.RemainingTime <= withinSeconds);
    }

    /// <summary>
    /// Gets the predicted damage level based on ACT mechanic data.
    /// </summary>
    public static MitigationHelper.DamageLevel GetPredictedDamageLevel()
    {
        var mechanic = GetActiveMechanicForMitigation();
        if (mechanic == null)
        {
            return MitigationHelper.DamageLevel.None;
        }

        return mechanic.Type switch
        {
            MechanicType.Tankbuster => mechanic.Severity switch
            {
                MechanicSeverity.Lethal => MitigationHelper.DamageLevel.Emergency,
                MechanicSeverity.Heavy => MitigationHelper.DamageLevel.Heavy,
                MechanicSeverity.Moderate => MitigationHelper.DamageLevel.Moderate,
                _ => MitigationHelper.DamageLevel.Light
            },
            MechanicType.RaidWide or MechanicType.PartyAoE => mechanic.Severity switch
            {
                MechanicSeverity.Lethal => MitigationHelper.DamageLevel.Heavy,
                MechanicSeverity.Heavy => MitigationHelper.DamageLevel.Moderate,
                MechanicSeverity.Moderate => MitigationHelper.DamageLevel.Light,
                _ => MitigationHelper.DamageLevel.None
            },
            MechanicType.StackMarker => MitigationHelper.DamageLevel.Moderate,
            MechanicType.Cleave => MitigationHelper.DamageLevel.Moderate,
            _ => MitigationHelper.DamageLevel.Light
        };
    }

    /// <summary>
    /// Gets the predicted damage type based on ACT mechanic data.
    /// </summary>
    public static MitigationHelper.DamageType GetPredictedDamageType()
    {
        var mechanic = GetActiveMechanicForMitigation();
        if (mechanic == null)
        {
            return MitigationHelper.DamageType.Unknown;
        }

        return mechanic.DamageType switch
        {
            MechanicDamageType.Physical => MitigationHelper.DamageType.Physical,
            MechanicDamageType.Magical => MitigationHelper.DamageType.Magical,
            MechanicDamageType.Hybrid => MitigationHelper.DamageType.Mixed,
            _ => MitigationHelper.DamageType.Unknown
        };
    }

    /// <summary>
    /// Checks if mitigation should be used based on ACT predictive data.
    /// </summary>
    public static bool ShouldUsePredictiveMitigation(out MitigationHelper.DamageLevel damageLevel)
    {
        damageLevel = MitigationHelper.DamageLevel.None;

        if (!Service.Config.EnableActCallouts)
        {
            return false;
        }

        var mechanic = GetActiveMechanicForMitigation();
        if (mechanic == null)
        {
            return false;
        }

        // Only act if mechanic is imminent but not too close
        if (mechanic.RemainingTime < 0.5f || mechanic.RemainingTime > 6.0f)
        {
            return false;
        }

        damageLevel = GetPredictedDamageLevel();
        return damageLevel >= MitigationHelper.DamageLevel.Light;
    }

    /// <summary>
    /// Gets all active mechanics for display/debugging.
    /// </summary>
    public static IReadOnlyCollection<ACTMechanicEvent> GetActiveMechanics()
    {
        CleanupOldMechanics();
        return _activeMechanics.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Clears all recorded mechanics (useful on zone change or wipe).
    /// </summary>
    public static void ClearAll()
    {
        _activeMechanics.Clear();
        while (_recentMechanics.TryDequeue(out _)) { }

        if (Service.Config.EnableDebugTrace)
        {
            Service.LogDebug("[ParseLord3] ACT Mitigation: Cleared all mechanics");
        }
    }

    private static void CleanupOldMechanics()
    {
        var keysToRemove = _activeMechanics
            .Where(kvp => !kvp.Value.IsValid)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _activeMechanics.TryRemove(key, out _);
        }

        // Also trim the recent queue
        while (_recentMechanics.Count > 20 && _recentMechanics.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Records a mechanic from raw JSON (for IPC/external calls).
    /// </summary>
    public static void RecordMechanicFromJson(string json)
    {
        try
        {
            var mechanic = System.Text.Json.JsonSerializer.Deserialize<ACTMechanicEvent>(json);
            if (mechanic != null)
            {
                RecordMechanic(mechanic);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"[ParseLord3] Failed to parse mechanic JSON: {ex.Message}");
        }
    }
}
