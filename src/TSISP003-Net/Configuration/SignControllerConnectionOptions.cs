namespace TSISP003.Configuration;

public class SignControllerConnectionOptions
{
    public required string IpAddress { get; set; }
    public required int Port { get; set; }
    public required string PasswordOffset { get; set; }
    public required string SeedOffset { get; set; }
    public required string Address { get; set; }
}
