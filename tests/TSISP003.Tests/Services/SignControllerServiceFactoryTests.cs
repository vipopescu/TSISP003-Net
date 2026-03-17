using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TSISP003.Infrastructure.Configuration;
using TSISP003.Infrastructure.Services;

namespace TSISP003.Tests.Services;

public class SignControllerServiceFactoryTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public SignControllerServiceFactoryTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
    }

    private IOptions<SignControllerServiceOptions> CreateOptions(Dictionary<string, SignControllerConnectionOptions>? devices = null)
    {
        var options = new SignControllerServiceOptions
        {
            Devices = devices ?? new Dictionary<string, SignControllerConnectionOptions>()
        };
        return Options.Create(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesEmptyServicesDictionary()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Assert
        Assert.False(factory.ContainsSignController("any"));
    }

    [Fact]
    public void Constructor_CreatesLoggerFromFactory()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Assert
        _mockLoggerFactory.Verify(f => f.CreateLogger(It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion

    #region ContainsSignController Tests

    [Fact]
    public void ContainsSignController_ReturnsFalse_WhenNoDevicesConfigured()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        var result = factory.ContainsSignController("device1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsSignController_ReturnsFalse_BeforeStartAsync()
    {
        // Arrange - Even with devices configured, they aren't available until StartAsync
        var devices = new Dictionary<string, SignControllerConnectionOptions>
        {
            ["device1"] = new SignControllerConnectionOptions
            {
                IpAddress = "127.0.0.1",
                Port = 5000,
                Address = "01",
                PasswordOffset = "00",
                SeedOffset = "00"
            }
        };
        var options = CreateOptions(devices);
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        var result = factory.ContainsSignController("device1");

        // Assert - Services are created during StartAsync, not constructor
        Assert.False(result);
    }

    #endregion

    #region GetSignControllerService Tests

    [Fact]
    public void GetSignControllerService_ThrowsKeyNotFoundException_WhenDeviceNotFound()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() =>
            factory.GetSignControllerService("nonexistent"));
        Assert.Contains("nonexistent", exception.Message);
    }

    [Fact]
    public void GetSignControllerService_ThrowsKeyNotFoundException_BeforeStartAsync()
    {
        // Arrange
        var devices = new Dictionary<string, SignControllerConnectionOptions>
        {
            ["device1"] = new SignControllerConnectionOptions
            {
                IpAddress = "127.0.0.1",
                Port = 5000,
                Address = "01",
                PasswordOffset = "00",
                SeedOffset = "00"
            }
        };
        var options = CreateOptions(devices);
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act & Assert - Service isn't available until StartAsync is called
        Assert.Throws<KeyNotFoundException>(() =>
            factory.GetSignControllerService("device1"));
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_CompletesSuccessfully_WithNoDevices()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        await factory.StartAsync(CancellationToken.None);

        // Assert - No exception thrown
        Assert.False(factory.ContainsSignController("any"));
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTask_WithNoDevices()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        var task = factory.StartAsync(CancellationToken.None);

        // Assert
        Assert.True(task.IsCompleted);
        await task; // Should not throw
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_CompletesSuccessfully_WithNoDevices()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        await factory.StopAsync(CancellationToken.None);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Act
        var task = factory.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(task.IsCompleted);
        await task; // Should not throw
    }

    #endregion

    #region IHostedService Implementation Tests

    [Fact]
    public void Factory_ImplementsIHostedService()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Assert
        Assert.IsAssignableFrom<IHostedService>(factory);
    }

    #endregion

    #region Options Validation Tests

    [Fact]
    public void Factory_AcceptsEmptyDevicesDictionary()
    {
        // Arrange
        var options = CreateOptions(new Dictionary<string, SignControllerConnectionOptions>());

        // Act
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Factory_AcceptsNullDevicesDictionary()
    {
        // Arrange
        var options = CreateOptions(null);

        // Act
        var factory = new SignControllerServiceFactory(options, _mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(factory);
    }

    #endregion
}
