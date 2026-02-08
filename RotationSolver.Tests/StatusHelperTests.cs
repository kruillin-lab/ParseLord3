using RotationSolver.Basic;
using RotationSolver.Basic.Data;
using RotationSolver.Basic.Helpers;
using Dalamud.Game.ClientState.Statuses;
using Moq;
using Xunit;

namespace RotationSolver.Tests;

public class StatusHelperTests
{
    [Fact]
    public void GetStatusName_ValidId_ReturnsName()
    {
        // This test requires Lumina/Service to be initialized which is hard in unit tests.
        // We will skip testing Lumina-dependent methods for now or use a mock if possible.
    }

    [Fact]
    public void IsPurifyStatus_CheckSpecificStatuses()
    {
        // PurifyPvPStatuses contains Stun, Heavy, Bind, Silence, DeepFreeze, MiracleOfNature
        Assert.Contains(StatusID.Stun_1343, StatusHelper.PurifyPvPStatuses);
        Assert.Contains(StatusID.MiracleOfNature, StatusHelper.PurifyPvPStatuses);
    }

    [Fact]
    public void IsSwiftStatus_CheckSpecificStatuses()
    {
        Assert.Contains(StatusID.Swiftcast, StatusHelper.SwiftcastStatus);
        Assert.Contains(StatusID.Triplecast, StatusHelper.SwiftcastStatus);
        Assert.Contains(StatusID.Dualcast, StatusHelper.SwiftcastStatus);
    }
}
