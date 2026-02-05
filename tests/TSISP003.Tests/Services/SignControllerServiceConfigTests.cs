using TSISP003.Services;
using static TSISP003.Services.SignControllerServiceConfig;

namespace TSISP003.Tests.Services;

public class SignControllerServiceConfigTests
{
    #region ResetLevel Enum Tests

    [Fact]
    public void ResetLevel_HasExpectedValues()
    {
        Assert.Equal(0, (int)ResetLevel.RESET_LEVEL_ZERO);
        Assert.Equal(1, (int)ResetLevel.RESET_LEVEL_ONE);
        Assert.Equal(2, (int)ResetLevel.RESET_LEVEL_TWO);
        Assert.Equal(3, (int)ResetLevel.RESET_LEVEL_THREE);
        Assert.Equal(255, (int)ResetLevel.RESET_LEVEL_FACTORY);
    }

    [Fact]
    public void ResetLevel_CanBeCast()
    {
        // Verify casting works correctly
        Assert.Equal(ResetLevel.RESET_LEVEL_ZERO, (ResetLevel)0);
        Assert.Equal(ResetLevel.RESET_LEVEL_FACTORY, (ResetLevel)255);
    }

    #endregion

    #region SignType Enum Tests

    [Fact]
    public void SignType_HasExpectedValues()
    {
        Assert.Equal(0, (int)SignType.SING_TYPE_TEXT);
        Assert.Equal(1, (int)SignType.SING_TYPE_GRAPHIC_MONOCOLOR);
        Assert.Equal(2, (int)SignType.SING_TYPE_GRAPHIC_MULTICOLOR);
        Assert.Equal(3, (int)SignType.SING_TYPE_GRAPHIC_24BIT_COLOR);
        Assert.Equal(4, (int)SignType.SING_TYPE_RESERVED);
    }

    [Fact]
    public void SignType_CanBeCast()
    {
        Assert.Equal(SignType.SING_TYPE_TEXT, (SignType)0);
        Assert.Equal(SignType.SING_TYPE_GRAPHIC_24BIT_COLOR, (SignType)3);
    }

    [Theory]
    [InlineData(SignType.SING_TYPE_TEXT, "SING_TYPE_TEXT")]
    [InlineData(SignType.SING_TYPE_GRAPHIC_MONOCOLOR, "SING_TYPE_GRAPHIC_MONOCOLOR")]
    [InlineData(SignType.SING_TYPE_GRAPHIC_MULTICOLOR, "SING_TYPE_GRAPHIC_MULTICOLOR")]
    [InlineData(SignType.SING_TYPE_GRAPHIC_24BIT_COLOR, "SING_TYPE_GRAPHIC_24BIT_COLOR")]
    [InlineData(SignType.SING_TYPE_RESERVED, "SING_TYPE_RESERVED")]
    public void SignType_ToStringReturnsExpected(SignType signType, string expected)
    {
        Assert.Equal(expected, signType.ToString());
    }

    #endregion

    #region Data Presentation Tags Tests

    [Fact]
    public void DataPresentationTags_HaveCorrectValues()
    {
        Assert.Equal('\u0001', SignControllerServiceConfig.SOH);
        Assert.Equal('\u0002', SignControllerServiceConfig.STX);
        Assert.Equal('\u0003', SignControllerServiceConfig.ETX);
        Assert.Equal('\u0004', SignControllerServiceConfig.EOT);
        Assert.Equal('\u0006', SignControllerServiceConfig.ACK);
        Assert.Equal('\u0015', SignControllerServiceConfig.NAK);
    }

    [Fact]
    public void SOH_IsStartOfHeader()
    {
        Assert.Equal(1, (int)SignControllerServiceConfig.SOH);
    }

    [Fact]
    public void STX_IsStartOfText()
    {
        Assert.Equal(2, (int)SignControllerServiceConfig.STX);
    }

    [Fact]
    public void ETX_IsEndOfText()
    {
        Assert.Equal(3, (int)SignControllerServiceConfig.ETX);
    }

    [Fact]
    public void EOT_IsEndOfTransmission()
    {
        Assert.Equal(4, (int)SignControllerServiceConfig.EOT);
    }

    [Fact]
    public void ACK_IsAcknowledge()
    {
        Assert.Equal(6, (int)SignControllerServiceConfig.ACK);
    }

    [Fact]
    public void NAK_IsNegativeAcknowledge()
    {
        Assert.Equal(21, (int)SignControllerServiceConfig.NAK);
    }

    #endregion

    #region MI Codes Tests

    [Fact]
    public void MICodes_SessionManagement_HaveCorrectValues()
    {
        Assert.Equal(0x00, SignControllerServiceConfig.MI_REJECT_MESSAGE);
        Assert.Equal(0x01, SignControllerServiceConfig.MI_ACK_MESSAGE);
        Assert.Equal(0x02, SignControllerServiceConfig.MI_START_SESSION);
        Assert.Equal(0x03, SignControllerServiceConfig.MI_PASSWORD_SEED);
        Assert.Equal(0x04, SignControllerServiceConfig.MI_PASSWORD);
        Assert.Equal(0x05, SignControllerServiceConfig.MI_HEARTBEAT_POLL);
        Assert.Equal(0x06, SignControllerServiceConfig.MI_SIGN_STATUS_REPLY);
        Assert.Equal(0x07, SignControllerServiceConfig.MI_END_SESSION);
    }

    [Fact]
    public void MICodes_SignControl_HaveCorrectValues()
    {
        Assert.Equal(0x08, SignControllerServiceConfig.MI_SYSTEM_RESET);
        Assert.Equal(0x09, SignControllerServiceConfig.MI_UPDATE_TIME);
        Assert.Equal(0x0A, SignControllerServiceConfig.MI_SIGN_SET_TEXT_FRAME);
        Assert.Equal(0x0B, SignControllerServiceConfig.MI_SIGN_SET_GRAPHIC_FRAME);
        Assert.Equal(0x0C, SignControllerServiceConfig.MI_SIGN_SET_MESSAGE);
        Assert.Equal(0x0D, SignControllerServiceConfig.MI_SIGN_SET_PLAN);
        Assert.Equal(0x0E, SignControllerServiceConfig.MI_SIGN_DISPLAY_FRAME);
        Assert.Equal(0x0F, SignControllerServiceConfig.MI_SIGN_DISPLAY_MESSAGE);
    }

    [Fact]
    public void MICodes_PlanManagement_HaveCorrectValues()
    {
        Assert.Equal(0x10, SignControllerServiceConfig.MI_ENABLE_PLAN);
        Assert.Equal(0x11, SignControllerServiceConfig.MI_DISABLE_PLAN);
        Assert.Equal(0x12, SignControllerServiceConfig.MI_REQUEST_ENABLED_PLANS);
        Assert.Equal(0x13, SignControllerServiceConfig.MI_REPORT_ENABLED_PLANS);
    }

    [Fact]
    public void MICodes_DeviceControl_HaveCorrectValues()
    {
        Assert.Equal(0x14, SignControllerServiceConfig.MI_SIGN_SET_DIMMING_LEVEL);
        Assert.Equal(0x15, SignControllerServiceConfig.MI_POWER_ON_OFF);
        Assert.Equal(0x16, SignControllerServiceConfig.MI_DISABLE_ENABLE_DEVICE);
    }

    [Fact]
    public void MICodes_Diagnostics_HaveCorrectValues()
    {
        Assert.Equal(0x17, SignControllerServiceConfig.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN);
        Assert.Equal(0x18, SignControllerServiceConfig.MI_RETRIEVE_FAULT_LOG);
        Assert.Equal(0x19, SignControllerServiceConfig.MI_FAULT_LOG_REPLY);
        Assert.Equal(0x1A, SignControllerServiceConfig.MI_RESET_FAULT_LOG);
    }

    [Fact]
    public void MICodes_ExtendedStatus_HaveCorrectValues()
    {
        Assert.Equal(0x1B, SignControllerServiceConfig.MI_SIGN_EXTENDED_STATUS_REQUEST);
        Assert.Equal(0x1C, SignControllerServiceConfig.MI_SIGN_EXTENDED_STATUS_REPLY);
    }

    [Fact]
    public void MICodes_HighResGraphics_HaveCorrectValue()
    {
        Assert.Equal(0x1D, SignControllerServiceConfig.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME);
    }

    [Fact]
    public void MICodes_Configuration_HaveCorrectValues()
    {
        Assert.Equal(0x21, SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REQUEST);
        Assert.Equal(0x22, SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REPLY);
    }

    [Fact]
    public void MICodes_AtomicFrames_HaveCorrectValue()
    {
        Assert.Equal(0x2B, SignControllerServiceConfig.MI_SIGN_DISPLAY_ATOMIC_FRAMES);
    }

    [Fact]
    public void MICodes_HAR_HaveCorrectValues()
    {
        Assert.Equal(0x40, SignControllerServiceConfig.MI_HAR_STATUS_REPLY);
        Assert.Equal(0x41, SignControllerServiceConfig.MI_HAR_SET_VOICE_DATA_INCOMPLETE);
        Assert.Equal(0x42, SignControllerServiceConfig.MI_HAR_SET_VOICE_DATA_COMPLETE);
        Assert.Equal(0x43, SignControllerServiceConfig.MI_HAR_SET_STRATEGY);
        Assert.Equal(0x44, SignControllerServiceConfig.MI_HAR_ACTIVATE_STRATEGY);
        Assert.Equal(0x45, SignControllerServiceConfig.MI_HAR_SET_PLAN);
        Assert.Equal(0x46, SignControllerServiceConfig.MI_HAR_REQUEST_STORED_VOICE_STRATEGY_PLAN);
        Assert.Equal(0x47, SignControllerServiceConfig.MI_HAR_SET_VOICE_DATA_ACK);
        Assert.Equal(0x48, SignControllerServiceConfig.MI_HAR_SET_VOICE_DATA_NAK);
    }

    [Fact]
    public void MICodes_Environmental_HaveCorrectValues()
    {
        Assert.Equal(0x80, SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY);
        Assert.Equal(0x81, SignControllerServiceConfig.MI_REQUEST_ENVIRONMENTAL_WEATHER_VALUES);
        Assert.Equal(0x82, SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_VALUES);
        Assert.Equal(0x83, SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_THRESHOLD_DEFINITION);
        Assert.Equal(0x84, SignControllerServiceConfig.MI_REQUEST_THRESHOLD_DEFINITION);
        Assert.Equal(0x85, SignControllerServiceConfig.MI_REQUEST_ENVIRONMENTAL_WEATHER_EVENT_LOG);
        Assert.Equal(0x86, SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_EVENT_LOG_REPLY);
        Assert.Equal(0x87, SignControllerServiceConfig.MI_RESET_ENVIRONMENTAL_WEATHER_EVENT_LOG);
    }

    #endregion
}
