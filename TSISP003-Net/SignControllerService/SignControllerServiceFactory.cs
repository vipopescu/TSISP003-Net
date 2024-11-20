
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

    /// <summary>
    /// Create a new instance of SignControllerService for a device
    /// </summary>
    /// <param name="deviceSettings"></param>
    /// <returns></returns>
    private ISignControllerService CreateServiceForDevice(SignControllerConnectionOptions deviceSettings)
    {
        return new SignControllerService(new TCPClient(deviceSettings.IpAddress, deviceSettings.Port), deviceSettings);
    }

    /// <summary>
    /// Get a SignControllerService by device name
    /// </summary>
    /// <param name="deviceName"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public ISignControllerService GetSignControllerService(string deviceName)
    {
        // Check if device exists
        if (_services.ContainsKey(deviceName))
        {
            return _services[deviceName];
        }

        throw new KeyNotFoundException($"Device with name {deviceName} not found in configuration.");
    }

    /// <summary>
    /// Check if a SignControllerService exists for a device
    /// </summary>
    /// <param name="deviceName"></param>
    /// <returns></returns>
    public bool ContainsSignController(string deviceName) => _services.ContainsKey(deviceName);

    /// <summary>
    /// Start all SignControllerServices
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start all services
        foreach (var deviceName in _options.Value.Devices.Keys)
        {
            var serviceOptions = _options.Value.Devices[deviceName];
            var service = CreateServiceForDevice(serviceOptions);
            _services.Add(deviceName, service);

            // Start service
            if (service is IHostedService hostedService)
            {
                hostedService.StartAsync(cancellationToken);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop all SignControllerServices
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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