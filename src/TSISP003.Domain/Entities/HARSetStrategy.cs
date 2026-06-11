using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class HARSetStrategy : ISignResponse
{
    public ushort StrategyID { get; set; }
    public byte Revision { get; set; }
    public List<ushort> VoiceIDs { get; set; } = [];
}
