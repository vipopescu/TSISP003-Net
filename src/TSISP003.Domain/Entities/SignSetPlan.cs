using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class SignSetPlan : ISignResponse
{
    public byte PlanID { get; set; }
    public byte Revision { get; set; }
    public byte DayOfWeek { get; set; }
    public List<SignSetPlanEntry> Entries { get; set; } = [];
}

public class SignSetPlanEntry
{
    public byte FrameMessageType { get; set; }
    public byte FrameMessageID { get; set; }
    public byte StartHour { get; set; }
    public byte StartMinute { get; set; }
    public byte StopHour { get; set; }
    public byte StopMinute { get; set; }
}
