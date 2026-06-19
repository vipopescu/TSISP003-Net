namespace TSISP003.Simulator.Configuration;

/// <summary>
/// Top-level simulator configuration: the set of sign controllers to host,
/// each on its own TCP port. Bound from the "Simulator" configuration section.
/// </summary>
public class SimulatorHostOptions
{
    public List<SimulatorOptions> Controllers { get; set; } = [];
}
