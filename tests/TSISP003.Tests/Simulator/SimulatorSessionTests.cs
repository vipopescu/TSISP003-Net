using TSISP003.Protocol;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Session;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorSessionTests
{
    private static SimulatorSession NewSession(SimulatorMemory mem)
    {
        var options = new SimulatorOptions { Address = "01", Seed = 0x12 };
        return new SimulatorSession(mem, new SimulatorReplyBuilder(options), options,
            () => new DateTime(2025, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void StartSession_RepliesAckAndPasswordSeed()
    {
        var session = NewSession(new SimulatorMemory());
        string start = PacketCodec.BuildData(0, 0, "01", "02");

        var outPackets = session.Handle(start);

        Assert.Contains(outPackets, p => p[0] == ProtocolConstants.ACK);
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x03 && d.AppData == "0312");
    }

    [Fact]
    public void Heartbeat_RepliesStatusReply()
    {
        var session = NewSession(new SimulatorMemory());
        var heartbeat = PacketCodec.BuildData(0, 0, "01", "05");

        var outPackets = session.Handle(heartbeat);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void SetTextFrame_StoresFrame_AndRepliesStatus()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        string appData = SetCommandParser.BuildTextFrameAppData(
            new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"));
        var packet = PacketCodec.BuildData(0, 0, "01", appData);

        var outPackets = session.Handle(packet);

        Assert.NotNull(mem.GetTextFrame(7));
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void DisplayFrame_MarksFrameActive_AndAcks()
    {
        var mem = new SimulatorMemory();
        mem.PutTextFrame(new StoredTextFrame(7, 3, 0, 0, 0, 5, "48454C4C4F"));
        var session = NewSession(mem);
        var packet = PacketCodec.BuildData(0, 0, "01", "0E" + "01" + "07"); // group 1, frame 7

        var outPackets = session.Handle(packet);

        Assert.Equal(7, mem.ActiveFrameId);
        Assert.Equal(3, mem.ActiveFrameRevision); // picked up from stored frame
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);
    }

    [Fact]
    public void RequestStoredFrame_EchoesStoredFrame()
    {
        var mem = new SimulatorMemory();
        mem.PutTextFrame(new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"));
        var session = NewSession(mem);
        // RequestType 1 = Frame, id 7
        var packet = PacketCodec.BuildData(0, 0, "01", "17" + "01" + "07");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x0A);
    }

    [Fact]
    public void UnsupportedMi_Rejects()
    {
        var session = NewSession(new SimulatorMemory());
        var packet = PacketCodec.BuildData(0, 0, "01", "40"); // HAR status — out of scope

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x00);
    }

    [Fact]
    public void BadCrc_EmitsNak_AndDoesNotStoreFrame()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        // Build a valid SetTextFrame packet for frame id 7
        string appData = SetCommandParser.BuildTextFrameAppData(
            new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"));
        string validPacket = PacketCodec.BuildData(0, 0, "01", appData);

        // Corrupt a character in the application-data region (index 9 is inside appdata)
        // The body is: SOH(1)+NS(2)+NR(2)+ADDR(2)+STX(1) = 8 chars before appdata starts at index 8
        // Flip a hex digit in the appdata at index 9 (0-based)
        char[] chars = validPacket.ToCharArray();
        chars[9] = chars[9] == '0' ? '1' : '0'; // flip one digit
        string corruptPacket = new string(chars);

        var outPackets = session.Handle(corruptPacket);

        // Should emit a NAK (not an ACK), and should NOT store the frame
        Assert.Contains(outPackets, p => p[0] == ProtocolConstants.NAK);
        Assert.DoesNotContain(outPackets, p => p[0] == ProtocolConstants.ACK);
        Assert.Null(mem.GetTextFrame(7));
    }
}
