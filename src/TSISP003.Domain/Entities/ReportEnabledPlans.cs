using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class ReportEnabledPlans : ISignResponse
{
    public List<EnabledPlanEntry> Entries { get; set; } = [];
}

public class EnabledPlanEntry
{
    public byte GroupID { get; set; }
    public byte PlanID { get; set; }
}
