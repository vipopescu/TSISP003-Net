namespace TSISP003.Application.DTOs;

public class HARSetStrategyCommandDto
{
    public ushort StrategyID { get; set; }
    public byte Revision { get; set; }
    public List<ushort> VoiceIDs { get; set; } = [];
}

public class HARSetStrategyReplyDto
{
    public ushort StrategyID { get; set; }
    public byte Revision { get; set; }
    public List<ushort> VoiceIDs { get; set; } = [];
}

public class HARActivateStrategyCommandDto
{
    public ushort StrategyID { get; set; }
}

public class HARSetPlanCommandDto
{
    public byte PlanID { get; set; }
    public byte Revision { get; set; }
    public byte DayOfWeek { get; set; }
    public List<HARSetPlanEntryDto> Entries { get; set; } = [];
}

public class HARSetPlanEntryDto
{
    public ushort StrategyID { get; set; }
    public byte StartHour { get; set; }
    public byte StartMinute { get; set; }
    public byte StopHour { get; set; }
    public byte StopMinute { get; set; }
}

public class HARSetPlanReplyDto
{
    public byte PlanID { get; set; }
    public byte Revision { get; set; }
    public byte DayOfWeek { get; set; }
    public List<HARSetPlanEntryDto> Entries { get; set; } = [];
}

public class HARRequestStoredCommandDto
{
    public byte RequestType { get; set; }
    public ushort RequestID { get; set; }
    public byte SequenceNumber { get; set; }
}
