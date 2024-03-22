namespace TSISP003.Settings
{
    public class SignControllerConnectionOptions
    {
        public required string IpAddress { get; set; }
        public required int Port { get; set; }
        public required string PasswordOffset { get; set; }
        public required string SeedOffset { get; set; }
    }
}