using TSISP003.Utilities.Constants;

namespace TSISP003.Tests.Utilities;

public class ErrorCodesTests
{
    #region ControllerDeviceErrorCodes Tests

    [Fact]
    public void ControllerDeviceErrorCodes_ContainsExpectedCodes()
    {
        // Assert
        Assert.True(ErrorCodes.ControllerDeviceErrorCodes.ContainsKey(0x00));
        Assert.True(ErrorCodes.ControllerDeviceErrorCodes.ContainsKey(0x01));
        Assert.True(ErrorCodes.ControllerDeviceErrorCodes.ContainsKey(0x99));
    }

    [Fact]
    public void ControllerDeviceErrorCodes_NoError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x00];

        // Assert
        Assert.Equal("No error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_PowerFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x01];

        // Assert
        Assert.Equal("Power failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_CommunicationsTimeout_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x02];

        // Assert
        Assert.Contains("Communications time-out", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_MemoryError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x03];

        // Assert
        Assert.Equal("Memory error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_BatteryFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x04];

        // Assert
        Assert.Equal("Battery failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_SignLampFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x06];

        // Assert
        Assert.Equal("Sign lamp failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_SignSingleLedFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x07];

        // Assert
        Assert.Equal("Sign single-LED failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_SignMultiLedFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x08];

        // Assert
        Assert.Equal("Sign multi-LED failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_OverTemperatureAlarm_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x09];

        // Assert
        Assert.Contains("Over-temperature", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_UnderTemperatureAlarm_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x0A];

        // Assert
        Assert.Contains("Under-temperature", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_HARHardwareFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x19];

        // Assert
        Assert.Equal("HAR hardware failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_PreExistingFault_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x99];

        // Assert
        Assert.Contains("Pre-existing", description);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x02)]
    [InlineData(0x03)]
    [InlineData(0x04)]
    [InlineData(0x05)]
    [InlineData(0x06)]
    [InlineData(0x07)]
    [InlineData(0x08)]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x0B)]
    [InlineData(0x0C)]
    [InlineData(0x0D)]
    [InlineData(0x0E)]
    [InlineData(0x0F)]
    [InlineData(0x10)]
    [InlineData(0x11)]
    [InlineData(0x19)]
    [InlineData(0x20)]
    [InlineData(0x99)]
    public void ControllerDeviceErrorCodes_AllCodes_HaveNonEmptyDescription(int code)
    {
        // Assert
        Assert.True(ErrorCodes.ControllerDeviceErrorCodes.ContainsKey(code));
        Assert.False(string.IsNullOrEmpty(ErrorCodes.ControllerDeviceErrorCodes[code]));
    }

    #endregion

    #region ApplicationErrorCodes Tests

    [Fact]
    public void ApplicationErrorCodes_ContainsExpectedCodes()
    {
        // Assert
        Assert.True(ErrorCodes.ApplicationErrorCodes.ContainsKey(0x00));
        Assert.True(ErrorCodes.ApplicationErrorCodes.ContainsKey(0x01));
        Assert.True(ErrorCodes.ApplicationErrorCodes.ContainsKey(0x21));
    }

    [Fact]
    public void ApplicationErrorCodes_NoError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x00];

        // Assert
        Assert.Equal("No error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_DeviceOffline_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x01];

        // Assert
        Assert.Contains("off-line", description);
    }

    [Fact]
    public void ApplicationErrorCodes_SyntaxError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x02];

        // Assert
        Assert.Contains("Syntax error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_LengthError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x03];

        // Assert
        Assert.Contains("Length error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_ChecksumError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x04];

        // Assert
        Assert.Contains("checksum error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_NonAsciiCharacters_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x05];

        // Assert
        Assert.Contains("non ASCII", description);
    }

    [Fact]
    public void ApplicationErrorCodes_FrameTooLarge_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x06];

        // Assert
        Assert.Contains("Frame too large", description);
    }

    [Fact]
    public void ApplicationErrorCodes_UnknownMICode_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x07];

        // Assert
        Assert.Contains("Unknown MI Code", description);
    }

    [Fact]
    public void ApplicationErrorCodes_FontNotSupported_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x0B];

        // Assert
        Assert.Contains("Font not supported", description);
    }

    [Fact]
    public void ApplicationErrorCodes_ColourNotSupported_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x0C];

        // Assert
        Assert.Contains("Colour not supported", description);
    }

    [Fact]
    public void ApplicationErrorCodes_IncorrectPassword_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x21];

        // Assert
        Assert.Contains("Incorrect password", description);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x02)]
    [InlineData(0x03)]
    [InlineData(0x04)]
    [InlineData(0x05)]
    [InlineData(0x06)]
    [InlineData(0x07)]
    [InlineData(0x08)]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x0B)]
    [InlineData(0x0C)]
    [InlineData(0x0D)]
    [InlineData(0x0E)]
    [InlineData(0x0F)]
    [InlineData(0x10)]
    [InlineData(0x11)]
    [InlineData(0x12)]
    [InlineData(0x13)]
    [InlineData(0x14)]
    [InlineData(0x15)]
    [InlineData(0x21)]
    public void ApplicationErrorCodes_AllCodes_HaveNonEmptyDescription(int code)
    {
        // Assert
        Assert.True(ErrorCodes.ApplicationErrorCodes.ContainsKey(code));
        Assert.False(string.IsNullOrEmpty(ErrorCodes.ApplicationErrorCodes[code]));
    }

    #endregion

    #region GetValueOrDefault Tests

    [Fact]
    public void ControllerDeviceErrorCodes_GetValueOrDefault_KnownCode_ReturnsDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(0x00, "Unknown");

        // Assert
        Assert.Equal("No error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_GetValueOrDefault_UnknownCode_ReturnsDefault()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(0xFF, "Unknown error code");

        // Assert
        Assert.Equal("Unknown error code", description);
    }

    [Fact]
    public void ApplicationErrorCodes_GetValueOrDefault_KnownCode_ReturnsDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault(0x00, "Unknown");

        // Assert
        Assert.Equal("No error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_GetValueOrDefault_UnknownCode_ReturnsDefault()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault(0xFF, "Unknown error code");

        // Assert
        Assert.Equal("Unknown error code", description);
    }

    #endregion
}
