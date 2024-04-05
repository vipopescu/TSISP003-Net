
using Microsoft.Extensions.Options;
using TSISP003.Settings;
using TSISP003.TCP;

namespace TSISP003.SignControllerService;

public class SignControllerServiceFactory : IHostedService
{
    private readonly IOptions<SignControllerServiceOptions> _options;
    private readonly Dictionary<string, ISignControllerService> _services;

    public SignControllerServiceFactory(IOptions<SignControllerServiceOptions> options)
    {
        _options = options;
        _services = new Dictionary<string, ISignControllerService>();

    }

    private ISignControllerService CreateServiceForDevice(SignControllerConnectionOptions deviceSettings)
    {
        return new SignControllerService(new TCPClient(deviceSettings.IpAddress, deviceSettings.Port), deviceSettings);
    }

    public ISignControllerService GetSignControllerService(string deviceName)
    {
        if (_services.ContainsKey(deviceName))
        {
            return _services[deviceName];
        }

        throw new KeyNotFoundException($"Device with name {deviceName} not found in configuration.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {

        foreach (var deviceName in _options.Value.Devices.Keys)
        {
            var serviceOptions = _options.Value.Devices[deviceName];
            var service = CreateServiceForDevice(serviceOptions);
            _services.Add(deviceName, service);

            if (service is IHostedService hostedService)
            {
                hostedService.StartAsync(cancellationToken);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var service in _services.Values)
        {
            if (service is IHostedService hostedService)
            {
                hostedService.StopAsync(cancellationToken);
            }
        }
        return Task.CompletedTask;
    }
}