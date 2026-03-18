namespace TSISP003.Infrastructure.Configuration;

public class SignControllerConnectionOptions
{
    public required string IpAddress { get; set; }
    public int Port { get; set; }
    public required string PasswordOffset { get; set; }
    public required string SeedOffset { get; set; }
    public required string Address { get; set; }
}
