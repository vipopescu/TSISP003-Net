namespace TSISP003.Settings
{
    public class SignControllerServiceOptions
    {
        public Dictionary<string, TcpClientOptions> Devices { get; set; } = new Dictionary<string, TcpClientOptions>();
    }
}