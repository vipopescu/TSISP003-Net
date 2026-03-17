namespace TSISP003.Application.DTOs;

public class SignSetMessageDto
{
    public byte MessageID { get; set; }
    public byte Revision { get; set; }
    public byte TransitionTimeBetweenFrames { get; set; }
    public byte Frame1ID { get; set; }
    public byte Frame1Time { get; set; }
    public byte Frame2ID { get; set; }
    public byte Frame2Time { get; set; }
    public byte Frame3ID { get; set; }
    public byte Frame3Time { get; set; }
    public byte Frame4ID { get; set; }
    public byte Frame4Time { get; set; }
    public byte Frame5ID { get; set; }
    public byte Frame5Time { get; set; }
    public byte Frame6ID { get; set; }
    public byte Frame6Time { get; set; }
}

public class ExtendedRequestMessageDto
{
    public byte TransitionTimeBetweenFrames { get; set; }
    public ExtendedTextFrameDto? Frame1 { get; set; }
    public byte Frame1Time { get; set; }
    public ExtendedTextFrameDto? Frame2 { get; set; }
    public byte Frame2Time { get; set; }
    public ExtendedTextFrameDto? Frame3 { get; set; }
    public byte Frame3Time { get; set; }
    public ExtendedTextFrameDto? Frame4 { get; set; }
    public byte Frame4Time { get; set; }
    public ExtendedTextFrameDto? Frame5 { get; set; }
    public byte Frame5Time { get; set; }
    public ExtendedTextFrameDto? Frame6 { get; set; }
    public byte Frame6Time { get; set; }
}

public class ExtendedTextFrameDto
{
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string Text { get; set; }
}

public class SignDisplayMessageDto
{
    public byte GroupID { get; set; }
    public byte MessageID { get; set; }
}
