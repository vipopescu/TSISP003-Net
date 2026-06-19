using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorMemoryTests
{
    [Fact]
    public void PutThenGetTextFrame_ReturnsSameFrame()
    {
        var mem = new SimulatorMemory();
        var frame = new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"); // "HELLO"
        mem.PutTextFrame(frame);

        Assert.Equal(frame, mem.GetTextFrame(7));
    }

    [Fact]
    public void GetTextFrame_Unknown_ReturnsNull()
        => Assert.Null(new SimulatorMemory().GetTextFrame(99));

    [Fact]
    public void DefaultSignCount_IsOneSign()
    {
        var snap = new SimulatorMemory().SnapshotSigns();
        Assert.Single(snap);
        Assert.Equal((byte)1, snap[0].SignId);
    }

    [Fact]
    public void SignCount_CreatesSequentialSignIds()
        => Assert.Equal(new byte[] { 1, 2, 3 }, new SimulatorMemory(3).SignIds);

    [Fact]
    public void SetActiveFrameAll_UpdatesEverySign()
    {
        var mem = new SimulatorMemory(3);
        mem.SetActiveFrameAll(7, 2);

        Assert.All(mem.SnapshotSigns(), s =>
        {
            Assert.Equal((byte)7, s.FrameId);
            Assert.Equal((byte)2, s.FrameRevision);
        });
    }

    [Fact]
    public void SetActiveFrameForSign_UpdatesOnlyThatSign()
    {
        var mem = new SimulatorMemory(3);
        mem.SetActiveFrameForSign(2, 9, 1);

        var snap = mem.SnapshotSigns();
        Assert.Equal((byte)0, snap[0].FrameId); // sign 1 untouched
        Assert.Equal((byte)9, snap[1].FrameId); // sign 2 updated
        Assert.Equal((byte)0, snap[2].FrameId); // sign 3 untouched
    }

    [Fact]
    public void SignEnabled_DefaultsTrue()
        => Assert.All(new SimulatorMemory(3).SnapshotSigns(), s => Assert.True(s.Enabled));
}
