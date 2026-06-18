namespace TSISP003.Simulator.Configuration;

public class SimulatorOptions
{
    public string Address { get; set; } = "01";
    public int Port { get; set; } = 5000;
    public string SeedOffset { get; set; } = "00";
    public string PasswordOffset { get; set; } = "00";
    public byte Seed { get; set; } = 0x12;
}
