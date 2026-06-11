namespace TSISP003.Application.DTOs;

public class SignDisplayFrameDto
{
    public byte SignID { get; set; }
    public byte FrameID { get; set; }
}

public class SignDisplayAtomicFrameDto
{
    public byte GroupID { get; set; }
    public byte NumbeOfSigns { get; set; }
    public List<SignDisplayFrameDto> Frames { get; set; } = new();
}
