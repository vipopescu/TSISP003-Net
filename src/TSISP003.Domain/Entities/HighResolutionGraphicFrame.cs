namespace TSISP003.Domain.Entities;

public class HighResolutionGraphicFrame : Frame
{
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public string Graphic { get; set; } = string.Empty;
}
