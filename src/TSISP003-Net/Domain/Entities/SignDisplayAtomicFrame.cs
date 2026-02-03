namespace TSISP003.Domain.Entities;

public class SignDisplayAtomicFrame
{
    public byte GroupID { get; set; }
    public byte NumbeOfSigns { get; set; }
    public List<SignDisplayFrame> Frames { get; set; } = new();
}