using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class HARStatusReply : ISignResponse
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public byte Day { get; set; }
    public byte Month { get; set; }
    public ushort Year { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
    public byte Second { get; set; }
    public DateTime DateTime { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public bool HAREnabled { get; set; }
    public ushort VoiceIDPlaying { get; set; }
    public byte VoiceRevision { get; set; }
    public ushort StrategyIDActive { get; set; }
    public byte StrategyRevision { get; set; }
    public byte StrategyStatus { get; set; }
}
