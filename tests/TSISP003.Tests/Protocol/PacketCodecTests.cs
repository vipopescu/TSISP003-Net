using TSISP003.Protocol;
using Xunit;

namespace TSISP003.Tests.Protocol;

public class PacketCodecTests
{
    [Fact]
    public void BuildData_ThenTryParse_RoundTrips()
    {
        // appdata = MI 06 + one byte payload "AB"
        string packet = PacketCodec.BuildData(ns: 1, nr: 2, addr: "01", appDataWithMi: "06AB");

        Assert.Equal(ProtocolConstants.SOH, packet[0]);
        Assert.Equal(ProtocolConstants.ETX, packet[^1]);

        Assert.True(PacketCodec.TryParse(packet, out var data, out char kind));
        Assert.Equal('D', kind);
        Assert.Equal(1, data.Ns);
        Assert.Equal(2, data.Nr);
        Assert.Equal("01", data.Addr);
        Assert.Equal(0x06, data.Mi);
        Assert.Equal("06AB", data.AppData);
    }

    [Fact]
    public void BuildData_CrcVerifies()
    {
        string packet = PacketCodec.BuildData(0, 0, "01", "05");
        Assert.True(PacketCodec.VerifyCrc(packet));
    }

    [Fact]
    public void BuildData_TamperedCrc_FailsVerify()
    {
        string packet = PacketCodec.BuildData(0, 0, "01", "05");
        // flip a char in the appdata region (index 8 is MI high nibble)
        char[] chars = packet.ToCharArray();
        chars[8] = chars[8] == '0' ? '1' : '0';
        Assert.False(PacketCodec.VerifyCrc(new string(chars)));
    }

    [Fact]
    public void BuildAck_ParsesAsAck_WithNrAndAddr()
    {
        string ack = PacketCodec.BuildAck(nr: 3, addr: "01");
        Assert.Equal(ProtocolConstants.ACK, ack[0]);
        Assert.True(PacketCodec.TryParse(ack, out var data, out char kind));
        Assert.Equal('A', kind);
        Assert.Equal(3, data.Nr);
        Assert.Equal("01", data.Addr);
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse()
    {
        Assert.False(PacketCodec.TryParse("not a packet", out _, out _));
    }

    [Fact]
    public void TryParse_DataPacketWithEmptyAppData_ReturnsFalse()
    {
        // SOH + NS + NR + ADDR + STX + (no appdata) + CRC(4) + ETX
        string body = ProtocolConstants.SOH + "0000" + "01" + ProtocolConstants.STX;
        string packet = body + "0000" + ProtocolConstants.ETX; // dummy 4-char CRC
        Assert.False(PacketCodec.TryParse(packet, out _, out _));
    }
}
