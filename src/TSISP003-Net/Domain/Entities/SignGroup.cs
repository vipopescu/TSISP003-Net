namespace TSISP003.Domain.Entities;

public class SignGroup
{
    public byte GroupID { get; set; }
    public Dictionary<byte, Sign> Signs { get; set; } = [];
    public string Signature { get; set; } = string.Empty;
}