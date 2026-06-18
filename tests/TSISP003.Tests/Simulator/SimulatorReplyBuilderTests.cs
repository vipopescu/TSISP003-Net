using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorReplyBuilderTests
{
    private static SimulatorReplyBuilder NewBuilder() => new(new SimulatorOptions { Address = "01", Seed = 0x12 });

    [Fact]
    public void StatusReply_EncodesYearAsBigEndianWord_AndDecimalCountFields()
    {
        var mem = new SimulatorMemory();
        mem.SetActiveFrame(7, 1);
        var b = NewBuilder();

        string app = b.StatusReply(mem, new DateTime(2025, 3, 4, 5, 6, 7));

        Assert.Equal("06", app[0..2]);          // MI
        Assert.Equal("01", app[2..4]);          // online
        Assert.Equal("07E9", app[10..14]);      // year 2025 big-endian word
        Assert.Equal("00", app[24..26]);        // controller error, decimal D2
        Assert.Equal("01", app[26..28]);        // number of signs, decimal D2
        // sign block (18 hex): SignID=01, err=00, enabled=01, frame=07, frameRev=01, msg=00, msgRev=00, plan=00, planRev=00
        Assert.Equal("010001070100000000", app[28..46]);
    }

    [Fact]
    public void Ack_IsMi01() => Assert.Equal("01", NewBuilder().Ack());

    [Fact]
    public void PasswordSeed_IsMi03PlusSeed() => Assert.Equal("0312", NewBuilder().PasswordSeed());

    [Fact]
    public void Reject_CarriesRejectedMiAndError()
        => Assert.Equal("004005", NewBuilder().Reject(0x40, 5));

    [Fact]
    public void ConfigReply_HasOneGroupOneSign()
    {
        string app = NewBuilder().ConfigReply();
        Assert.Equal("22", app[0..2]);
        Assert.Equal("01", app[22..24]); // number of groups
    }

    [Fact]
    public void ReportEnabledPlans_EncodesEntries()
    {
        string app = NewBuilder().ReportEnabledPlans(new[] { ((byte)1, (byte)5) });
        Assert.Equal("13", app[0..2]);
        Assert.Equal("01", app[2..4]);   // count
        Assert.Equal("0105", app[4..8]); // group 1 plan 5
    }
}
