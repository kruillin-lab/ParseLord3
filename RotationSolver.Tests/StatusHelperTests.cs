using RotationSolver.Basic;
using RotationSolver.Basic.Data;
using RotationSolver.Basic.Helpers;
using Dalamud.Game.ClientState.Statuses;
using Moq;
using Xunit;

namespace RotationSolver.Tests;

public class StatusHelperTests
{
    #region Static Array Content Tests

    [Fact]
    public void PurifyPvPStatuses_ContainsExpectedStatuses()
    {
        // PurifyPvPStatuses should contain all CC statuses that can be cleansed in PvP
        Assert.Contains(StatusID.Stun_1343, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.Heavy_1344, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.Bind_1345, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.Silence_1347, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.DeepFreeze_3219, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.MiracleOfNature, StatusHelper.PurifyPvPStatuses);
        Assert.Equal(6, StatusHelper.PurifyPvPStatuses.Length);
    }

    [Fact]
    public void SwiftcastStatus_ContainsExpectedStatuses()
    {
        // SwiftcastStatus should contain all instant-cast enabling statuses
        Assert.Contains(StatusID.Swiftcast, StatusHelper.SwiftcastStatus);
        Assert.Contains(StatusID.Triplecast, StatusHelper.SwiftcastStatus);
        Assert.Contains(StatusID.Dualcast, StatusHelper.SwiftcastStatus);
        Assert.Contains(StatusID.OccultQuick, StatusHelper.SwiftcastStatus);
        Assert.Equal(4, StatusHelper.SwiftcastStatus.Length);
    }

    [Fact]
    public void TankStanceStatus_ContainsAllTankStances()
    {
        // All tank stances should be present
        Assert.Contains(StatusID.Grit, StatusHelper.TankStanceStatus);
        Assert.Contains(StatusID.RoyalGuard_1833, StatusHelper.TankStanceStatus);
        Assert.Contains(StatusID.IronWill, StatusHelper.TankStanceStatus);
        Assert.Contains(StatusID.Defiance, StatusHelper.TankStanceStatus);
        Assert.Contains(StatusID.Defiance_3124, StatusHelper.TankStanceStatus);
        Assert.Equal(5, StatusHelper.TankStanceStatus.Length);
    }

    [Fact]
    public void NoNeedHealingStatus_ContainsInvulnerabilityStatuses()
    {
        // Should contain tank invulnerability statuses
        Assert.Contains(StatusID.Holmgang_409, StatusHelper.NoNeedHealingStatus);
        Assert.Contains(StatusID.LivingDead, StatusHelper.NoNeedHealingStatus);
        Assert.Contains(StatusID.Superbolide, StatusHelper.NoNeedHealingStatus);
        Assert.Contains(StatusID.Invulnerability, StatusHelper.NoNeedHealingStatus);
    }

    [Fact]
    public void RampartStatus_ContainsTankMitigations()
    {
        // Should contain major tank mitigation statuses
        Assert.Contains(StatusID.Rampart, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.Sentinel, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.Vengeance, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.ShadowWall, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.Nebula, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.Bulwark, StatusHelper.RampartStatus);
        Assert.Contains(StatusID.Bloodwhetting, StatusHelper.RampartStatus);
    }

    [Fact]
    public void AreaHots_ContainsHealerAoEHoTs()
    {
        // Should contain area HoT statuses
        Assert.Contains(StatusID.MedicaIi, StatusHelper.AreaHots);
        Assert.Contains(StatusID.AspectedHelios, StatusHelper.AreaHots);
        Assert.Contains(StatusID.WhisperingDawn, StatusHelper.AreaHots);
        Assert.Contains(StatusID.SacredSoil_1944, StatusHelper.AreaHots);
        Assert.Contains(StatusID.Asylum_1911, StatusHelper.AreaHots);
    }

    [Fact]
    public void SingleHots_ContainsSingleTargetHoTs()
    {
        // Should contain single-target HoT statuses
        Assert.Contains(StatusID.Regen, StatusHelper.SingleHots);
        Assert.Contains(StatusID.AspectedBenefic, StatusHelper.SingleHots);
    }

    [Fact]
    public void NoPositionalStatus_ContainsTrueNorth()
    {
        Assert.Contains(StatusID.TrueNorth, StatusHelper.NoPositionalStatus);
        Assert.Single(StatusHelper.NoPositionalStatus);
    }

    [Fact]
    public void RotationLockoutStatus_ContainsJobLockoutStatuses()
    {
        // These statuses lock the player into specific rotation sequences
        Assert.Contains(StatusID.Reawakened, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.Overheated, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.InnerRelease, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.Eukrasia, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.Mudra, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.TenChiJin, StatusHelper.RotationLockoutStatus);
        Assert.Contains(StatusID.FullMetalMachinist, StatusHelper.RotationLockoutStatus);
    }

    [Fact]
    public void OracleStatuses_ContainsAllPredictionStatuses()
    {
        Assert.Contains(StatusID.PredictionOfCleansing, StatusHelper.OracleStatuses);
        Assert.Contains(StatusID.PredictionOfStarfall, StatusHelper.OracleStatuses);
        Assert.Contains(StatusID.PredictionOfJudgment, StatusHelper.OracleStatuses);
        Assert.Contains(StatusID.PredictionOfBlessing, StatusHelper.OracleStatuses);
        Assert.Equal(4, StatusHelper.OracleStatuses.Length);
    }

    [Fact]
    public void RangePhysicalDefense_ContainsRangedMitigations()
    {
        Assert.Contains(StatusID.Troubadour, StatusHelper.RangePhysicalDefense);
        Assert.Contains(StatusID.Tactician_1951, StatusHelper.RangePhysicalDefense);
        Assert.Contains(StatusID.ShieldSamba, StatusHelper.RangePhysicalDefense);
    }

    [Fact]
    public void MagicResistance_ContainsMagicDefenseStatuses()
    {
        Assert.Contains(StatusID.MagicResistance, StatusHelper.MagicResistance);
    }

    #endregion

    #region Array Non-Empty Tests

    [Fact]
    public void AllStatusArrays_AreNotEmpty()
    {
        Assert.NotEmpty(StatusHelper.RangePhysicalDefense);
        Assert.NotEmpty(StatusHelper.PhysicalResistance);
        Assert.NotEmpty(StatusHelper.MagicResistance);
        Assert.NotEmpty(StatusHelper.AreaHots);
        Assert.NotEmpty(StatusHelper.SingleHots);
        Assert.NotEmpty(StatusHelper.TankStanceStatus);
        Assert.NotEmpty(StatusHelper.NoNeedHealingStatus);
        Assert.NotEmpty(StatusHelper.SwiftcastStatus);
        Assert.NotEmpty(StatusHelper.RampartStatus);
        Assert.NotEmpty(StatusHelper.NoPositionalStatus);
        Assert.NotEmpty(StatusHelper.PurifyPvPStatuses);
        Assert.NotEmpty(StatusHelper.RotationLockoutStatus);
        Assert.NotEmpty(StatusHelper.OracleStatuses);
        Assert.NotEmpty(StatusHelper.PhantomDispellable);
    }

    #endregion

    #region Array Uniqueness Tests

    [Fact]
    public void PurifyPvPStatuses_ContainsNoDuplicates()
    {
        Assert.Equal(StatusHelper.PurifyPvPStatuses.Length, StatusHelper.PurifyPvPStatuses.Distinct().Count());
    }

    [Fact]
    public void SwiftcastStatus_ContainsNoDuplicates()
    {
        Assert.Equal(StatusHelper.SwiftcastStatus.Length, StatusHelper.SwiftcastStatus.Distinct().Count());
    }

    [Fact]
    public void TankStanceStatus_ContainsNoDuplicates()
    {
        Assert.Equal(StatusHelper.TankStanceStatus.Length, StatusHelper.TankStanceStatus.Distinct().Count());
    }

    [Fact]
    public void RampartStatus_ContainsNoDuplicates()
    {
        Assert.Equal(StatusHelper.RampartStatus.Length, StatusHelper.RampartStatus.Distinct().Count());
    }

    #endregion
}
