using System.ComponentModel.DataAnnotations;

namespace TSISP003_Net;

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

public class FaultLogEntryDto
{
    public byte Id { get; set; }
    public byte EntryNumber { get; set; }
    public byte ErrorCode { get; set; }
    public required string ErrorDescription { get; set; }
    public bool IsFaultCleared { get; set; }
    public DateTime EntryDateTime { get; set; }
}

public class SignStatusDto
{
    public byte SignID { get; set; }
    public byte SignErrorCode { get; set; }
    public required string SignError { get; set; }
    public bool SignEnabled { get; set; }
    public byte FrameID { get; set; }
    public byte FrameRevision { get; set; }
    public byte MessageID { get; set; }
    public byte MessageRevision { get; set; }
    public byte PlanID { get; set; }
    public byte PlanRevision { get; set; }
}

public class SignStatusReplyDto
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public DateTime dateTime { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public required string ControllerError { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, SignStatusDto> Signs { get; set; } = [];
}

public class SignSetTextFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string Text { get; set; }
}

public class SignRequestStoredFrameMessagePlanDto
{
    [Range(0, 2, ErrorMessage = "TypeRequest must be 0 (frame), 1 (message), or 2 (plan)")]
    public byte TypeRequest { get; set; }

    public byte RequestID { get; set; }
}