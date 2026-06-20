using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TSISP003.Application.Interfaces;
using TSISP003.Infrastructure.Configuration;
using TSISP003.Infrastructure.Tcp;

namespace TSISP003.Infrastructure.Services;

public class SignControllerServiceFactory : ISignControllerServiceFactory, IHostedService
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

    private ISignControllerService CreateServiceForDevice(SignControllerConnectionOptions deviceSettings, string deviceName)
    {
        var tcpLogger = _loggerFactory.CreateLogger<TcpClientAdapter>();
        var serviceLogger = _loggerFactory.CreateLogger<SignControllerService>();
        return new SignControllerService(new TcpClientAdapter(deviceSettings.IpAddress, deviceSettings.Port, deviceName, tcpLogger), deviceSettings, deviceName, serviceLogger);
    }

    public ISignControllerService GetSignControllerService(string deviceName)
    {
        if (_services.TryGetValue(deviceName, out var service))
        {
            return service;
        }

        throw new KeyNotFoundException($"Device with name {deviceName} not found in configuration.");
    }

    public bool ContainsSignController(string deviceName) => _services.ContainsKey(deviceName);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SignControllerServiceFactory with {DeviceCount} configured devices", _options.Value.Devices.Count);

        foreach (var deviceName in _options.Value.Devices.Keys)
        {
            var serviceOptions = _options.Value.Devices[deviceName];
            _logger.LogInformation("Creating service for device {DeviceName} at {IpAddress}:{Port}",
                deviceName, serviceOptions.IpAddress, serviceOptions.Port);

            var service = CreateServiceForDevice(serviceOptions, deviceName);
            _services.Add(deviceName, service);

            if (service is IHostedService hostedService)
            {
                _logger.LogInformation("Starting service for device {DeviceName}", deviceName);
                hostedService.StartAsync(cancellationToken);
            }
        }

        _logger.LogInformation("SignControllerServiceFactory started successfully");
        return Task.CompletedTask;
    }

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
