using TSISP003.Domain.Constants;

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
        Assert.Equal("No Error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_LampFailure_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x01];

        // Assert
        Assert.Contains("Lamp failure", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_LessThan50Percent_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x02];

        // Assert
        Assert.Contains("50%", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_PowerSupplyError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x03];

        // Assert
        Assert.Equal("Power supply error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_ConflictingFrame_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x04];

        // Assert
        Assert.Equal("Conflicting frame", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_ColourError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x06];

        // Assert
        Assert.Equal("Colour error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_TemperatureAlarm_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x07];

        // Assert
        Assert.Contains("Temperature alarm", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_CommunicationError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x08];

        // Assert
        Assert.Equal("Communication error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_CutOff_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x09];

        // Assert
        Assert.Contains("Cut off", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_CriticalTemperature_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x0A];

        // Assert
        Assert.Contains("Critical temperature", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_DisplayTestActive_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x19];

        // Assert
        Assert.Equal("Display test active", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_UnknownError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes[0x99];

        // Assert
        Assert.Contains("Unknown", description);
    }

    [Theory]
    [InlineData((byte)0x00)]
    [InlineData((byte)0x01)]
    [InlineData((byte)0x02)]
    [InlineData((byte)0x03)]
    [InlineData((byte)0x04)]
    [InlineData((byte)0x05)]
    [InlineData((byte)0x06)]
    [InlineData((byte)0x07)]
    [InlineData((byte)0x08)]
    [InlineData((byte)0x09)]
    [InlineData((byte)0x0A)]
    [InlineData((byte)0x0B)]
    [InlineData((byte)0x0C)]
    [InlineData((byte)0x0D)]
    [InlineData((byte)0x0E)]
    [InlineData((byte)0x0F)]
    [InlineData((byte)0x10)]
    [InlineData((byte)0x11)]
    [InlineData((byte)0x19)]
    [InlineData((byte)0x20)]
    [InlineData((byte)0x99)]
    public void ControllerDeviceErrorCodes_AllCodes_HaveNonEmptyDescription(byte code)
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
        Assert.Equal("No Error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_RequestRejected_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x01];

        // Assert
        Assert.Contains("Request rejected", description);
    }

    [Fact]
    public void ApplicationErrorCodes_MessageIntegrityError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x02];

        // Assert
        Assert.Contains("Message Integrity error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_SyntaxError_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x03];

        // Assert
        Assert.Contains("Syntax error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_OutOfRange_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x04];

        // Assert
        Assert.Contains("Out of range", description);
    }

    [Fact]
    public void ApplicationErrorCodes_NoSession_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x05];

        // Assert
        Assert.Contains("No session", description);
    }

    [Fact]
    public void ApplicationErrorCodes_FrameTooLarge_ReturnsCorrectDescription()
    {
        // Act - Note: In the new ErrorCodes, 0x06 is "Insufficient access"
        string description = ErrorCodes.ApplicationErrorCodes[0x06];

        // Assert
        Assert.Contains("Insufficient access", description);
    }

    [Fact]
    public void ApplicationErrorCodes_NotSupported_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x07];

        // Assert
        Assert.Contains("Not supported", description);
    }

    [Fact]
    public void ApplicationErrorCodes_InvalidFont_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x0D];

        // Assert
        Assert.Contains("Invalid font", description);
    }

    [Fact]
    public void ApplicationErrorCodes_InvalidColour_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x0E];

        // Assert
        Assert.Contains("Invalid colour", description);
    }

    [Fact]
    public void ApplicationErrorCodes_HARVoiceDataTooLarge_ReturnsCorrectDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes[0x21];

        // Assert
        Assert.Contains("HAR voice data too large", description);
    }

    [Theory]
    [InlineData((byte)0x00)]
    [InlineData((byte)0x01)]
    [InlineData((byte)0x02)]
    [InlineData((byte)0x03)]
    [InlineData((byte)0x04)]
    [InlineData((byte)0x05)]
    [InlineData((byte)0x06)]
    [InlineData((byte)0x07)]
    [InlineData((byte)0x08)]
    [InlineData((byte)0x09)]
    [InlineData((byte)0x0A)]
    [InlineData((byte)0x0B)]
    [InlineData((byte)0x0C)]
    [InlineData((byte)0x0D)]
    [InlineData((byte)0x0E)]
    [InlineData((byte)0x0F)]
    [InlineData((byte)0x10)]
    [InlineData((byte)0x11)]
    [InlineData((byte)0x12)]
    [InlineData((byte)0x13)]
    [InlineData((byte)0x14)]
    [InlineData((byte)0x15)]
    [InlineData((byte)0x21)]
    public void ApplicationErrorCodes_AllCodes_HaveNonEmptyDescription(byte code)
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
        string description = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault((byte)0x00, "Unknown");

        // Assert
        Assert.Equal("No Error", description);
    }

    [Fact]
    public void ControllerDeviceErrorCodes_GetValueOrDefault_UnknownCode_ReturnsDefault()
    {
        // Act
        string description = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault((byte)0xFF, "Unknown error code");

        // Assert
        Assert.Equal("Unknown error code", description);
    }

    [Fact]
    public void ApplicationErrorCodes_GetValueOrDefault_KnownCode_ReturnsDescription()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault((byte)0x00, "Unknown");

        // Assert
        Assert.Equal("No Error", description);
    }

    [Fact]
    public void ApplicationErrorCodes_GetValueOrDefault_UnknownCode_ReturnsDefault()
    {
        // Act
        string description = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault((byte)0xFF, "Unknown error code");

        // Assert
        Assert.Equal("Unknown error code", description);
    }

    #endregion
}
