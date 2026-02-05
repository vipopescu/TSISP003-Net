namespace TSISP003.Domain.Entities;

/// <summary>
/// Sign Extended Status Reply - detailed status report from sign controller
/// </summary>
public class SignExtendedStatusReply
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    /// <summary>
    /// Manufacturer code details (10 bytes)
    /// </summary>
    public string ManufacturerCode { get; set; } = string.Empty;
    public byte Day { get; set; }
    public byte Month { get; set; }
    public ushort Year { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
    public byte Second { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, SignExtendedStatus> Signs { get; set; } = [];
    public ushort CRC { get; set; }
}

/// <summary>
/// Extended status information for a single sign
/// </summary>
public class SignExtendedStatus
{
    public byte SignID { get; set; }
    /// <summary>
    /// Sign type: 0=text, 1=graphics, 2=advanced graphics
    /// </summary>
    public byte SignType { get; set; }
    /// <summary>
    /// For text signs: Number of lines of characters
    /// For graphics signs: Number of rows of pixels
    /// For advanced graphics: Number of lines based on default character matrix
    /// </summary>
    public byte NumberOfRows { get; set; }
    /// <summary>
    /// For text signs: Number of columns of characters
    /// For graphics signs: Number of columns of pixels
    /// For advanced graphics: Number of columns based on default character matrix
    /// </summary>
    public byte NumberOfColumns { get; set; }
    public byte SignErrorCode { get; set; }
    /// <summary>
    /// Dimming mode: 0=automatic, 1=manual
    /// </summary>
    public byte DimmingMode { get; set; }
    /// <summary>
    /// Luminance level (1-16)
    /// </summary>
    public byte LuminanceLevel { get; set; }
    /// <summary>
    /// Length of lamp/LED status field in bytes
    /// </summary>
    public byte LampLedStatusLength { get; set; }
    /// <summary>
    /// Lamp/LED status data (hex string)
    /// For advanced graphics: includes display attributes and faulty pixel count
    /// </summary>
    public string LampLedStatus { get; set; } = string.Empty;
}
