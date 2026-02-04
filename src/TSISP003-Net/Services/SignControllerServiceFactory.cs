using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TSISP003.Configuration;
using TSISP003.Infrastructure.Tcp;


/// <summary>
/// Factory that creates and manages SignControllerService instances for each configured sign device.
/// </summary>
namespace TSISP003.Services;

public class SignControllerServiceFactory : IHostedService
{
    private readonly IOptions<SignControllerServiceOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SignControllerServiceFactory> _logger;
    private readonly Dictionary<string, ISignControllerService> _services;

    public SignControllerServiceFactory(IOptions<SignControllerServiceOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<SignControllerServiceFactory>();
        _services = new Dictionary<string, ISignControllerService>();

    }

    /// <summary>
    /// Create a new instance of SignControllerService for a device
    /// </summary>
    /// <param name="deviceSettings"></param>
    /// <param name="deviceName"></param>
    /// <returns></returns>
    private ISignControllerService CreateServiceForDevice(SignControllerConnectionOptions deviceSettings, string deviceName)
    {
        var tcpLogger = _loggerFactory.CreateLogger<TCPClient>();
        var serviceLogger = _loggerFactory.CreateLogger<SignControllerService>();
        return new SignControllerService(new TCPClient(deviceSettings.IpAddress, deviceSettings.Port, deviceName, tcpLogger), deviceSettings, serviceLogger);
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
        _logger.LogInformation("Starting SignControllerServiceFactory with {DeviceCount} configured devices", _options.Value.Devices.Count);

        // Start all services
        foreach (var deviceName in _options.Value.Devices.Keys)
        {
            var serviceOptions = _options.Value.Devices[deviceName];
            _logger.LogInformation("Creating service for device {DeviceName} at {IpAddress}:{Port}",
                deviceName, serviceOptions.IpAddress, serviceOptions.Port);

            var service = CreateServiceForDevice(serviceOptions, deviceName);
            _services.Add(deviceName, service);

            // Start service
            if (service is IHostedService hostedService)
            {
                _logger.LogInformation("Starting service for device {DeviceName}", deviceName);
                hostedService.StartAsync(cancellationToken);
            }
        }

        _logger.LogInformation("SignControllerServiceFactory started successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop all SignControllerServices
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SignControllerServiceFactory");

        foreach (var service in _services.Values)
        {
            if (service is IHostedService hostedService)
            {
                hostedService.StopAsync(cancellationToken);
            }
        }

        _logger.LogInformation("SignControllerServiceFactory stopped");
        return Task.CompletedTask;
    }
}
