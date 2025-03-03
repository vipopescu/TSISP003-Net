namespace TSISP003_Net.SignControllerDataStore.Entities;

public class SignStatusReply
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public byte Day { get; set; }
    public byte Month { get; set; }
    public short Year { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
    public byte Second { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, SignStatus> Signs { get; set; } = [];
}