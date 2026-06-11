namespace TSISP003.Application.DTOs;

public class SystemResetCommandDto
{
    public byte GroupID { get; set; }
    public byte ResetLevel { get; set; }
}

public class UpdateTimeCommandDto
{
    public DateTime? DateTime { get; set; }
}

public class PowerOnOffCommandDto
{
    public byte GroupID { get; set; }
    public bool PoweredOn { get; set; }
}

public class DisableEnableDeviceCommandDto
{
    public List<DisableEnableDeviceEntryDto> Entries { get; set; } = [];
}

public class DisableEnableDeviceEntryDto
{
    public byte GroupID { get; set; }
    public bool Enabled { get; set; }
}

public class SignSetDimmingLevelCommandDto
{
    public List<DimmingLevelEntryDto> Entries { get; set; } = [];
}

public class DimmingLevelEntryDto
{
    public byte GroupID { get; set; }
    public byte DimmingMode { get; set; }
    public byte LuminanceLevel { get; set; }
}

public class SignRequestStoredFrameMessagePlanDto
{
    [System.ComponentModel.DataAnnotations.Range(0, 2, ErrorMessage = "TypeRequest must be 0 (frame), 1 (message), or 2 (plan)")]
    public byte TypeRequest { get; set; }
    public byte RequestID { get; set; }
}
