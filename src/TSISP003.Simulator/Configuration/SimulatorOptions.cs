namespace TSISP003.Simulator.Configuration;

/// <summary>
/// Configuration for a single simulated sign controller (one TCP listener,
/// one device with a group of <see cref="SignCount"/> text signs).
/// </summary>
public class SimulatorOptions
{
    public string Name { get; set; } = "controller";
    public string Address { get; set; } = "01";
    // 5050, not 5000: on macOS port 5000 is taken by the AirPlay Receiver.
    public int Port { get; set; } = 5050;
    public string SeedOffset { get; set; } = "00";
    public string PasswordOffset { get; set; } = "00";
    public byte Seed { get; set; } = 0x12;

    /// <summary>Number of text signs in the controller's single group (SignIDs 1..N).</summary>
    public int SignCount { get; set; } = 3;
}
