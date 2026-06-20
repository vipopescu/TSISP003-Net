namespace TSISP003.Application.DTOs;

/// <summary>
/// Sign status enriched with the resolved content of each sign's active message/frame,
/// fetched live from the controller via Request-Stored. Returned by GET extended/status.
/// </summary>
public class ExtendedStatusReplyDto
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public DateTime DateTime { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public required string ControllerError { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, ExtendedSignStatusContentDto> Signs { get; set; } = [];
}

public class ExtendedSignStatusContentDto
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

    /// <summary>Resolved content of the active message (null if the sign has no active message or it could not be fetched).</summary>
    public ExtendedMessageContentDto? ActiveMessage { get; set; }

    /// <summary>Resolved content of a directly-displayed active frame (null if the sign is showing a message or no frame).</summary>
    public ExtendedFrameContentDto? ActiveFrame { get; set; }
}

public class ExtendedMessageContentDto
{
    public byte MessageID { get; set; }
    public byte Revision { get; set; }
    public byte TransitionTimeBetweenFrames { get; set; }
    public List<ExtendedMessageFrameDto> Frames { get; set; } = [];
}

public class ExtendedMessageFrameDto
{
    public byte FrameID { get; set; }
    /// <summary>Per-frame display time as stored in the controller (protocol units).</summary>
    public byte Time { get; set; }
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    /// <summary>Decoded frame text, or null if the frame is not a text frame or could not be fetched.</summary>
    public string? Text { get; set; }
}

public class ExtendedFrameContentDto
{
    public byte FrameID { get; set; }
    public byte Revision { get; set; }
    public byte Font { get; set; }
    public byte Colour { get; set; }
    public byte Conspicuity { get; set; }
    /// <summary>Decoded frame text, or null if the frame is not a text frame or could not be fetched.</summary>
    public string? Text { get; set; }
}
