namespace TSISP003.Configuration;

public class SignControllerServiceOptions
{
    public Dictionary<string, SignControllerConnectionOptions> Devices { get; set; } = new Dictionary<string, SignControllerConnectionOptions>();
}
