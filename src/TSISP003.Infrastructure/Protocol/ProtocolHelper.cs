using System.Text;
using Microsoft.Extensions.Logging;

namespace TSISP003.Infrastructure.Protocol;

public static class ProtocolHelper
{
    public static void PrintMessagePacket(string packet, string direction, ILogger? logger = null)
    {
        packet = packet.Replace("\u0001", "<SOH>");
        packet = packet.Replace("\u0002", "<STX>");
        packet = packet.Replace("\u0003", "<ETX>");
        packet = packet.Replace("\u0004", "<EOT>");
        packet = packet.Replace("\u0006", "<ACK>");
        packet = packet.Replace("\u0015", "<NAK>");

        string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        logger?.LogDebug("[{DateTime}] {Direction} {Packet}", dateTimeNow, direction, packet);
    }

    public static ushort CRCGenerator(ushort data, ushort accum)
    {
        const ushort CRC_CCITT = 0x1021;
        data <<= 8;

        for (int index = 0; index < 8; index++)
        {
            if (((data ^ accum) & 0x8000) != 0)
                accum = (ushort)((accum << 1) ^ CRC_CCITT);
            else
                accum <<= 1;
            data <<= 1;
        }
        return accum;
    }

    public static string PacketCRC(byte[] packetBytes)
    {
        ushort accumulator = 0;

        foreach (byte b in packetBytes)
        {
            accumulator = CRCGenerator(b, accumulator);
        }

        return accumulator.ToString("X4");
    }

    public static ushort PacketCRCushort(byte[] packetBytes)
    {
        ushort accumulator = 0;

        foreach (byte b in packetBytes)
        {
            accumulator = CRCGenerator(b, accumulator);
        }

        return accumulator;
    }

    public static IEnumerable<string> GetChunks(string input, out string remaining)
    {
        List<string> chunks = new List<string>();
        List<char> startChars = new List<char>
        {
            ProtocolConstants.ACK,
            ProtocolConstants.SOH,
            ProtocolConstants.NAK
        };
        char endChar = ProtocolConstants.ETX;
        int startIndex = 0;
        int lastProcessedIndex = 0;

        while (startIndex < input.Length)
        {
            int chunkStart = -1;

            for (int i = startIndex; i < input.Length; i++)
            {
                if (startChars.Contains(input[i]))
                {
                    chunkStart = i;
                    break;
                }
            }
            if (chunkStart == -1)
            {
                break;
            }

            int chunkEnd = input.IndexOf(endChar, chunkStart + 1);
            if (chunkEnd == -1)
            {
                break;
            }
            else
            {
                chunks.Add(input.Substring(chunkStart, chunkEnd - chunkStart + 1));
                lastProcessedIndex = chunkEnd + 1;
                startIndex = lastProcessedIndex;
            }
        }

        remaining = input.Substring(lastProcessedIndex);
        return chunks;
    }

    public static string GeneratePassword(string passwordSeedStr, string seedOffsetStr, string passwordOffsetStr)
    {
        int passwordSeed = Convert.ToInt32(passwordSeedStr, 16);
        int seedOffset = Convert.ToInt32(seedOffsetStr, 16);
        int passwordOffset = Convert.ToInt32(passwordOffsetStr, 16);

        ushort l_reply = (ushort)((passwordSeed + seedOffset) % 256);
        bool bit5, bit7, bit8;
        ushort xordbits;

        for (int count = 1; count <= 16; count++)
        {
            bit5 = (l_reply & 0x20) != 0;
            bit7 = (l_reply & 0x80) != 0;
            bit8 = (l_reply & 0x100) != 0;

            if (bit5 ^ bit7 ^ bit8)
                xordbits = 1;
            else
                xordbits = 0;

            l_reply = (ushort)(l_reply << 1);
            l_reply = (ushort)(l_reply + xordbits);
        }

        l_reply = (ushort)(l_reply + passwordOffset);

        return l_reply.ToString("X4");
    }

    public static string HexToAscii(string hex)
    {
        if (hex == null)
            throw new ArgumentNullException(nameof(hex));

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length", nameof(hex));

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            string hexPair = hex.Substring(i * 2, 2);
            bytes[i] = Convert.ToByte(hexPair, 16);
        }

        return Encoding.ASCII.GetString(bytes);
    }

    public static string AsciiToHex(string ascii)
    {
        if (ascii == null)
            throw new ArgumentNullException(nameof(ascii));

        StringBuilder hex = new StringBuilder(ascii.Length * 2);
        foreach (char c in ascii)
        {
            hex.AppendFormat("{0:X2}", (byte)c);
        }

        return hex.ToString();
    }
}
