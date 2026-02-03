using System.ComponentModel.DataAnnotations;

namespace TSISP003.DTOs;

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

public class SignSetGraphicsFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    /// <summary>
    /// Graphics data as hex string (e.g., "FF00FF00" for pixel data)
    /// </summary>
    public required string GraphicsData { get; set; }
}

public class SignSetMessageDto
{
    public byte MessageID { get; set; }
    public byte Revision { get; set; }
    public byte TransitionTimeBetweenFrames { get; set; }
    public byte Frame1ID { get; set; }
    public byte Frame1Time { get; set; }
    public byte Frame2ID { get; set; }
    public byte Frame2Time { get; set; }
    public byte Frame3ID { get; set; }
    public byte Frame3Time { get; set; }
    public byte Frame4ID { get; set; }
    public byte Frame4Time { get; set; }
    public byte Frame5ID { get; set; }
    public byte Frame5Time { get; set; }
    public byte Frame6ID { get; set; }
    public byte Frame6Time { get; set; }
}

public class ExtendedRequestMessageDto
{
    public byte TransitionTimeBetweenFrames { get; set; }
    public ExtendedTextFrameDto? Frame1 { get; set; }
    public byte Frame1Time { get; set; }
    public ExtendedTextFrameDto? Frame2 { get; set; }
    public byte Frame2Time { get; set; }
    public ExtendedTextFrameDto? Frame3 { get; set; }
    public byte Frame3Time { get; set; }
    public ExtendedTextFrameDto? Frame4 { get; set; }
    public byte Frame4Time { get; set; }
    public ExtendedTextFrameDto? Frame5 { get; set; }
    public byte Frame5Time { get; set; }
    public ExtendedTextFrameDto? Frame6 { get; set; }
    public byte Frame6Time { get; set; }
}

public class ExtendedTextFrameDto
{
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    public required string Text { get; set; }
}

public class SignDisplayMessageDto
{
    public byte GroupID { get; set; }
    public byte MessageID { get; set; }
}

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


public class AckReplyDto
{
}

public class SignRequestStoredFrameMessagePlanDto
{
    [Range(0, 2, ErrorMessage = "TypeRequest must be 0 (frame), 1 (message), or 2 (plan)")]
    public byte TypeRequest { get; set; }

    public byte RequestID { get; set; }
}

public class PowerOnOffCommandDto
{
    public byte GroupID { get; set; }
    public bool PoweredOn { get; set; }
}

public class RejectReplyDto
{
    public byte ApplicationErrorCode { get; set; }
    public string? ApplicationErrorDescription { get; set; }
}

public class SystemResetCommandDto
{
    public byte GroupID { get; set; }
    public byte ResetLevel { get; set; }
}

public class EnablePlanCommandDto
{
    /// <summary>
    /// Group ID - the group where the plan is to be enabled
    /// </summary>
    public byte GroupID { get; set; }

    /// <summary>
    /// Plan ID - identifies the plan as stored in the device controller's memory.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).
    /// </summary>
    public byte PlanID { get; set; }
}

public class DisablePlanCommandDto
{
    /// <summary>
    /// Group ID - the group where the plan is to be disabled
    /// </summary>
    public byte GroupID { get; set; }

    /// <summary>
    /// Plan ID - identifies the plan as stored in the device controller's memory.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).
    /// </summary>
    public byte PlanID { get; set; }
}

public class SignSetHighResolutionGraphicsFrameDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    /// <summary>
    /// Number of rows of pixels (1-65535)
    /// </summary>
    public ushort NumberOfRows { get; set; }
    /// <summary>
    /// Number of columns of pixels (1-65535)
    /// </summary>
    public ushort NumberOfColumns { get; set; }
    /// <summary>
    /// Colour code (0=Default, 1-9=Monochrome colours, 0x0D=Multiple colours, 0x0E=24-bit RGB)
    /// </summary>
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    /// <summary>
    /// Graphics data as hex string (e.g., "FF00FF00" for pixel data)
    /// </summary>
    public required string GraphicsData { get; set; }
}

public class SignSetPlanDto
{
    /// <summary>
    /// Plan ID (1-255)
    /// </summary>
    public byte PlanID { get; set; }

    /// <summary>
    /// Revision - identifies the modification level of the plan
    /// </summary>
    public byte Revision { get; set; }

    /// <summary>
    /// Day of the week - bitwise field where bits 1-7 represent Sunday through Saturday.
    /// 0x7F (127) means daily operation.
    /// </summary>
    public byte DayOfWeek { get; set; }

    /// <summary>
    /// List of plan entries (up to 6)
    /// </summary>
    public List<SignSetPlanEntryDto> Entries { get; set; } = [];
}

public class SignSetPlanEntryDto
{
    /// <summary>
    /// Type of entry: 1 = frame, 2 = message
    /// </summary>
    public byte FrameMessageType { get; set; }

    /// <summary>
    /// Frame or message ID
    /// </summary>
    public byte FrameMessageID { get; set; }

    /// <summary>
    /// Start time - hour (0-23)
    /// </summary>
    public byte StartHour { get; set; }

    /// <summary>
    /// Start time - minute (0-59)
    /// </summary>
    public byte StartMinute { get; set; }

    /// <summary>
    /// Stop time - hour (0-23)
    /// </summary>
    public byte StopHour { get; set; }

    /// <summary>
    /// Stop time - minute (0-59)
    /// </summary>
    public byte StopMinute { get; set; }
}