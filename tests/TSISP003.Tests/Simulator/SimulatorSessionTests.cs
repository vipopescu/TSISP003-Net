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

    // ---- Fix 1: state machine ----

    [Fact]
    public void StateMachine_FreshSession_IsIdle()
    {
        var session = NewSession(new SimulatorMemory());
        Assert.Equal(SimulatorSession.State.Idle, session.CurrentState);
    }

    [Fact]
    public void StateMachine_AfterStartAndPassword_IsOnline()
    {
        var session = NewSession(new SimulatorMemory());
        string startPacket = PacketCodec.BuildData(0, 0, "01", "02");
        session.Handle(startPacket);
        // After StartSession (MI 02), state should be SeedSent
        Assert.Equal(SimulatorSession.State.SeedSent, session.CurrentState);

        string passwordPacket = PacketCodec.BuildData(1, 1, "01", "04");
        session.Handle(passwordPacket);
        // After Password (MI 04), state should be Online
        Assert.Equal(SimulatorSession.State.Online, session.CurrentState);
    }

    // ---- Fix 3: coverage of untested dispatch branches ----

    [Fact]
    public void ConfigRequest_RepliesConfigReply()
    {
        var session = NewSession(new SimulatorMemory());
        var packet = PacketCodec.BuildData(0, 0, "01", "21");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x22);
    }

    [Fact]
    public void SetGraphicsFrame_StoresFrame_AndRepliesStatus()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        string appData = SetCommandParser.BuildGraphicsFrameAppData(
            new StoredGraphicsFrame(3, 2, 18, 48, 1, 0, 2, "ABCD"));
        var packet = PacketCodec.BuildData(0, 0, "01", appData);

        var outPackets = session.Handle(packet);

        Assert.NotNull(mem.GetGraphicsFrame(3));
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void SetMessage_StoresMessage_AndRepliesStatus()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        string appData = SetCommandParser.BuildMessageAppData(
            new StoredMessage(2, 1, 5, new (byte, byte)[] { (7, 10) }));
        var packet = PacketCodec.BuildData(0, 0, "01", appData);

        var outPackets = session.Handle(packet);

        Assert.NotNull(mem.GetMessage(2));
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void SetPlan_StoresPlan_AndRepliesStatus()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        string appData = SetCommandParser.BuildPlanAppData(
            new StoredPlan(1, 1, 0x7F, new[] { new StoredPlanEntry(1, 7, 8, 0, 17, 30) }));
        var packet = PacketCodec.BuildData(0, 0, "01", appData);

        var outPackets = session.Handle(packet);

        Assert.NotNull(mem.GetPlan(1));
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void DisplayMessage_SetsActiveMessage_AndAcks()
    {
        var mem = new SimulatorMemory();
        mem.PutMessage(new StoredMessage(2, 4, 5, new (byte, byte)[] { (1, 3) }));
        var session = NewSession(mem);
        // MI=0F, group=01, msgId=02
        var packet = PacketCodec.BuildData(0, 0, "01", "0F" + "01" + "02");

        var outPackets = session.Handle(packet);

        Assert.Equal(2, mem.ActiveMessageId);
        Assert.Equal(4, mem.ActiveMessageRevision);
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);
    }

    [Fact]
    public void EnablePlan_Then_RequestEnabledPlans_Then_DisablePlan()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);

        // Enable plan: group=01, plan=05
        var enablePacket = PacketCodec.BuildData(0, 0, "01", "10" + "01" + "05");
        var enableReply = session.Handle(enablePacket);
        Assert.Contains(enableReply, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);

        // Request enabled plans → MI 0x13 with one entry "0105"
        var reqPacket = PacketCodec.BuildData(1, 1, "01", "12");
        var reqReply = session.Handle(reqPacket);
        Assert.Contains(reqReply, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x13
            && d.AppData.Contains("0105", StringComparison.OrdinalIgnoreCase));

        // Disable plan: group=01, plan=05
        var disablePacket = PacketCodec.BuildData(2, 2, "01", "11" + "01" + "05");
        var disableReply = session.Handle(disablePacket);
        Assert.Contains(disableReply, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);

        // Request enabled plans again → MI 0x13 with count "00"
        var reqPacket2 = PacketCodec.BuildData(3, 3, "01", "12");
        var reqReply2 = session.Handle(reqPacket2);
        Assert.Contains(reqReply2, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x13
            && d.AppData[2..4] == "00");
    }

    [Fact]
    public void EndSession_AcksAndResetsStateToIdle()
    {
        var session = NewSession(new SimulatorMemory());
        // Drive to Online first
        session.Handle(PacketCodec.BuildData(0, 0, "01", "02"));
        session.Handle(PacketCodec.BuildData(1, 1, "01", "04"));
        Assert.Equal(SimulatorSession.State.Online, session.CurrentState);

        // End session
        var packet = PacketCodec.BuildData(2, 2, "01", "07");
        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);
        Assert.Equal(SimulatorSession.State.Idle, session.CurrentState);
    }

    [Fact]
    public void RequestStored_Message_ReturnsMessageAppData()
    {
        var mem = new SimulatorMemory();
        mem.PutMessage(new StoredMessage(2, 3, 5, new (byte, byte)[] { (1, 2) }));
        var session = NewSession(mem);
        // RequestType 2 = Message, id 2
        var packet = PacketCodec.BuildData(0, 0, "01", "17" + "02" + "02");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x0C);
    }

    [Fact]
    public void RequestStored_Plan_ReturnsPlanAppData()
    {
        var mem = new SimulatorMemory();
        mem.PutPlan(new StoredPlan(1, 2, 0x7F, new[] { new StoredPlanEntry(1, 7, 8, 0, 17, 30) }));
        var session = NewSession(mem);
        // RequestType 3 = Plan, id 1
        var packet = PacketCodec.BuildData(0, 0, "01", "17" + "03" + "01");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x0D);
    }

    [Fact]
    public void RequestStored_NotFound_RejectsWithMiZero()
    {
        var session = NewSession(new SimulatorMemory());
        // RequestType 1 = Frame, id 0x63 — not stored
        var packet = PacketCodec.BuildData(0, 0, "01", "17" + "01" + "63");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x00);
    }

    [Fact]
    public void MalformedPacket_WithValidCrc_RepliesRejectNotThrow()
    {
        // DisplayFrame (MI 0E) with group byte but missing the frame-id byte.
        // PacketCodec.BuildData produces a CRC-valid packet; Dispatch will throw
        // ArgumentOutOfRangeException when it tries a[4..6]. Handle must catch it
        // and reply with a Reject (MI 0x00) rather than propagating the exception.
        var session = NewSession(new SimulatorMemory());
        var packet = PacketCodec.BuildData(0, 0, "01", "0E01"); // MI 0E + group 01, frame-id missing

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x00);
    }

    [Fact]
    public void SplitPacket_BufferedAndProcessedWhenComplete()
    {
        var session = NewSession(new SimulatorMemory());
        // A valid heartbeat packet
        string p = PacketCodec.BuildData(0, 0, "01", "05");

        // Send first 4 chars — incomplete, no ETX yet
        var partial = session.Handle(p[..4]);
        Assert.DoesNotContain(partial, pkt =>
            PacketCodec.TryParse(pkt, out var d, out var k) && k == 'D');

        // Send the rest — now the buffer has the full packet
        var full = session.Handle(p[4..]);
        Assert.Contains(full, pkt =>
            PacketCodec.TryParse(pkt, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }
}
