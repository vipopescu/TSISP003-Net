using System.ComponentModel.DataAnnotations;

namespace TSISP003.Application.DTOs;

public class SignSetPlanDto
{
    public byte PlanID { get; set; }
    public byte Revision { get; set; }
    public byte DayOfWeek { get; set; }
    public List<SignSetPlanEntryDto> Entries { get; set; } = [];
}

public class SignSetPlanEntryDto
{
    public byte FrameMessageType { get; set; }
    public byte FrameMessageID { get; set; }
    public byte StartHour { get; set; }
    public byte StartMinute { get; set; }
    public byte StopHour { get; set; }
    public byte StopMinute { get; set; }
}

public class EnablePlanCommandDto
{
    public byte GroupID { get; set; }
    public byte PlanID { get; set; }
}

public class DisablePlanCommandDto
{
    public byte GroupID { get; set; }
    public byte PlanID { get; set; }
}

public class ReportEnabledPlansDto
{
    public List<EnabledPlanEntryDto> Entries { get; set; } = [];
}

public class EnabledPlanEntryDto
{
    public byte GroupID { get; set; }
    public byte PlanID { get; set; }
}
