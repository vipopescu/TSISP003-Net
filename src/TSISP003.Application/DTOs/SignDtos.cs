namespace TSISP003.Application.DTOs;

public class SignDto
{
    public byte SignID { get; set; }
    public byte SignErrorCode { get; set; }
    public bool SignEnabled { get; set; }
    public byte FrameID { get; set; }
    public byte FrameRevision { get; set; }
    public byte MessageID { get; set; }
    public byte MessageRevision { get; set; }
    public byte PlanID { get; set; }
    public byte PlanRevision { get; set; }
    public required string SignType { get; set; }
    public short SignWidth { get; set; }
    public short SignHeight { get; set; }
}

public class SignGroupDto
{
    public byte GroupId { get; set; }
    public Dictionary<byte, SignDto> Signs { get; set; } = new();
    public string? Signature { get; set; }
}

public class SignControllerDto
{
    public bool OnlineStatus { get; set; }
    public DateTime DateChange { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfGroups { get; set; }
    public Dictionary<byte, SignGroupDto> Groups { get; set; } = [];
}
