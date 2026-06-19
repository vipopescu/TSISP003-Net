using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorReplyBuilderTests
{
    private static SimulatorReplyBuilder NewBuilder(int signCount = 1)
        => new(new SimulatorOptions { Address = "01", Seed = 0x12, SignCount = signCount });

    [Fact]
    public void StatusReply_EncodesYearAsBigEndianWord_AndDecimalCountFields()
    {
        var mem = new SimulatorMemory(1);
        mem.SetActiveFrameAll(7, 1);

        string app = NewBuilder(1).StatusReply(mem, new DateTime(2025, 3, 4, 5, 6, 7));

        Assert.Equal("06", app[0..2]);          // MI
        Assert.Equal("01", app[2..4]);          // online
        Assert.Equal("07E9", app[10..14]);      // year 2025 big-endian word
        Assert.Equal("00", app[24..26]);        // controller error, decimal D2
        Assert.Equal("01", app[26..28]);        // number of signs, decimal D2
        // sign block (18 hex): SignID=01, err=00, enabled=01, frame=07, frameRev=01, msg=00, msgRev=00, plan=00, planRev=00
        Assert.Equal("010001070100000000", app[28..46]);
    }

    [Fact]
    public void StatusReply_ThreeSigns_EmitsThreeBlocksWithPerSignState()
    {
        var mem = new SimulatorMemory(3);
        mem.SetActiveFrameAll(7, 1);            // group display: all signs frame 7 rev 1
        mem.SetActiveFrameForSign(2, 9, 4);     // atomic override: sign 2 -> frame 9 rev 4

        string app = NewBuilder(3).StatusReply(mem, new DateTime(2025, 1, 1, 0, 0, 0));

        Assert.Equal("03", app[26..28]);                    // number of signs (decimal)
        Assert.Equal("010001070100000000", app[28..46]);    // sign 1: frame 07 rev 01
        Assert.Equal("020001090400000000", app[46..64]);    // sign 2: frame 09 rev 04
        Assert.Equal("030001070100000000", app[64..82]);    // sign 3: frame 07 rev 01
    }

    [Fact]
    public void Ack_IsMi01() => Assert.Equal("01", NewBuilder().Ack());

    [Fact]
    public void PasswordSeed_IsMi03PlusSeed() => Assert.Equal("0312", NewBuilder().PasswordSeed());

    [Fact]
    public void Reject_CarriesRejectedMiAndError()
        => Assert.Equal("004005", NewBuilder().Reject(0x40, 5));

    [Fact]
    public void ConfigReply_HasOneGroupThreeSigns()
    {
        string app = NewBuilder(3).ConfigReply();

        Assert.Equal("22", app[0..2]);
        Assert.Equal("01", app[22..24]);            // number of groups
        Assert.Equal("03", app[26..28]);            // number of signs in the group
        // sign 1 entry [28..40]: id 01, type 00, width 3000 (48 LE), height 1200 (18 LE)
        Assert.Equal("01", app[28..30]);            // sign 1 id
        Assert.Equal("00", app[30..32]);            // sign 1 type (text)
        Assert.Equal("3000", app[32..36]);          // sign 1 width 48 little-endian
        Assert.Equal("1200", app[36..40]);          // sign 1 height 18 little-endian
        Assert.Equal("02", app[40..42]);            // sign 2 id
        Assert.Equal("03", app[52..54]);            // sign 3 id
        Assert.Equal("00", app[64..66]);            // signature length
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
