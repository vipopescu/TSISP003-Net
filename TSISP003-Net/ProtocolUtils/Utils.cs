using TSISP003.SignControllerService;

namespace TSISP003.ProtocolUtils
{
    public class Utils
    {
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
        public static IEnumerable<string> GetChunks(string input)
        {
            List<char> startChars = new List<char> { SignControllerServiceConfig.ACK, SignControllerServiceConfig.SOH, SignControllerServiceConfig.NAK };
            char endChar = SignControllerServiceConfig.ETX;
            int startIndex = 0;

            while (startIndex < input.Length)
            {
                int chunkStart = -1;

                // Find the start of the next chunk
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
                    // No more chunks
                    break;
                }

                // Find the end of this chunk
                int chunkEnd = input.IndexOf(endChar, chunkStart + 1);
                if (chunkEnd == -1)
                {
                    // No end found, assume the rest of the string is the last chunk
                    yield return input.Substring(chunkStart);
                    break;
                }
                else
                {
                    // Return the chunk, including the start and end characters
                    yield return input.Substring(chunkStart, chunkEnd - chunkStart + 1);
                    startIndex = chunkEnd + 1;
                }
            }
        }

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


    }
}