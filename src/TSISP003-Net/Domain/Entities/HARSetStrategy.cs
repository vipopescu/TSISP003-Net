namespace TSISP003.Domain.Entities;

/// <summary>
/// HAR Set Strategy entity - used to store a voice strategy in the HAR controller's memory
/// </summary>
public class HARSetStrategy : ISignResponse
{
    /// <summary>
    /// Strategy ID (1-65535, 0 cannot be used)
    /// </summary>
    public ushort StrategyID { get; set; }

    /// <summary>
    /// Revision number - identifies the modification level of the strategy
    /// </summary>
    public byte Revision { get; set; }

    /// <summary>
    /// List of Voice IDs that make up the strategy
    /// The order in which the Voice IDs appear is the order in which to play the associated Voice files
    /// </summary>
    public List<ushort> VoiceIDs { get; set; } = [];
}
