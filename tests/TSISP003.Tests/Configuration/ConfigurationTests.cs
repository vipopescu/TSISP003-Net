using TSISP003.Infrastructure.Configuration;

namespace TSISP003.Tests.Configuration;

public class ConfigurationTests
{
    #region SignControllerServiceOptions Tests

    [Fact]
    public void SignControllerServiceOptions_DefaultValues()
    {
        // Act
        var options = new SignControllerServiceOptions();

        // Assert
        Assert.NotNull(options.Devices);
        Assert.Empty(options.Devices);
    }

    [Fact]
    public void SignControllerServiceOptions_SetDevices()
    {
        // Arrange & Act
        var options = new SignControllerServiceOptions
        {
            Devices = new Dictionary<string, SignControllerConnectionOptions>
            {
                { "TMS01", new SignControllerConnectionOptions
                    {
                        IpAddress = "192.168.1.100",
                        Port = 12333,
                        Address = "01",
                        PasswordOffset = "1234",
                        SeedOffset = "56"
                    }
                },
                { "TMS02", new SignControllerConnectionOptions
                    {
                        IpAddress = "192.168.1.101",
                        Port = 12333,
                        Address = "02",
                        PasswordOffset = "ABCD",
                        SeedOffset = "EF"
                    }
                }
            }
        };

        // Assert
        Assert.Equal(2, options.Devices.Count);
        Assert.True(options.Devices.ContainsKey("TMS01"));
        Assert.True(options.Devices.ContainsKey("TMS02"));
    }

    #endregion

    #region SignControllerConnectionOptions Tests

    [Fact]
    public void SignControllerConnectionOptions_SetAllProperties()
    {
        // Arrange & Act
        var options = new SignControllerConnectionOptions
        {
            IpAddress = "192.168.1.100",
            Port = 12333,
            Address = "01",
            PasswordOffset = "1234",
            SeedOffset = "56"
        };

        // Assert
        Assert.Equal("192.168.1.100", options.IpAddress);
        Assert.Equal(12333, options.Port);
        Assert.Equal("01", options.Address);
        Assert.Equal("1234", options.PasswordOffset);
        Assert.Equal("56", options.SeedOffset);
    }

    [Fact]
    public void SignControllerConnectionOptions_CanBeUsedInDictionary()
    {
        // Arrange
        var options = new SignControllerConnectionOptions
        {
            IpAddress = "10.0.0.1",
            Port = 8080,
            Address = "01",
            PasswordOffset = "1234",
            SeedOffset = "56"
        };
        var dict = new Dictionary<string, SignControllerConnectionOptions>();

        // Act
        dict["device1"] = options;

        // Assert
        Assert.True(dict.ContainsKey("device1"));
        Assert.Equal("10.0.0.1", dict["device1"].IpAddress);
    }

    [Fact]
    public void SignControllerConnectionOptions_DifferentPortValues()
    {
        // Test different port configurations
        var options1 = new SignControllerConnectionOptions
        {
            IpAddress = "192.168.1.1",
            Port = 12333, // Default TSI-SP-003 port
            Address = "01",
            PasswordOffset = "0000",
            SeedOffset = "00"
        };

        var options2 = new SignControllerConnectionOptions
        {
            IpAddress = "192.168.1.2",
            Port = 8080, // Custom port
            Address = "02",
            PasswordOffset = "FFFF",
            SeedOffset = "FF"
        };

        Assert.Equal(12333, options1.Port);
        Assert.Equal(8080, options2.Port);
    }

    [Fact]
    public void SignControllerConnectionOptions_HexAddressValues()
    {
        // Test hex address configurations
        var options = new SignControllerConnectionOptions
        {
            IpAddress = "192.168.1.100",
            Port = 12333,
            Address = "FF", // Max hex value
            PasswordOffset = "ABCD",
            SeedOffset = "EF"
        };

        Assert.Equal("FF", options.Address);
        Assert.Equal("ABCD", options.PasswordOffset);
        Assert.Equal("EF", options.SeedOffset);
    }

    #endregion
}
