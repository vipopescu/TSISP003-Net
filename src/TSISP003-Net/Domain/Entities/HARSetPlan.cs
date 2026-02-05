namespace TSISP003.Domain.Entities;

/// <summary>
/// HAR Set Plan entity - used to store a plan of up to six strategies in the HAR controller's memory
/// </summary>
public class HARSetPlan : ISignResponse
{
    /// <summary>
    /// Plan ID (1-255, 0 is invalid)
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
    /// List of strategy entries (up to 6)
    /// </summary>
    public List<HARSetPlanEntry> Entries { get; set; } = [];
}

/// <summary>
/// HAR Set Plan Entry - represents a single strategy in a plan
/// </summary>
public class HARSetPlanEntry
{
    /// <summary>
    /// Strategy ID (WORD)
    /// </summary>
    public ushort StrategyID { get; set; }

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
