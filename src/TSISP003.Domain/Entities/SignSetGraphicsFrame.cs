using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class SignSetGraphicsFrame : ISignResponse
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public ushort GraphicsLength { get; set; }
    public string GraphicsData { get; set; } = string.Empty;
    public ushort CRC { get; set; }
}
