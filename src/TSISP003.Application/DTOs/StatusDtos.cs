namespace TSISP003.Application.DTOs;

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

public class SignExtendedStatusReplyDto
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public required string ApplicationError { get; set; }
    public string ManufacturerCode { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public byte ControllerErrorCode { get; set; }
    public required string ControllerError { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, SignExtendedStatusDto> Signs { get; set; } = [];
}

public class SignExtendedStatusDto
{
    public byte SignID { get; set; }
    public byte SignType { get; set; }
    public required string SignTypeDescription { get; set; }
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public byte SignErrorCode { get; set; }
    public required string SignError { get; set; }
    public byte DimmingMode { get; set; }
    public required string DimmingModeDescription { get; set; }
    public byte LuminanceLevel { get; set; }
    public string LampLedStatus { get; set; } = string.Empty;
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

public class HARStatusReplyDto
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public required string ApplicationError { get; set; }
    public DateTime DateTime { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public required string ControllerError { get; set; }
    public bool HAREnabled { get; set; }
    public ushort VoiceIDPlaying { get; set; }
    public byte VoiceRevision { get; set; }
    public ushort StrategyIDActive { get; set; }
    public byte StrategyRevision { get; set; }
    public byte StrategyStatus { get; set; }
    public required string StrategyStatusDescription { get; set; }
}
