using RotationSolver.Basic.Helpers;

namespace RotationSolver.RebornRotations.Tank;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/Tank/DRK_Reborn.cs")]

public sealed class DRK_Reborn : DarkKnightRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Smart Auto-Mitigation System")]
    public bool UseSmartMitigation { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Keep at least 3000 MP")]
    public bool TheBlackestNight { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use The Blackest Night on lowest HP party member during AOE scenarios")]
    public bool BlackLantern { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Blackest Night with above option", Parent = nameof(BlackLantern))]
    private float BlackLanternRatio { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Use Oblation on lowest HP party member during AOE scenarios")]
    public bool OblationLantern { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Oblation last stack of Oblation for party members", Parent = nameof(OblationLantern))]
    public bool OblationLanternStack { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Oblation with above option", Parent = nameof(OblationLantern))]
    private float OblationLanternRatio { get; set; } = 0.5f;
    #endregion

    #region Countdown Logic
    // Countdown logic to prepare for combat.
    // Includes logic for using Provoke, tank stances, and burst medicines.
    protected override IAction? CountDownAction(float remainTime)
    {
        //Provoke when has Shield.
        if (remainTime <= CountDownAhead)
        {
            if (HasTankStance)
            {
                if (ProvokePvE.CanUse(out _))
                {
                    return ProvokePvE;
                }
            }
        }
        if (remainTime <= 2 && UseBurstMedicine(out IAction? act))
        {
            return act;
        }

        if (remainTime <= 3 && TheBlackestNightPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime <= 4 && BloodWeaponPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    // Decision-making for emergency abilities, focusing on Blood Weapon usage.
    [RotationDesc(ActionID.LivingDeadPvE)]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // Smart Mitigation emergency logic
        if (UseSmartMitigation && Service.Config.UseSmartTankMitigation && MitigationHelper.ShouldUseInvulnerability())
        {
            if (LivingDeadPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Legacy emergency logic
        if (!UseSmartMitigation && Player?.GetHealthRatio() <= HealthForDyingTanks)
        {
            if (LivingDeadPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShadowstridePvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (ShadowstridePvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        // Smart Mitigation for party-wide damage
        if (UseSmartMitigation && Service.Config.UseSmartTankMitigation)
        {
            var damageLevel = MitigationHelper.GetIncomingDamageLevel();

            // Use TBN on low HP party member
            if (!InTwoMIsBurst && OblationLantern && TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.LowHP) &&
                !TheBlackestNightPvE.Target.Target.HasStatus(false, StatusID.Transcendent) &&
                TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio)
            {
                return true;
            }

            // Use Oblation on low HP party member
            if (!InTwoMIsBurst && OblationLantern && OblationPvE.CanUse(out act, usedUp: OblationLanternStack, targetOverride: TargetType.LowHP) &&
                !OblationPvE.Target.Target.HasStatus(false, StatusID.Transcendent) &&
                OblationPvE.Target.Target.GetHealthRatio() <= OblationLanternRatio)
            {
                return true;
            }

            // Use Dark Missionary for moderate/heavy party damage
            if (!InTwoMIsBurst && damageLevel >= MitigationHelper.DamageLevel.Moderate &&
                MitigationHelper.CanUseActionWithoutOverwrite(DarkMissionaryPvE.Cooldown) &&
                DarkMissionaryPvE.CanUse(out act))
            {
                return true;
            }

            // Use Reprisal for enemy casts
            if (!InTwoMIsBurst && MitigationHelper.ShouldUseReprisal() && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }

            // Fallback Oblation for self
            if (!InTwoMIsBurst && OblationPvE.CanUse(out act, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
            {
                return true;
            }

            act = null;
            return false;
        }

        // Legacy logic
        if (!InTwoMIsBurst && OblationLantern && TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.LowHP) && !TheBlackestNightPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio)
        {
            return true;
        }

        if (!InTwoMIsBurst && OblationLantern && OblationPvE.CanUse(out act, usedUp: OblationLanternStack, targetOverride: TargetType.LowHP) && !OblationPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && OblationPvE.Target.Target.GetHealthRatio() <= OblationLanternRatio)
        {
            return true;
        }

        if (!InTwoMIsBurst && DarkMissionaryPvE.CanUse(out act))
        {
            return true;
        }

        if (!InTwoMIsBurst && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (!InTwoMIsBurst && OblationPvE.CanUse(out act, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.OblationPvE, ActionID.TheBlackestNightPvE, ActionID.DarkMindPvE, ActionID.ShadowWallPvE, ActionID.ShadowedVigilPvE, ActionID.RampartPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        // Don't use mitigation during Living Dead
        if (StatusHelper.PlayerHasStatus(true, StatusID.LivingDead, StatusID.WalkingDead))
        {
            return false;
        }

        // Smart Mitigation System
        if (UseSmartMitigation && Service.Config.UseSmartTankMitigation)
        {
            var damageLevel = MitigationHelper.GetIncomingDamageLevel();
            var damageType = MitigationHelper.GetIncomingDamageType();

            // Skip if no damage or can't use mitigation yet
            if (!MitigationHelper.ShouldUseMitigation(damageLevel,
                ShadowWallPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.IsCoolingDown))
            {
                act = null;
                return false;
            }

            var useBigCooldown = MitigationHelper.ShouldUseBigCooldown(damageLevel);

            // Priority 1: The Blackest Night (15s CD, 25% HP shield)
            if (TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.Self))
            {
                MitigationHelper.RecordMitigationUsed(false);
                return true;
            }

            // Priority 2: Oblation (60s CD, 10% mit)
            if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
            {
                MitigationHelper.RecordMitigationUsed(false);
                return true;
            }

            // Priority 3: Reprisal (10% enemy damage down)
            if (MitigationHelper.ShouldUseReprisal() && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
            {
                MitigationHelper.RecordMitigationUsed(false);
                return true;
            }

            // Priority 4: Dark Mind (20% magical mit, 60s CD) - ONLY for magical damage
            if (damageType == MitigationHelper.DamageType.Magical && DarkMindPvE.CanUse(out act))
            {
                MitigationHelper.RecordMitigationUsed(false);
                return true;
            }

            // Priority 5: Shadow Wall/Shadowed Vigil (30% mit, 120s CD)
            if (useBigCooldown &&
                (!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) &&
                MitigationHelper.CanUseActionWithoutOverwrite(ShadowedVigilPvE.EnoughLevel ? ShadowedVigilPvE.Cooldown : ShadowWallPvE.Cooldown))
            {
                if (ShadowedVigilPvE.EnoughLevel && ShadowedVigilPvE.CanUse(out act))
                {
                    MitigationHelper.RecordMitigationUsed(true);
                    return true;
                }

                if (!ShadowedVigilPvE.EnoughLevel && ShadowWallPvE.CanUse(out act))
                {
                    MitigationHelper.RecordMitigationUsed(true);
                    return true;
                }
            }

            // Priority 6: Rampart (20% mit, 90s CD)
            if (((ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(60)) ||
                (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(60)) ||
                !ShadowWallPvE.EnoughLevel || damageLevel >= MitigationHelper.DamageLevel.Heavy) &&
                MitigationHelper.CanUseActionWithoutOverwrite(RampartPvE.Cooldown) &&
                RampartPvE.CanUse(out act))
            {
                MitigationHelper.RecordMitigationUsed(true);
                return true;
            }

            // Priority 7: Arms Length for trash pulls (NOT on bosses)
            if (!MitigationHelper.IsFightingBoss() &&
                !DataCenter.IsHostileCastingToTank &&
                ArmsLengthPvE.CanUse(out act))
            {
                MitigationHelper.RecordMitigationUsed(false);
                return true;
            }

            act = null;
            return false;
        }

        // Legacy mitigation logic
        if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
        {
            return true;
        }

        if (TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.Self))
        {
            return true;
        }

        if (DarkMindPvE.CanUse(out act))
        {
            return true;
        }

        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act))
        {
            return true;
        }

        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act))
        {
            return true;
        }

        if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        if (ReprisalPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (CheckDarkSide)
        {
            if (FloodOfDarknessPvE.CanUse(out act))
            {
                return true;
            }

            if (EdgeOfDarknessPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (IsBurst)
        {
            if (InCombat && DeliriumPvE.CanUse(out act))
            {
                return true;
            }

            if (DeliriumPvE.EnoughLevel && DeliriumPvE.Cooldown.ElapsedAfterGCD(1) && !DeliriumPvE.Cooldown.ElapsedAfterGCD(3)
                && BloodWeaponPvE.CanUse(out act))
            {
                return true;
            }

            if (!DeliriumPvE.EnoughLevel)
            {
                if (BloodWeaponPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (InCombat && LivingShadowPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (CombatElapsedLess(3))
        {
            act = null;
            return false;
        }

        if (!IsMoving && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (ShadowbringerPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (NumberOfHostilesInRange >= 3 && AbyssalDrainPvE.CanUse(out act))
        {
            return true;
        }

        if (CarveAndSpitPvE.CanUse(out act))
        {
            return true;
        }

        if (InTwoMIsBurst)
        {
            if (ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (SaltAndDarknessPvE.CanUse(out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (DisesteemPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true))
        {
            return true;
        }

        //AOE Delirium
        if (ImpalementPvE.CanUse(out act))
        {
            return true;
        }

        if (QuietusPvE.CanUse(out act))
        {
            return true;
        }

        // Single Target Delirium
        if (TorcleaverPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (ComeuppancePvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (BloodspillerPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        //AOE
        if (StalwartSoulPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        //Single Target
        if (!HasDelirium && SouleaterPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDelirium && SyphonStrikePvE.CanUse(out act))
        {
            return true;
        }

        if (UnleashPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDelirium && HardSlashPvE.CanUse(out act))
        {
            return true;
        }

        if (UnmendPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Indicates whether the Dark Knight can heal using a single ability.
    public override bool CanHealSingleAbility => false;

    // Logic to determine when to use blood-based abilities.
    private bool UseBlood
    {
        get
        {
            // Conditions based on player statuses and ability cooldowns.
            if (!DeliriumPvE.EnoughLevel || !LivingShadowPvE.EnoughLevel)
            {
                return true;
            }

            if (StatusHelper.PlayerHasStatus(true, StatusID.Delirium_3836))
            {
                return true;
            }

            if ((StatusHelper.PlayerHasStatus(true, StatusID.Delirium_1972) || StatusHelper.PlayerHasStatus(true, StatusID.Delirium_3836)) && LivingShadowPvE.Cooldown.IsCoolingDown)
            {
                return true;
            }

            return (DeliriumPvE.Cooldown.WillHaveOneChargeGCD(1) && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(3)) || (Blood >= 90 && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(1));
        }
    }
    // Determines if currently in a burst phase based on cooldowns of key abilities.
    private bool InTwoMIsBurst => BloodWeaponPvE.Cooldown.IsCoolingDown && DeliriumPvE.Cooldown.IsCoolingDown && ((LivingShadowPvE.Cooldown.IsCoolingDown && !LivingShadowPvE.Cooldown.ElapsedAfter(15)) || !LivingShadowPvE.EnoughLevel);

    // Manages DarkSide ability based on several conditions.
    private bool CheckDarkSide
    {
        get
        {
            if (DarkSideEndAfterGCD(3))
            {
                return true;
            }

            if (CombatElapsedLess(3))
            {
                return false;
            }

            if ((InTwoMIsBurst && HasDarkArts) || (HasDarkArts && StatusHelper.PlayerHasStatus(true, StatusID.BlackestNight)) || (HasDarkArts && DarkSideEndAfterGCD(3)))
            {
                return true;
            }

            if (InTwoMIsBurst && BloodWeaponPvE.Cooldown.IsCoolingDown && LivingShadowPvE.Cooldown.IsCoolingDown && SaltedEarthPvE.Cooldown.IsCoolingDown && ShadowbringerPvE.Cooldown.CurrentCharges == 0 && CarveAndSpitPvE.Cooldown.IsCoolingDown)
            {
                return true;
            }

            // Check if we should preserve MP for Blackest Night
            if (TheBlackestNight)
            {
                // Edge/Flood costs 3000 MP, reserve 3000 for TBN = need 6000 total
                uint mpAfterCast = CurrentMp - 3000;

                // If we'd have less than 3000 MP after cast, check if TBN is on cooldown
                // and MP will regenerate before it's ready
                if (mpAfterCast < 3000)
                {
                    // If TBN is on cooldown, calculate MP regen before it's available
                    if (TheBlackestNightPvE.Cooldown.IsCoolingDown)
                    {
                        float cooldownRemaining = TheBlackestNightPvE.Cooldown.RecastTimeRemain;
                        // MP regenerates every 3 seconds in FFXIV
                        // Approx 200 MP per tick at level 90+
                        int ticks = (int)(cooldownRemaining / 3f);
                        uint mpRegen = (uint)(ticks * 200);

                        // Allow cast if MP after cast + expected regen >= 3000
                        if (mpAfterCast + mpRegen >= 3000)
                        {
                            return true;
                        }
                    }

                    // TBN is ready or MP won't regenerate in time - don't cast
                    return false;
                }

                // We'd have >= 3000 MP after cast - safe to use
                return CurrentMp >= 6000;
            }

            return CurrentMp >= 8500;
        }
    }
    #endregion
}
