namespace TSISP003_Net.SignControllerDataStore.Entities;

public class SignController
{
    public bool OnlineStatus { get; set; }
    public DateTime DateChange { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfGroup { get; set; }
    public List<Group> Group { get; set; } = [];
}