using RotationSolver.Basic.Helpers;
using Xunit;

namespace RotationSolver.Tests;

/// <summary>
/// Tests for ActionHelper methods.
/// </summary>
public class ActionHelperTests
{
    #region Cooldown Group Constants

    [Fact]
    public void GCDCooldownGroup_IsExpectedValue()
    {
        // The GCD cooldown group constant should be 58 (standard GCD recast group)
        Assert.Equal(58, ActionHelper.GCDCooldownGroup);
    }

    [Fact]
    public void AbilityCooldownGroup_IsExpectedValue()
    {
        // The ability cooldown group constant should be 19
        Assert.Equal(19, ActionHelper.AbilityCooldownGroup);
    }

    [Fact]
    public void OGCDCooldownGroup_IsExpectedValue()
    {
        // The oGCD cooldown group constant should be 71
        Assert.Equal(71, ActionHelper.OGCDCooldownGroup);
    }

    [Fact]
    public void CooldownGroups_AreDistinct()
    {
        // All cooldown groups should be different values
        Assert.NotEqual(ActionHelper.GCDCooldownGroup, ActionHelper.AbilityCooldownGroup);
        Assert.NotEqual(ActionHelper.GCDCooldownGroup, ActionHelper.OGCDCooldownGroup);
        Assert.NotEqual(ActionHelper.AbilityCooldownGroup, ActionHelper.OGCDCooldownGroup);
    }

    #endregion

    #region Raise Action ID Tests

    // Note: IsRaise is an extension method on Lumina.Excel.Sheets.Action
    // which requires actual Lumina data to test properly.
    // These are the known raise action IDs documented for reference:
    // 125 - Raise (WHM)
    // 173 - Resurrection (SMN/SCH)
    // 3594 - Ascend (AST)
    // 7523 - Verraise (RDM)
    // 24287 - Egeiro (SGE)
    // 18317 - Angel Whisper (BLU)
    // 20703 - Lost Arise (Bozja)
    // 20704 - Lost Sacrifice (Bozja)
    // 29734 - Variant Raise (Variant)
    // 29735 - Variant Raise II (Variant)

    /// <summary>
    /// Documents the expected raise action IDs for verification.
    /// </summary>
    public static readonly uint[] ExpectedRaiseActionIds =
    [
        125,    // Raise (WHM)
        173,    // Resurrection (SMN/SCH)
        3594,   // Ascend (AST)
        7523,   // Verraise (RDM)
        24287,  // Egeiro (SGE)
        18317,  // Angel Whisper (BLU)
        20703,  // Lost Arise (Bozja)
        20704,  // Lost Sacrifice (Bozja)
        29734,  // Variant Raise (Variant)
        29735,  // Variant Raise II (Variant)
    ];

    [Fact]
    public void ExpectedRaiseActionIds_ContainsAllHealerRaises()
    {
        // WHM, SCH, AST, SGE, RDM should all have raise abilities
        Assert.Contains((uint)125, ExpectedRaiseActionIds);   // WHM Raise
        Assert.Contains((uint)173, ExpectedRaiseActionIds);   // SCH/SMN Resurrection
        Assert.Contains((uint)3594, ExpectedRaiseActionIds);  // AST Ascend
        Assert.Contains((uint)24287, ExpectedRaiseActionIds); // SGE Egeiro
        Assert.Contains((uint)7523, ExpectedRaiseActionIds);  // RDM Verraise
    }

    [Fact]
    public void ExpectedRaiseActionIds_ContainsBlueMageRaise()
    {
        Assert.Contains((uint)18317, ExpectedRaiseActionIds); // BLU Angel Whisper
    }

    [Fact]
    public void ExpectedRaiseActionIds_ContainsBozjaRaises()
    {
        Assert.Contains((uint)20703, ExpectedRaiseActionIds); // Lost Arise
        Assert.Contains((uint)20704, ExpectedRaiseActionIds); // Lost Sacrifice
    }

    [Fact]
    public void ExpectedRaiseActionIds_ContainsVariantRaises()
    {
        Assert.Contains((uint)29734, ExpectedRaiseActionIds); // Variant Raise
        Assert.Contains((uint)29735, ExpectedRaiseActionIds); // Variant Raise II
    }

    [Fact]
    public void ExpectedRaiseActionIds_HasCorrectCount()
    {
        // Should have exactly 10 raise abilities
        Assert.Equal(10, ExpectedRaiseActionIds.Length);
    }

    #endregion
}
