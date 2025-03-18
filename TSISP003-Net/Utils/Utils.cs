using System.Text;
using TSISP003.SignControllerService;

namespace TSISP003.Utils;

public class Functions
{
    /// <summary>
    /// Prints a message packet to the console
    /// </summary>
    /// <param name="packet"></param>
    /// <param name="direction"></param>
    public static void PrintMessagePacket(string packet, string direction)
    {
        packet = packet.Replace("\u0001", "<SOH>");
        packet = packet.Replace("\u0002", "<STX>");
        packet = packet.Replace("\u0003", "<ETX>");
        packet = packet.Replace("\u0004", "<EOT>");
        packet = packet.Replace("\u0006", "<ACK>");
        packet = packet.Replace("\u0015", "<NAK>");

        string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // Output the modified string with the current date and time
        Console.WriteLine($"[{dateTimeNow}] {direction} {packet}");
    }

    /// <summary>
    /// Generates a CRC for a packet of bytes
    /// </summary>
    /// <param name="data"></param>
    /// <param name="accum"></param>
    /// <returns></returns>
    public static ushort CRCGenerator(ushort data, ushort accum)
    {
        const ushort CRC_CCITT = 0x1021;
        data <<= 8; // the incoming byte becomes the high order byte in the register

        for (int index = 0; index < 8; index++)
        {
            if (((data ^ accum) & 0x8000) != 0) // check msb
                accum = (ushort)((accum << 1) ^ CRC_CCITT);
            else
                accum <<= 1;
            data <<= 1;
        }
        return accum;
    }

    /// <summary>
    /// Generates a CRC for a packet of bytes
    /// </summary>
    /// <param name="packetBytes"></param>
    /// <returns></returns>
    public static string PacketCRC(byte[] packetBytes)
    {
        ushort accumulator = 0;

        foreach (byte b in packetBytes)
        {
            accumulator = CRCGenerator(b, accumulator); // Process each byte in the packet
        }

        // Convert the accumulator to a 4-character hexadecimal string
        return accumulator.ToString("X4");
    }

    public static ushort PacketCRCushort(byte[] packetBytes)
    {
        ushort accumulator = 0;

        foreach (byte b in packetBytes)
        {
            accumulator = CRCGenerator(b, accumulator); // Process each byte in the packet
        }

        // Convert the accumulator to a 4-character hexadecimal string
        return accumulator;
    }

    /// <summary>
    /// Splits a string into chunks based on the start and end characters
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetChunks(string input, out string remaining)
    {
        List<string> chunks = new List<string>();
        List<char> startChars = new List<char>
    {
        SignControllerServiceConfig.ACK,
        SignControllerServiceConfig.SOH,
        SignControllerServiceConfig.NAK
    };
        char endChar = SignControllerServiceConfig.ETX;
        int startIndex = 0;
        int lastProcessedIndex = 0;

        while (startIndex < input.Length)
        {
            int chunkStart = -1;

            // Find the start of the next chunk.
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
                // No starting character found.
                break;
            }

            // Find the end of this chunk.
            int chunkEnd = input.IndexOf(endChar, chunkStart + 1);
            if (chunkEnd == -1)
            {
                // Incomplete chunk – leave it in the remaining buffer.
                break;
            }
            else
            {
                // Add the complete chunk.
                chunks.Add(input.Substring(chunkStart, chunkEnd - chunkStart + 1));
                lastProcessedIndex = chunkEnd + 1;
                startIndex = lastProcessedIndex;
            }
        }

        // The rest of the input is incomplete.
        remaining = input.Substring(lastProcessedIndex);
        return chunks;
    }


    /// <summary>
    /// Generates a password based on the provided seed values
    /// </summary>
    /// <param name="passwordSeedStr"></param>
    /// <param name="seedOffsetStr"></param>
    /// <param name="passwordOffsetStr"></param>
    /// <returns></returns>
    public static string GeneratePassword(string passwordSeedStr, string seedOffsetStr, string passwordOffsetStr)
    {
        // Convert hex strings to integers
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

        return l_reply.ToString("X4"); // Converts to hex string with 4 characters
    }

    /// <summary>
    /// Converts a byte array to a hexadecimal string
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string HexToAscii(string hex)
    {
        if (hex == null)
            throw new ArgumentNullException(nameof(hex));

        // Ensure the hex string has an even number of characters.
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length", nameof(hex));

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            // Convert each pair of hex characters to a byte.
            string hexPair = hex.Substring(i * 2, 2);
            bytes[i] = Convert.ToByte(hexPair, 16);
        }

        // Convert the byte array to an ASCII string.
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Converts an ASCII string to a hexadecimal string.
    /// </summary>
    /// <param name="ascii">The ASCII string to convert.</param>
    /// <returns>The hexadecimal representation of the ASCII string.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string AsciiToHex(string ascii)
    {
        if (ascii == null)
            throw new ArgumentNullException(nameof(ascii));

        StringBuilder hex = new StringBuilder(ascii.Length * 2);
        foreach (char c in ascii)
        {
            // Convert each ASCII character to its hexadecimal representation.
            hex.AppendFormat("{0:X2}", (byte)c);
        }

        return hex.ToString();
    }


}