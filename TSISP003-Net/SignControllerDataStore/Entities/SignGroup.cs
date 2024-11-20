namespace TSISP003_Net.SignControllerDataStore.Entities;

public class SignGroup
{
    public byte GroupID { get; set; }
    public Dictionary<byte, Sign> Signs { get; set; } = [];
    public string Signature { get; set; } = string.Empty;
}