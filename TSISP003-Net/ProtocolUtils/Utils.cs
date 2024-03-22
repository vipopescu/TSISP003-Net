using TSISP003.SignControllerService;

namespace TSISP003.ProtocolUtils
{
    public class Utils
    {
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

        static string GeneratePassword(string passwordSeedStr, string seedOffsetStr)
        {
            // Convert hex strings to integers
            uint passwordSeed = Convert.ToUInt32(passwordSeedStr, 16);
            uint seedOffset = Convert.ToUInt32(seedOffsetStr, 16);


            ushort xordbits, count;
            uint l_reply;
            bool bit5, bit7, bit8;

            // Initial calculation for l_reply using the seeds
            l_reply = (passwordSeed + seedOffset) % 256;

            // Loop to generate the password
            for (count = 1; count <= 16; count++)
            {
                // Determining the boolean values based on l_reply's bits
                bit5 = (l_reply & 0x20) != 0;
                bit7 = (l_reply & 0x80) != 0;
                bit8 = (l_reply & 0x100) != 0;

                // XOR operation
                if (bit5 ^ bit7 ^ bit8)
                    xordbits = 1;
                else
                    xordbits = 0;

                // Left shift l_reply and add xordbits
                l_reply = l_reply << 1;
                l_reply = l_reply + xordbits;
            }

            // Adding the password parameter
            l_reply += 0x5A5A;

            // Convert to hex and return
            return l_reply.ToString("X4");
        }


    }
}