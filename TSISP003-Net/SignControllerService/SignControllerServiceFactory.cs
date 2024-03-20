
using Microsoft.Extensions.Options;
using TSISP003.Settings;

namespace TSISP003.SignControllerService
{
    public class SignControllerServiceFactory
    {
        private readonly IOptions<SignControllerServiceOptions> _options;
        private readonly Dictionary<string, ISignControllerService> _services;

        public SignControllerServiceFactory(IOptions<SignControllerServiceOptions> options)
        {
            _options = options;
            _services = new Dictionary<string, ISignControllerService>();
        }

        public ISignControllerService GetSignControllerService(string deviceName)
        {
            if (_services.ContainsKey(deviceName))
            {
                return _services[deviceName];
            }

            throw new KeyNotFoundException($"Device with name {deviceName} not found in configuration.");
        }
    }
}