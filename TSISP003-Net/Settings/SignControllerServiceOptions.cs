namespace TSISP003_Net.Settings;

public class SignControllerServiceOptions
{
    public Dictionary<string, SignControllerConnectionOptions> Devices { get; set; } = new Dictionary<string, SignControllerConnectionOptions>();
}
