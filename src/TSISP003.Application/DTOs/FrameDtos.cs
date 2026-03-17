namespace TSISP003.Application.DTOs;

public class SignSetTextFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string Text { get; set; }
}

public class SignSetGraphicsFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string GraphicsData { get; set; }
}

public class SignSetHighResolutionGraphicsFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public ushort NumberOfRows { get; set; }
    public ushort NumberOfColumns { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string GraphicsData { get; set; }
}
