using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class SignSetHighResolutionGraphicsFrame : ISignResponse
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public ushort NumberOfRows { get; set; }
    public ushort NumberOfColumns { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public uint GraphicsLength { get; set; }
    public string GraphicsData { get; set; } = string.Empty;
    public ushort CRC { get; set; }
}
