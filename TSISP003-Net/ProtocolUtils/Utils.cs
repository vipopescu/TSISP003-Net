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

    }
}