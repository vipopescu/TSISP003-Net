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

        var got = mem.GetTextFrame(7);
        Assert.Equal(frame, got);
    }

    [Fact]
    public void GetTextFrame_Unknown_ReturnsNull()
    {
        var mem = new SimulatorMemory();
        Assert.Null(mem.GetTextFrame(99));
    }

    [Fact]
    public void SetActiveFrame_UpdatesDisplayState()
    {
        var mem = new SimulatorMemory();
        mem.SetActiveFrame(7, 1);
        Assert.Equal(7, mem.ActiveFrameId);
        Assert.Equal(1, mem.ActiveFrameRevision);
    }

    [Fact]
    public void SignEnabled_DefaultsTrue()
    {
        Assert.True(new SimulatorMemory().SignEnabled);
    }
}
