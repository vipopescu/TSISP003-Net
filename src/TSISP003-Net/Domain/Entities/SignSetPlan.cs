namespace TSISP003.Domain.Entities;

/// <summary>
/// Represents a plan to be stored in the sign controller's memory.
/// A plan can contain up to 6 frames or messages scheduled by time and day of week.
/// </summary>
public class SignSetPlan : ISignResponse
{
    /// <summary>
    /// Plan ID - identifies the plan as it is stored in the sign controller's memory (1-255)
    /// </summary>
    public byte PlanID { get; set; }

    /// <summary>
    /// Revision - identifies the modification level of the plan
    /// </summary>
    public byte Revision { get; set; }

    /// <summary>
    /// Day of the week - bitwise field where bits 1-7 represent Sunday through Saturday.
    /// 0x7F means daily operation.
    /// </summary>
    public byte DayOfWeek { get; set; }

    /// <summary>
    /// List of plan entries (up to 6)
    /// </summary>
    public List<SignSetPlanEntry> Entries { get; set; } = [];
}

/// <summary>
/// Represents a single entry in a plan
/// </summary>
public class SignSetPlanEntry
{
    /// <summary>
    /// Type of entry: 0 = end of plan, 1 = frame, 2 = message
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
