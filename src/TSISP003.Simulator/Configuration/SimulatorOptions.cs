namespace TSISP003.Simulator.Configuration;

public class SimulatorOptions
{
    public string Address { get; set; } = "01";
    // 5050, not 5000: on macOS port 5000 is taken by the AirPlay Receiver.
    public int Port { get; set; } = 5050;
    public string SeedOffset { get; set; } = "00";
    public string PasswordOffset { get; set; } = "00";
    public byte Seed { get; set; } = 0x12;
}
