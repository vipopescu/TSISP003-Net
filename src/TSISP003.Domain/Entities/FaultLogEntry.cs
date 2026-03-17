namespace TSISP003.Domain.Entities;

public class FaultLogEntry
{
    public byte Id { get; set; }
    public byte EntryNumber { get; set; }
    public byte Day { get; set; }
    public byte Month { get; set; }
    public short Year { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
    public byte Second { get; set; }
    public byte ErrorCode { get; set; }
    public bool IsFaultCleared { get; set; }
}
