using TSISP003.Domain.Enums;
using TSISP003.Infrastructure.Protocol;

namespace TSISP003.Tests.Services;

public class SignControllerServiceConfigTests
{
    #region ResetLevel Enum Tests

    [Fact]
    public void ResetLevel_HasExpectedValues()
    {
        Assert.Equal(0, (int)ResetLevel.Zero);
        Assert.Equal(1, (int)ResetLevel.One);
        Assert.Equal(2, (int)ResetLevel.Two);
        Assert.Equal(3, (int)ResetLevel.Three);
        Assert.Equal(255, (int)ResetLevel.Factory);
    }

    [Fact]
    public void ResetLevel_CanBeCast()
    {
        // Verify casting works correctly
        Assert.Equal(ResetLevel.Zero, (ResetLevel)0);
        Assert.Equal(ResetLevel.Factory, (ResetLevel)255);
    }

    #endregion

    #region SignType Enum Tests

    [Fact]
    public void SignType_HasExpectedValues()
    {
        Assert.Equal(0, (int)SignType.Text);
        Assert.Equal(1, (int)SignType.GraphicMonocolor);
        Assert.Equal(2, (int)SignType.GraphicMulticolor);
        Assert.Equal(3, (int)SignType.Graphic24BitColor);
        Assert.Equal(4, (int)SignType.Reserved);
    }

    [Fact]
    public void SignType_CanBeCast()
    {
        Assert.Equal(SignType.Text, (SignType)0);
        Assert.Equal(SignType.Graphic24BitColor, (SignType)3);
    }

    [Theory]
    [InlineData(SignType.Text, "Text")]
    [InlineData(SignType.GraphicMonocolor, "GraphicMonocolor")]
    [InlineData(SignType.GraphicMulticolor, "GraphicMulticolor")]
    [InlineData(SignType.Graphic24BitColor, "Graphic24BitColor")]
    [InlineData(SignType.Reserved, "Reserved")]
    public void SignType_ToStringReturnsExpected(SignType signType, string expected)
    {
        Assert.Equal(expected, signType.ToString());
    }

    #endregion

    #region Data Presentation Tags Tests

    [Fact]
    public void DataPresentationTags_HaveCorrectValues()
    {
        Assert.Equal('\u0001', ProtocolConstants.SOH);
        Assert.Equal('\u0002', ProtocolConstants.STX);
        Assert.Equal('\u0003', ProtocolConstants.ETX);
        Assert.Equal('\u0004', ProtocolConstants.EOT);
        Assert.Equal('\u0006', ProtocolConstants.ACK);
        Assert.Equal('\u0015', ProtocolConstants.NAK);
    }

    [Fact]
    public void SOH_IsStartOfHeader()
    {
        Assert.Equal(1, (int)ProtocolConstants.SOH);
    }

    [Fact]
    public void STX_IsStartOfText()
    {
        Assert.Equal(2, (int)ProtocolConstants.STX);
    }

    [Fact]
    public void ETX_IsEndOfText()
    {
        Assert.Equal(3, (int)ProtocolConstants.ETX);
    }

    [Fact]
    public void EOT_IsEndOfTransmission()
    {
        Assert.Equal(4, (int)ProtocolConstants.EOT);
    }

    [Fact]
    public void ACK_IsAcknowledge()
    {
        Assert.Equal(6, (int)ProtocolConstants.ACK);
    }

    [Fact]
    public void NAK_IsNegativeAcknowledge()
    {
        Assert.Equal(21, (int)ProtocolConstants.NAK);
    }

    #endregion

    #region MI Codes Tests

    [Fact]
    public void MICodes_SessionManagement_HaveCorrectValues()
    {
        Assert.Equal(0x00, ProtocolConstants.MI_REJECT_MESSAGE);
        Assert.Equal(0x01, ProtocolConstants.MI_ACK_MESSAGE);
        Assert.Equal(0x02, ProtocolConstants.MI_START_SESSION);
        Assert.Equal(0x03, ProtocolConstants.MI_PASSWORD_SEED);
        Assert.Equal(0x04, ProtocolConstants.MI_PASSWORD);
        Assert.Equal(0x05, ProtocolConstants.MI_HEARTBEAT_POLL);
        Assert.Equal(0x06, ProtocolConstants.MI_SIGN_STATUS_REPLY);
        Assert.Equal(0x07, ProtocolConstants.MI_END_SESSION);
    }

    [Fact]
    public void MICodes_SignControl_HaveCorrectValues()
    {
        Assert.Equal(0x08, ProtocolConstants.MI_SYSTEM_RESET);
        Assert.Equal(0x09, ProtocolConstants.MI_UPDATE_TIME);
        Assert.Equal(0x0A, ProtocolConstants.MI_SIGN_SET_TEXT_FRAME);
        Assert.Equal(0x0B, ProtocolConstants.MI_SIGN_SET_GRAPHIC_FRAME);
        Assert.Equal(0x0C, ProtocolConstants.MI_SIGN_SET_MESSAGE);
        Assert.Equal(0x0D, ProtocolConstants.MI_SIGN_SET_PLAN);
        Assert.Equal(0x0E, ProtocolConstants.MI_SIGN_DISPLAY_FRAME);
        Assert.Equal(0x0F, ProtocolConstants.MI_SIGN_DISPLAY_MESSAGE);
    }

    [Fact]
    public void MICodes_PlanManagement_HaveCorrectValues()
    {
        Assert.Equal(0x10, ProtocolConstants.MI_ENABLE_PLAN);
        Assert.Equal(0x11, ProtocolConstants.MI_DISABLE_PLAN);
        Assert.Equal(0x12, ProtocolConstants.MI_REQUEST_ENABLED_PLANS);
        Assert.Equal(0x13, ProtocolConstants.MI_REPORT_ENABLED_PLANS);
    }

    [Fact]
    public void MICodes_DeviceControl_HaveCorrectValues()
    {
        Assert.Equal(0x14, ProtocolConstants.MI_SIGN_SET_DIMMING_LEVEL);
        Assert.Equal(0x15, ProtocolConstants.MI_POWER_ON_OFF);
        Assert.Equal(0x16, ProtocolConstants.MI_DISABLE_ENABLE_DEVICE);
    }

    [Fact]
    public void MICodes_Diagnostics_HaveCorrectValues()
    {
        Assert.Equal(0x17, ProtocolConstants.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN);
        Assert.Equal(0x18, ProtocolConstants.MI_RETRIEVE_FAULT_LOG);
        Assert.Equal(0x19, ProtocolConstants.MI_FAULT_LOG_REPLY);
        Assert.Equal(0x1A, ProtocolConstants.MI_RESET_FAULT_LOG);
    }

    [Fact]
    public void MICodes_ExtendedStatus_HaveCorrectValues()
    {
        Assert.Equal(0x1B, ProtocolConstants.MI_SIGN_EXTENDED_STATUS_REQUEST);
        Assert.Equal(0x1C, ProtocolConstants.MI_SIGN_EXTENDED_STATUS_REPLY);
    }

    [Fact]
    public void MICodes_HighResGraphics_HaveCorrectValue()
    {
        Assert.Equal(0x1D, ProtocolConstants.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME);
    }

    [Fact]
    public void MICodes_Configuration_HaveCorrectValues()
    {
        Assert.Equal(0x21, ProtocolConstants.MI_SIGN_CONFIGURATION_REQUEST);
        Assert.Equal(0x22, ProtocolConstants.MI_SIGN_CONFIGURATION_REPLY);
    }

    [Fact]
    public void MICodes_AtomicFrames_HaveCorrectValue()
    {
        Assert.Equal(0x2B, ProtocolConstants.MI_SIGN_DISPLAY_ATOMIC_FRAMES);
    }

    [Fact]
    public void MICodes_HAR_HaveCorrectValues()
    {
        Assert.Equal(0x40, ProtocolConstants.MI_HAR_STATUS_REPLY);
        Assert.Equal(0x41, ProtocolConstants.MI_HAR_SET_VOICE_DATA_INCOMPLETE);
        Assert.Equal(0x42, ProtocolConstants.MI_HAR_SET_VOICE_DATA_COMPLETE);
        Assert.Equal(0x43, ProtocolConstants.MI_HAR_SET_STRATEGY);
        Assert.Equal(0x44, ProtocolConstants.MI_HAR_ACTIVATE_STRATEGY);
        Assert.Equal(0x45, ProtocolConstants.MI_HAR_SET_PLAN);
        Assert.Equal(0x46, ProtocolConstants.MI_HAR_REQUEST_STORED_VOICE_STRATEGY_PLAN);
        Assert.Equal(0x47, ProtocolConstants.MI_HAR_SET_VOICE_DATA_ACK);
        Assert.Equal(0x48, ProtocolConstants.MI_HAR_SET_VOICE_DATA_NAK);
    }

    [Fact]
    public void MICodes_Environmental_HaveCorrectValues()
    {
        Assert.Equal(0x80, ProtocolConstants.MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY);
        Assert.Equal(0x81, ProtocolConstants.MI_REQUEST_ENVIRONMENTAL_WEATHER_VALUES);
        Assert.Equal(0x82, ProtocolConstants.MI_ENVIRONMENTAL_WEATHER_VALUES);
        Assert.Equal(0x83, ProtocolConstants.MI_ENVIRONMENTAL_WEATHER_THRESHOLD_DEFINITION);
        Assert.Equal(0x84, ProtocolConstants.MI_REQUEST_THRESHOLD_DEFINITION);
        Assert.Equal(0x85, ProtocolConstants.MI_REQUEST_ENVIRONMENTAL_WEATHER_EVENT_LOG);
        Assert.Equal(0x86, ProtocolConstants.MI_ENVIRONMENTAL_WEATHER_EVENT_LOG_REPLY);
        Assert.Equal(0x87, ProtocolConstants.MI_RESET_ENVIRONMENTAL_WEATHER_EVENT_LOG);
    }

    #endregion
}
