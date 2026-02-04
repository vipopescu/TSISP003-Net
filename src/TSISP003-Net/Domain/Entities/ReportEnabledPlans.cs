namespace TSISP003.Domain.Entities;

/// <summary>
/// Represents the response to a Request Enabled Plans command.
/// Contains a list of enabled plans across all groups.
/// </summary>
public class ReportEnabledPlans
{
    /// <summary>
    /// List of enabled plan entries
    /// </summary>
    public List<EnabledPlanEntry> Entries { get; set; } = [];
}

/// <summary>
/// Represents a single enabled plan entry
/// </summary>
public class EnabledPlanEntry
{
    /// <summary>
    /// Group ID - identifies the group where the plan is enabled
    /// </summary>
    public byte GroupID { get; set; }

    /// <summary>
    /// Plan ID - identifies the plan as stored in the device controller's memory
    /// </summary>
    public byte PlanID { get; set; }
}
