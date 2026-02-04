namespace TSISP003.Domain.Entities;

/// <summary>
/// HAR Status Reply entity containing the status of the HAR controller
/// </summary>
public class HARStatusReply : ISignResponse
{
    /// <summary>
    /// Online/Offline status (false = offline, true = online)
    /// </summary>
    public bool OnlineStatus { get; set; }

    /// <summary>
    /// Application error code (see Appendix C)
    /// </summary>
    public byte ApplicationErrorCode { get; set; }

    /// <summary>
    /// Day of month (1-31)
    /// </summary>
    public byte Day { get; set; }

    /// <summary>
    /// Month (1-12)
    /// </summary>
    public byte Month { get; set; }

    /// <summary>
    /// Year (1-9999)
    /// </summary>
    public ushort Year { get; set; }

    /// <summary>
    /// Hours (0-23)
    /// </summary>
    public byte Hour { get; set; }

    /// <summary>
    /// Minutes (0-59)
    /// </summary>
    public byte Minute { get; set; }

    /// <summary>
    /// Seconds (0-59)
    /// </summary>
    public byte Second { get; set; }

    /// <summary>
    /// Controller hardware checksum
    /// </summary>
    public ushort ControllerChecksum { get; set; }

    /// <summary>
    /// Controller error code (see Appendix C)
    /// </summary>
    public byte ControllerErrorCode { get; set; }

    /// <summary>
    /// HAR disabled/enabled (false = disabled, true = enabled)
    /// </summary>
    public bool HAREnabled { get; set; }

    /// <summary>
    /// Voice ID currently playing (0 if none)
    /// </summary>
    public ushort VoiceIDPlaying { get; set; }

    /// <summary>
    /// Voice revision - identifies the modification level of the voice data
    /// </summary>
    public byte VoiceRevision { get; set; }

    /// <summary>
    /// Strategy ID active (0 if none)
    /// </summary>
    public ushort StrategyIDActive { get; set; }

    /// <summary>
    /// Strategy revision - identifies the modification level of the strategy
    /// </summary>
    public byte StrategyRevision { get; set; }

    /// <summary>
    /// Strategy status:
    /// 1 = Strategy is playing
    /// 2 = Strategy is preparing to play
    /// 3 = Strategy is not playing
    /// </summary>
    public byte StrategyStatus { get; set; }

    /// <summary>
    /// Gets the DateTime from the individual date/time components
    /// </summary>
    public DateTime DateTime => new DateTime(Year, Month, Day, Hour, Minute, Second);
}
