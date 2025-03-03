namespace TSISP003_Net.SignControllerDataStore.Entities;

public class SignSetTextFrame
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public byte NumberOfCharsInText { get; set; }
    public required string Text { get; set; }
    public ushort CRC { get; set; }
}