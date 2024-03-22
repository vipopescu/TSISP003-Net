namespace TSISP003.Settings
{
    public class SignControllerServiceOptions
    {
        public Dictionary<string, SignControllerConnectionOptions> Devices { get; set; } = new Dictionary<string, SignControllerConnectionOptions>();
    }
}