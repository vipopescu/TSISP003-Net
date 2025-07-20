namespace TSISP003_Net.Settings;

public class SignControllerConnectionOptions
{
    public required string IpAddress { get; set; }
    public required int Port { get; set; }
    public required string PasswordOffset { get; set; }
    public required string SeedOffset { get; set; }
    public required string Address { get; set; }
}
