using System.Text;

namespace TSISP003.Protocol;

public record DataPacket(int Ns, int Nr, string Addr, int Mi, string AppData);

public static class PacketCodec
{
    public static string BuildData(int ns, int nr, string addr, string appDataWithMi)
    {
        string body = ProtocolConstants.SOH
            + ns.ToString("X2") + nr.ToString("X2")
            + addr
            + ProtocolConstants.STX
            + appDataWithMi;
        return body
            + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body))
            + ProtocolConstants.ETX;
    }

    public static string BuildAck(int nr, string addr) => BuildLink(ProtocolConstants.ACK, nr, addr);
    public static string BuildNak(int nr, string addr) => BuildLink(ProtocolConstants.NAK, nr, addr);

    private static string BuildLink(char control, int nr, string addr)
    {
        string body = control + nr.ToString("X2") + addr;
        return body
            + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body))
            + ProtocolConstants.ETX;
    }

    public static bool TryParse(string packet, out DataPacket data, out char kind)
    {
        data = new DataPacket(0, 0, string.Empty, 0, string.Empty);
        kind = '?';
        if (string.IsNullOrEmpty(packet) || packet[^1] != ProtocolConstants.ETX)
            return false;

        char start = packet[0];
        if (start == ProtocolConstants.ACK || start == ProtocolConstants.NAK)
        {
            // control(1) + NR(2) + ADDR(2) + CRC(4) + ETX(1) = 10 chars
            if (packet.Length < 10) return false;
            kind = start == ProtocolConstants.ACK ? 'A' : 'N';
            int lnr = Convert.ToInt32(packet[1..3], 16);
            string laddr = packet[3..5];
            data = new DataPacket(0, lnr, laddr, 0, string.Empty);
            return true;
        }

        if (start == ProtocolConstants.SOH)
        {
            // SOH + NS(2)+NR(2)+ADDR(2) + STX + appdata + CRC(4) + ETX
            if (packet.Length < 13 || packet[7] != ProtocolConstants.STX) return false;
            int ns = Convert.ToInt32(packet[1..3], 16);
            int nr = Convert.ToInt32(packet[3..5], 16);
            string addr = packet[5..7];
            string appData = packet[8..^5];
            if (appData.Length < 2) return false;
            int mi = Convert.ToInt32(appData[0..2], 16);
            kind = 'D';
            data = new DataPacket(ns, nr, addr, mi, appData);
            return true;
        }

        return false;
    }

    public static bool VerifyCrc(string packet)
    {
        if (string.IsNullOrEmpty(packet) || packet[^1] != ProtocolConstants.ETX) return false;
        string body = packet[0..^5];          // everything except CRC(4) + ETX(1)
        string crc = packet[^5..^1];
        return ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body)) == crc;
    }
}
