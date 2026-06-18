using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SetCommandParserTests
{
    [Fact]
    public void TextFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"); // HELLO
        string appData = SetCommandParser.BuildTextFrameAppData(f);

        Assert.Equal("0A", appData[0..2]);
        var parsed = SetCommandParser.ParseTextFrame(appData);
        Assert.Equal(f, parsed);
    }

    [Fact]
    public void GraphicsFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredGraphicsFrame(3, 2, 18, 48, 1, 0, 2, "ABCD");
        string appData = SetCommandParser.BuildGraphicsFrameAppData(f);
        Assert.Equal("0B", appData[0..2]);
        Assert.Equal(f, SetCommandParser.ParseGraphicsFrame(appData));
    }

    [Fact]
    public void HiResFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredHiResFrame(4, 1, 256, 512, 2, 0, 3, "AABBCC");
        string appData = SetCommandParser.BuildHiResFrameAppData(f);
        Assert.Equal("1D", appData[0..2]);
        Assert.Equal(f, SetCommandParser.ParseHiResFrame(appData));
    }

    [Fact]
    public void Message_BuildThenParse_RoundTrips()
    {
        var m = new StoredMessage(2, 1, 5, new (byte, byte)[] { (7, 10), (8, 10) });
        string appData = SetCommandParser.BuildMessageAppData(m);
        Assert.Equal("0C", appData[0..2]);
        var parsed = SetCommandParser.ParseMessage(appData);
        Assert.Equal((byte)2, parsed.MessageId);
        Assert.Equal((7, 10), ((int)parsed.Frames[0].Id, (int)parsed.Frames[0].Time));
        Assert.Equal((8, 10), ((int)parsed.Frames[1].Id, (int)parsed.Frames[1].Time));
    }

    [Fact]
    public void Plan_BuildThenParse_RoundTrips()
    {
        var p = new StoredPlan(1, 1, 0x7F, new[] { new StoredPlanEntry(1, 7, 8, 0, 17, 30) });
        string appData = SetCommandParser.BuildPlanAppData(p);
        Assert.Equal("0D", appData[0..2]);
        var parsed = SetCommandParser.ParsePlan(appData);
        Assert.Equal((byte)1, parsed.PlanId);
        Assert.Single(parsed.Entries);
        Assert.Equal(p.Entries[0], parsed.Entries[0]);
    }
}
