using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Data;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Helper class for intelligent tank mitigation management.
/// Provides damage prediction, cooldown rotation, and smart mitigation decisions.
///
/// Features:
/// - Damage level assessment (None, Light, Moderate, Heavy, Emergency)
/// - Damage type detection (Physical, Magical, Mixed)
/// - Cooldown rotation with minimum intervals
/// - Long cooldown (90s+) overlap prevention - prevents wasting major cooldowns by overlapping them
/// - Smart invulnerability usage for emergencies
/// </summary>
public static class MitigationHelper
{
    private static DateTime _lastMitigationTime = DateTime.MinValue;
    private static DateTime _lastBigCooldownTime = DateTime.MinValue;
    private static readonly TimeSpan MinMitigationInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the incoming damage level to the player.
    /// </summary>
    public enum DamageLevel
    {
        None,
        Light,      // Auto-attacks, normal damage
        Moderate,   // Heavy auto-attacks or small busters
        Heavy,      // Tankbusters
        Emergency   // Life-threatening damage
    }

    /// <summary>
    /// Gets the type of incoming damage.
    /// </summary>
    public enum DamageType
    {
        Unknown,
        Physical,
        Magical,
        Mixed
    }

    /// <summary>
    /// Evaluates the current incoming damage level to the player.
    /// </summary>
    public static DamageLevel GetIncomingDamageLevel()
    {
        if (Player.Object == null) return DamageLevel.None;

        var healthRatio = Player.Object.GetHealthRatio();
        var isBeingAttacked = DataCenter.IsHostileCastingToTank || DataCenter.PartyMembers.Any(p =>
            p.TargetObject?.TargetObjectId == Player.Object.GameObjectId);

        // Emergency: Very low HP and taking damage
        if (healthRatio < Service.Config.HealthForDyingTanks && isBeingAttacked)
        {
            return DamageLevel.Emergency;
        }

        // Heavy: Enemy casting tankbuster or significant HP loss
        if (DataCenter.IsHostileCastingToTank)
        {
            var castingEnemies = DataCenter.AllHostileTargets.Where(e =>
                e.IsCasting && e.CastTargetObjectId == Player.Object.GameObjectId);

            if (castingEnemies.Any())
            {
                return DamageLevel.Heavy;
            }
        }

        // Moderate: Taking consistent damage or multiple enemies
        if (isBeingAttacked)
        {
            var attackingEnemies = DataCenter.AllHostileTargets.Count(e =>
                e.TargetObject?.GameObjectId == Player.Object.GameObjectId);

            if (attackingEnemies >= 3 || healthRatio < 0.8f)
            {
                return DamageLevel.Moderate;
            }

            if (attackingEnemies > 0)
            {
                return DamageLevel.Light;
            }
        }

        return DamageLevel.None;
    }

    /// <summary>
    /// Determines the type of incoming damage based on enemy casts and statuses.
    /// </summary>
    public static DamageType GetIncomingDamageType()
    {
        if (Player.Object == null) return DamageType.Unknown;

        var castingEnemies = DataCenter.AllHostileTargets.Where(e =>
            e.IsCasting && e.CastTargetObjectId == Player.Object.GameObjectId).ToList();

        if (!castingEnemies.Any())
        {
            // Assume physical if no casts (auto-attacks are physical)
            return DamageType.Physical;
        }

        // Check cast action types
        var hasPhysical = false;
        var hasMagical = false;

        foreach (var enemy in castingEnemies)
        {
            // Cast time typically indicates magical damage
            if (enemy.TotalCastTime > 0.5f)
            {
                hasMagical = true;
            }
            else
            {
                hasPhysical = true;
            }
        }

        if (hasPhysical && hasMagical)
        {
            return DamageType.Mixed;
        }
        else if (hasMagical)
        {
            return DamageType.Magical;
        }
        else if (hasPhysical)
        {
            return DamageType.Physical;
        }

        return DamageType.Unknown;
    }

    /// <summary>
    /// Checks if enough time has passed since the last mitigation to avoid spam.
    /// </summary>
    public static bool CanUseMitigation(bool isBigCooldown = false)
    {
        var now = DateTime.Now;

        if (isBigCooldown)
        {
            // Big cooldowns have a longer minimum interval (10 seconds)
            if ((now - _lastBigCooldownTime) < TimeSpan.FromSeconds(10))
            {
                return false;
            }
        }
        else
        {
            // Small cooldowns have a shorter interval (2 seconds)
            if ((now - _lastMitigationTime) < MinMitigationInterval)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Records that a mitigation ability was used.
    /// </summary>
    public static void RecordMitigationUsed(bool isBigCooldown = false)
    {
        var now = DateTime.Now;
        _lastMitigationTime = now;

        if (isBigCooldown)
        {
            _lastBigCooldownTime = now;
        }
    }

    /// <summary>
    /// Checks if the player has any active tank mitigation buffs.
    /// </summary>
    public static bool HasActiveMitigation()
    {
        return StatusHelper.PlayerHasStatus(true,
            // Tank role actions
            StatusID.Rampart,
            StatusID.Reprisal,

            // Paladin
            StatusID.Sentinel,
            StatusID.Guardian,
            StatusID.Bulwark,
            StatusID.HolySheltron,
            StatusID.Sheltron,
            StatusID.HallowedGround,
            StatusID.PassageOfArms,

            // Warrior
            StatusID.Vengeance,
            StatusID.Damnation,
            StatusID.ThrillOfBattle,
            StatusID.Bloodwhetting,
            StatusID.RawIntuition,
            StatusID.Holmgang,

            // Dark Knight
            StatusID.ShadowWall,
            StatusID.ShadowedVigil,
            StatusID.DarkMind,
            StatusID.Oblation,
            StatusID.LivingDead,

            // Gunbreaker
            StatusID.Nebula,
            StatusID.Camouflage,
            StatusID.HeartOfStone,
            StatusID.HeartOfCorundum,
            StatusID.Superbolide
        );
    }

    /// <summary>
    /// Checks if the player has a strong (30s cooldown or invuln) mitigation active.
    /// </summary>
    public static bool HasStrongMitigation()
    {
        return StatusHelper.PlayerHasStatus(true,
            // 30% mitigations
            StatusID.Sentinel,
            StatusID.Guardian,
            StatusID.Vengeance,
            StatusID.Damnation,
            StatusID.ShadowWall,
            StatusID.ShadowedVigil,
            StatusID.Nebula,

            // Invulnerabilities
            StatusID.HallowedGround,
            StatusID.Holmgang,
            StatusID.LivingDead,
            StatusID.WalkingDead,
            StatusID.Superbolide
        );
    }

    /// <summary>
    /// Checks if the player has a long cooldown (90+ seconds) mitigation active.
    /// These should not be overwritten by other long cooldowns.
    /// </summary>
    public static bool HasLongCooldownMitigationActive()
    {
        return StatusHelper.PlayerHasStatus(true,
            // 90s cooldown - Rampart (20% mitigation)
            StatusID.Rampart,

            // 120s cooldowns - Major tank mitigations (30%)
            StatusID.Sentinel,
            StatusID.Guardian,
            StatusID.Vengeance,
            StatusID.Damnation,
            StatusID.ShadowWall,
            StatusID.ShadowedVigil,
            StatusID.Nebula,

            // 240s+ cooldowns - Invulnerabilities
            StatusID.HallowedGround,    // 420s
            StatusID.Holmgang,          // 240s
            StatusID.LivingDead,        // 300s
            StatusID.WalkingDead,       // 300s
            StatusID.Superbolide        // 360s
        );
    }

    /// <summary>
    /// Determines if a long cooldown (90+ seconds) mitigation can be used.
    /// Returns false if another long cooldown mitigation is already active.
    /// </summary>
    /// <param name="isLongCooldown">Whether the mitigation being considered has a 90+ second cooldown.</param>
    /// <returns>True if the long cooldown can be used; otherwise, false.</returns>
    public static bool CanUseLongCooldownMitigation(bool isLongCooldown)
    {
        if (!isLongCooldown) return true;

        // If a long cooldown mitigation is already active, don't use another one
        return !HasLongCooldownMitigationActive();
    }

    /// <summary>
    /// Determines if a mitigation with the specified recast time can be used.
    /// Prevents long cooldowns (90+ seconds) from overwriting each other.
    /// </summary>
    /// <param name="recastTime">The recast time of the mitigation in seconds.</param>
    /// <returns>True if the mitigation can be used; otherwise, false.</returns>
    public static bool CanUseMitigationByRecast(float recastTime)
    {
        // If recast time is 90 seconds or more, check for long cooldown overlap
        if (recastTime >= 90f)
        {
            return !HasLongCooldownMitigationActive();
        }

        return true;
    }

    /// <summary>
    /// Checks if using a specific mitigation action would overwrite an existing long cooldown.
    /// </summary>
    /// <param name="cooldown">The cooldown info of the mitigation action.</param>
    /// <returns>True if the action can be used without overwriting; otherwise, false.</returns>
    public static bool CanUseActionWithoutOverwrite(ActionCooldownInfo cooldown)
    {
        return CanUseMitigationByRecast(cooldown.RecastTimeOneChargeRaw);
    }

    /// <summary>
    /// Determines if mitigation should be used based on damage level and current buffs.
    /// </summary>
    /// <param name="damageLevel">The incoming damage level.</param>
    /// <param name="hasStrongMitigationAvailable">Whether strong mitigation is available to use.</param>
    /// <param name="isLongCooldown">Whether the mitigation being considered has a 90+ second cooldown.</param>
    public static bool ShouldUseMitigation(DamageLevel damageLevel, bool hasStrongMitigationAvailable = false, bool isLongCooldown = false)
    {
        if (Player.Object == null || !Player.Object.IsTargetable) return false;

        var hasActiveMit = HasActiveMitigation();
        var hasStrongMit = HasStrongMitigation();

        // Check for long cooldown overlap - don't overwrite existing long cooldowns
        if (isLongCooldown && HasLongCooldownMitigationActive())
        {
            return false;
        }

        return damageLevel switch
        {
            DamageLevel.Emergency => true, // Always mitigate emergencies
            DamageLevel.Heavy => !hasStrongMit, // Use if no strong mitigation active
            DamageLevel.Moderate => !hasActiveMit || (hasStrongMitigationAvailable && !hasStrongMit),
            DamageLevel.Light => !hasActiveMit && CanUseMitigation(false),
            DamageLevel.None => false,
            _ => false
        };
    }

    /// <summary>
    /// Gets the recommended mitigation strength based on damage level.
    /// </summary>
    /// <param name="damageLevel">The incoming damage level</param>
    /// <returns>True if a big cooldown (30s/invuln) should be used, false for small cooldowns</returns>
    public static bool ShouldUseBigCooldown(DamageLevel damageLevel)
    {
        return damageLevel switch
        {
            DamageLevel.Emergency => true,
            DamageLevel.Heavy => true,
            DamageLevel.Moderate => false,
            DamageLevel.Light => false,
            _ => false
        };
    }

    /// <summary>
    /// Checks if Reprisal should be used based on enemy casts.
    /// </summary>
    public static bool ShouldUseReprisal()
    {
        if (Player.Object == null) return false;

        // Use Reprisal when enemies are casting on the tank or party
        var castingEnemies = DataCenter.AllHostileTargets.Where(e => e.IsCasting).ToList();

        if (!castingEnemies.Any()) return false;

        // Prioritize casts targeting the player
        if (castingEnemies.Any(e => e.CastTargetObjectId == Player.Object.GameObjectId))
        {
            return true;
        }

        // Also use for AoE casts affecting party
        if (castingEnemies.Any(e => e.CastTargetObjectId == 0 ||
            DataCenter.PartyMembers.Any(p => p.GameObjectId == e.CastTargetObjectId)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player should use an invulnerability ability.
    /// </summary>
    public static bool ShouldUseInvulnerability()
    {
        if (Player.Object == null) return false;

        var healthRatio = Player.Object.GetHealthRatio();
        var damageLevel = GetIncomingDamageLevel();

        // Use invuln if critically low HP and no other mitigation
        if (healthRatio <= Service.Config.HealthForDyingTanks &&
            damageLevel >= DamageLevel.Heavy &&
            !HasStrongMitigation())
        {
            return true;
        }

        // Use invuln if about to take a tankbuster with very low HP
        if (healthRatio < 0.3f &&
            DataCenter.IsHostileCastingToTank &&
            !HasActiveMitigation())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of attackers targeting the player.
    /// </summary>
    public static int GetAttackerCount()
    {
        if (Player.Object == null) return 0;

        return DataCenter.AllHostileTargets.Count(e =>
            e.TargetObject?.GameObjectId == Player.Object.GameObjectId);
    }

    /// <summary>
    /// Checks if the player is main tanking (has aggro on multiple enemies or boss).
    /// </summary>
    public static bool IsMainTanking()
    {
        if (Player.Object == null) return false;

        var attackerCount = GetAttackerCount();

        // Main tank if multiple enemies or a boss is targeting
        if (attackerCount >= 2) return true;

        var targetedBoss = DataCenter.AllHostileTargets.FirstOrDefault(e =>
            e.TargetObject?.GameObjectId == Player.Object.GameObjectId &&
            e.IsBossFromIcon());

        return targetedBoss != null;
    }

    /// <summary>
    /// Checks if the player is currently fighting a boss (for Arms Length immunity check).
    /// </summary>
    public static bool IsFightingBoss()
    {
        if (Player.Object == null) return false;

        // Check if any boss is in combat range
        var bosses = DataCenter.AllHostileTargets.Where(e => e.IsBossFromIcon()).ToList();

        // Debug logging to help diagnose boss detection issues
        if (Svc.PluginInterface != null && Service.Config.EnableDebugTrace)
        {
            var hostileCount = DataCenter.AllHostileTargets.Count();
            Service.LogDebug($"[ParseLord3] IsFightingBoss: {bosses.Any()} (Hostiles: {hostileCount}, Bosses: {bosses.Count})");
            foreach (var boss in bosses.Take(3))
            {
                Service.LogDebug($"[ParseLord3]   Boss: {boss.Name} (BaseId: {boss.BaseId})");
            }
        }

        return bosses.Any();
    }
}
