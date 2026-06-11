namespace TSISP003.Infrastructure.Configuration;

public class SignControllerServiceOptions
{
    public Dictionary<string, SignControllerConnectionOptions> Devices { get; set; } = new();
}
