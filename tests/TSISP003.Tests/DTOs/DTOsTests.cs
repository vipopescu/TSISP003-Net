using TSISP003.DTOs;

namespace TSISP003.Tests.DTOs;

public class DTOsTests
{
    #region SignDto Tests

    [Fact]
    public void SignDto_DefaultValues()
    {
        var dto = new SignDto { SignType = "" };

        Assert.Equal(0, dto.SignID);
        Assert.Equal(0, dto.SignErrorCode);
        Assert.False(dto.SignEnabled);
        Assert.Equal(0, dto.FrameID);
        Assert.Equal(0, dto.FrameRevision);
        Assert.Equal(0, dto.MessageID);
        Assert.Equal(0, dto.MessageRevision);
        Assert.Equal(0, dto.PlanID);
        Assert.Equal(0, dto.PlanRevision);
        Assert.Equal(0, dto.SignWidth);
        Assert.Equal(0, dto.SignHeight);
    }

    [Fact]
    public void SignDto_SetAllProperties()
    {
        var dto = new SignDto
        {
            SignID = 1,
            SignErrorCode = 2,
            SignEnabled = true,
            FrameID = 3,
            FrameRevision = 4,
            MessageID = 5,
            MessageRevision = 6,
            PlanID = 7,
            PlanRevision = 8,
            SignType = "Text",
            SignWidth = 100,
            SignHeight = 50
        };

        Assert.Equal(1, dto.SignID);
        Assert.Equal(2, dto.SignErrorCode);
        Assert.True(dto.SignEnabled);
        Assert.Equal("Text", dto.SignType);
        Assert.Equal(100, dto.SignWidth);
        Assert.Equal(50, dto.SignHeight);
    }

    #endregion

    #region SignGroupDto Tests

    [Fact]
    public void SignGroupDto_DefaultValues()
    {
        var dto = new SignGroupDto();

        Assert.Equal(0, dto.GroupId);
        Assert.NotNull(dto.Signs);
        Assert.Empty(dto.Signs);
        Assert.Null(dto.Signature);
    }

    [Fact]
    public void SignGroupDto_SetAllProperties()
    {
        var dto = new SignGroupDto
        {
            GroupId = 1,
            Signature = "SIG",
            Signs = new Dictionary<byte, SignDto>
            {
                { 1, new SignDto { SignID = 1, SignType = "Text" } }
            }
        };

        Assert.Equal(1, dto.GroupId);
        Assert.Equal("SIG", dto.Signature);
        Assert.Single(dto.Signs);
    }

    #endregion

    #region SignControllerDto Tests

    [Fact]
    public void SignControllerDto_DefaultValues()
    {
        var dto = new SignControllerDto();

        Assert.False(dto.OnlineStatus);
        Assert.Equal(0, dto.ControllerChecksum);
        Assert.Equal(0, dto.ControllerErrorCode);
        Assert.Equal(0, dto.NumberOfGroups);
        Assert.NotNull(dto.Groups);
        Assert.Empty(dto.Groups);
    }

    #endregion

    #region FaultLogEntryDto Tests

    [Fact]
    public void FaultLogEntryDto_SetAllProperties()
    {
        var dto = new FaultLogEntryDto
        {
            Id = 1,
            EntryNumber = 10,
            ErrorCode = 0x01,
            ErrorDescription = "Power failure",
            IsFaultCleared = true,
            EntryDateTime = new DateTime(2024, 6, 15)
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.EntryNumber);
        Assert.Equal(0x01, dto.ErrorCode);
        Assert.Equal("Power failure", dto.ErrorDescription);
        Assert.True(dto.IsFaultCleared);
    }

    #endregion

    #region SignStatusDto Tests

    [Fact]
    public void SignStatusDto_SetAllProperties()
    {
        var dto = new SignStatusDto
        {
            SignID = 1,
            SignErrorCode = 0,
            SignError = "No error",
            SignEnabled = true,
            FrameID = 10,
            FrameRevision = 1,
            MessageID = 20,
            MessageRevision = 2,
            PlanID = 30,
            PlanRevision = 3
        };

        Assert.Equal(1, dto.SignID);
        Assert.Equal("No error", dto.SignError);
        Assert.True(dto.SignEnabled);
    }

    #endregion

    #region SignStatusReplyDto Tests

    [Fact]
    public void SignStatusReplyDto_DefaultValues()
    {
        var dto = new SignStatusReplyDto { ControllerError = "" };

        Assert.False(dto.OnlineStatus);
        Assert.Equal(0, dto.ApplicationErrorCode);
        Assert.Equal(0, dto.ControllerChecksum);
        Assert.Equal(0, dto.ControllerErrorCode);
        Assert.Equal(0, dto.NumberOfSigns);
        Assert.NotNull(dto.Signs);
        Assert.Empty(dto.Signs);
    }

    #endregion

    #region SignExtendedStatusReplyDto Tests

    [Fact]
    public void SignExtendedStatusReplyDto_DefaultValues()
    {
        var dto = new SignExtendedStatusReplyDto
        {
            ApplicationError = "",
            ControllerError = ""
        };

        Assert.False(dto.OnlineStatus);
        Assert.Equal(0, dto.ApplicationErrorCode);
        Assert.Equal(0, dto.ControllerErrorCode);
        Assert.Equal(0, dto.NumberOfSigns);
        Assert.NotNull(dto.Signs);
    }

    #endregion

    #region SignExtendedStatusDto Tests

    [Fact]
    public void SignExtendedStatusDto_SetAllProperties()
    {
        var dto = new SignExtendedStatusDto
        {
            SignID = 1,
            SignType = 0,
            SignTypeDescription = "Text",
            NumberOfRows = 10,
            NumberOfColumns = 20,
            SignErrorCode = 0,
            SignError = "No error",
            DimmingMode = 0,
            DimmingModeDescription = "Automatic",
            LuminanceLevel = 8,
            LampLedStatus = "FF00"
        };

        Assert.Equal(1, dto.SignID);
        Assert.Equal("Text", dto.SignTypeDescription);
        Assert.Equal("Automatic", dto.DimmingModeDescription);
        Assert.Equal(8, dto.LuminanceLevel);
    }

    #endregion

    #region SignSetTextFrameDto Tests

    [Fact]
    public void SignSetTextFrameDto_SetAllProperties()
    {
        var dto = new SignSetTextFrameDto
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            Text = "HELLO"
        };

        Assert.Equal(1, dto.FrameID);
        Assert.Equal(2, dto.Revision);
        Assert.Equal(3, dto.Font);
        Assert.Equal(4, dto.Colour);
        Assert.Equal(5, dto.Conspicuity);
        Assert.Equal("HELLO", dto.Text);
    }

    #endregion

    #region SignSetGraphicsFrameDto Tests

    [Fact]
    public void SignSetGraphicsFrameDto_SetAllProperties()
    {
        var dto = new SignSetGraphicsFrameDto
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 10,
            NumberOfColumns = 20,
            Colour = 3,
            Conspicuity = 4,
            GraphicsData = "FF00FF00"
        };

        Assert.Equal(1, dto.FrameID);
        Assert.Equal(10, dto.NumberOfRows);
        Assert.Equal(20, dto.NumberOfColumns);
        Assert.Equal("FF00FF00", dto.GraphicsData);
    }

    #endregion

    #region SignSetMessageDto Tests

    [Fact]
    public void SignSetMessageDto_DefaultValues()
    {
        var dto = new SignSetMessageDto();

        Assert.Equal(0, dto.MessageID);
        Assert.Equal(0, dto.Revision);
        Assert.Equal(0, dto.TransitionTimeBetweenFrames);
        Assert.Equal(0, dto.Frame1ID);
        Assert.Equal(0, dto.Frame1Time);
        Assert.Equal(0, dto.Frame6ID);
        Assert.Equal(0, dto.Frame6Time);
    }

    #endregion

    #region ExtendedRequestMessageDto Tests

    [Fact]
    public void ExtendedRequestMessageDto_DefaultValues()
    {
        var dto = new ExtendedRequestMessageDto();

        Assert.Equal(0, dto.TransitionTimeBetweenFrames);
        Assert.Null(dto.Frame1);
        Assert.Null(dto.Frame2);
        Assert.Null(dto.Frame3);
        Assert.Null(dto.Frame4);
        Assert.Null(dto.Frame5);
        Assert.Null(dto.Frame6);
    }

    [Fact]
    public void ExtendedRequestMessageDto_SetFrames()
    {
        var dto = new ExtendedRequestMessageDto
        {
            Frame1 = new ExtendedTextFrameDto { Font = 1, Colour = 2, Conspicuity = 3, Text = "Test" },
            Frame1Time = 100
        };

        Assert.NotNull(dto.Frame1);
        Assert.Equal("Test", dto.Frame1.Text);
        Assert.Equal(100, dto.Frame1Time);
    }

    #endregion

    #region ExtendedTextFrameDto Tests

    [Fact]
    public void ExtendedTextFrameDto_SetAllProperties()
    {
        var dto = new ExtendedTextFrameDto
        {
            Font = 1,
            Colour = 2,
            Conspicuity = 3,
            Text = "Hello World"
        };

        Assert.Equal(1, dto.Font);
        Assert.Equal(2, dto.Colour);
        Assert.Equal(3, dto.Conspicuity);
        Assert.Equal("Hello World", dto.Text);
    }

    #endregion

    #region SignDisplayMessageDto Tests

    [Fact]
    public void SignDisplayMessageDto_SetAllProperties()
    {
        var dto = new SignDisplayMessageDto
        {
            GroupID = 1,
            MessageID = 10
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.MessageID);
    }

    #endregion

    #region SignDisplayFrameDto Tests

    [Fact]
    public void SignDisplayFrameDto_SetAllProperties()
    {
        var dto = new SignDisplayFrameDto
        {
            SignID = 1,
            FrameID = 10
        };

        Assert.Equal(1, dto.SignID);
        Assert.Equal(10, dto.FrameID);
    }

    #endregion

    #region SignDisplayAtomicFrameDto Tests

    [Fact]
    public void SignDisplayAtomicFrameDto_DefaultValues()
    {
        var dto = new SignDisplayAtomicFrameDto();

        Assert.Equal(0, dto.GroupID);
        Assert.Equal(0, dto.NumbeOfSigns);
        Assert.NotNull(dto.Frames);
        Assert.Empty(dto.Frames);
    }

    #endregion

    #region AckReplyDto Tests

    [Fact]
    public void AckReplyDto_CanBeCreated()
    {
        var dto = new AckReplyDto();
        Assert.NotNull(dto);
    }

    #endregion

    #region SignRequestStoredFrameMessagePlanDto Tests

    [Fact]
    public void SignRequestStoredFrameMessagePlanDto_SetAllProperties()
    {
        var dto = new SignRequestStoredFrameMessagePlanDto
        {
            TypeRequest = 1,
            RequestID = 10
        };

        Assert.Equal(1, dto.TypeRequest);
        Assert.Equal(10, dto.RequestID);
    }

    #endregion

    #region PowerOnOffCommandDto Tests

    [Fact]
    public void PowerOnOffCommandDto_SetAllProperties()
    {
        var dto = new PowerOnOffCommandDto
        {
            GroupID = 1,
            PoweredOn = true
        };

        Assert.Equal(1, dto.GroupID);
        Assert.True(dto.PoweredOn);
    }

    #endregion

    #region DisableEnableDeviceCommandDto Tests

    [Fact]
    public void DisableEnableDeviceCommandDto_DefaultValues()
    {
        var dto = new DisableEnableDeviceCommandDto();

        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    [Fact]
    public void DisableEnableDeviceEntryDto_SetAllProperties()
    {
        var dto = new DisableEnableDeviceEntryDto
        {
            GroupID = 1,
            Enabled = true
        };

        Assert.Equal(1, dto.GroupID);
        Assert.True(dto.Enabled);
    }

    #endregion

    #region RejectReplyDto Tests

    [Fact]
    public void RejectReplyDto_SetAllProperties()
    {
        var dto = new RejectReplyDto
        {
            ApplicationErrorCode = 0x02,
            ApplicationErrorDescription = "Syntax error"
        };

        Assert.Equal(0x02, dto.ApplicationErrorCode);
        Assert.Equal("Syntax error", dto.ApplicationErrorDescription);
    }

    #endregion

    #region SystemResetCommandDto Tests

    [Fact]
    public void SystemResetCommandDto_SetAllProperties()
    {
        var dto = new SystemResetCommandDto
        {
            GroupID = 1,
            ResetLevel = 2
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(2, dto.ResetLevel);
    }

    #endregion

    #region UpdateTimeCommandDto Tests

    [Fact]
    public void UpdateTimeCommandDto_NullDateTime()
    {
        var dto = new UpdateTimeCommandDto
        {
            DateTime = null
        };

        Assert.Null(dto.DateTime);
    }

    [Fact]
    public void UpdateTimeCommandDto_SetDateTime()
    {
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 0);
        var dto = new UpdateTimeCommandDto
        {
            DateTime = dateTime
        };

        Assert.Equal(dateTime, dto.DateTime);
    }

    #endregion

    #region EnablePlanCommandDto Tests

    [Fact]
    public void EnablePlanCommandDto_SetAllProperties()
    {
        var dto = new EnablePlanCommandDto
        {
            GroupID = 1,
            PlanID = 10
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.PlanID);
    }

    #endregion

    #region DisablePlanCommandDto Tests

    [Fact]
    public void DisablePlanCommandDto_SetAllProperties()
    {
        var dto = new DisablePlanCommandDto
        {
            GroupID = 1,
            PlanID = 10
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.PlanID);
    }

    #endregion

    #region ReportEnabledPlansDto Tests

    [Fact]
    public void ReportEnabledPlansDto_DefaultValues()
    {
        var dto = new ReportEnabledPlansDto();

        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    #endregion

    #region EnabledPlanEntryDto Tests

    [Fact]
    public void EnabledPlanEntryDto_SetAllProperties()
    {
        var dto = new EnabledPlanEntryDto
        {
            GroupID = 1,
            PlanID = 10
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.PlanID);
    }

    #endregion

    #region SignSetDimmingLevelCommandDto Tests

    [Fact]
    public void SignSetDimmingLevelCommandDto_DefaultValues()
    {
        var dto = new SignSetDimmingLevelCommandDto();

        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    [Fact]
    public void DimmingLevelEntryDto_SetAllProperties()
    {
        var dto = new DimmingLevelEntryDto
        {
            GroupID = 1,
            DimmingMode = 1,
            LuminanceLevel = 8
        };

        Assert.Equal(1, dto.GroupID);
        Assert.Equal(1, dto.DimmingMode);
        Assert.Equal(8, dto.LuminanceLevel);
    }

    #endregion

    #region SignSetHighResolutionGraphicsFrameDto Tests

    [Fact]
    public void SignSetHighResolutionGraphicsFrameDto_SetAllProperties()
    {
        var dto = new SignSetHighResolutionGraphicsFrameDto
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 1000,
            NumberOfColumns = 2000,
            Colour = 0x0E,
            Conspicuity = 3,
            GraphicsData = "FF00FF00"
        };

        Assert.Equal(1, dto.FrameID);
        Assert.Equal(1000, dto.NumberOfRows);
        Assert.Equal(2000, dto.NumberOfColumns);
        Assert.Equal(0x0E, dto.Colour);
    }

    #endregion

    #region SignSetPlanDto Tests

    [Fact]
    public void SignSetPlanDto_DefaultValues()
    {
        var dto = new SignSetPlanDto();

        Assert.Equal(0, dto.PlanID);
        Assert.Equal(0, dto.Revision);
        Assert.Equal(0, dto.DayOfWeek);
        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    [Fact]
    public void SignSetPlanEntryDto_SetAllProperties()
    {
        var dto = new SignSetPlanEntryDto
        {
            FrameMessageType = 1,
            FrameMessageID = 10,
            StartHour = 8,
            StartMinute = 30,
            StopHour = 17,
            StopMinute = 45
        };

        Assert.Equal(1, dto.FrameMessageType);
        Assert.Equal(10, dto.FrameMessageID);
        Assert.Equal(8, dto.StartHour);
        Assert.Equal(30, dto.StartMinute);
        Assert.Equal(17, dto.StopHour);
        Assert.Equal(45, dto.StopMinute);
    }

    #endregion

    #region HAR DTOs Tests

    [Fact]
    public void HARStatusReplyDto_SetAllProperties()
    {
        var dto = new HARStatusReplyDto
        {
            OnlineStatus = true,
            ApplicationErrorCode = 0,
            ApplicationError = "No error",
            DateTime = new DateTime(2024, 6, 15),
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0,
            ControllerError = "No error",
            HAREnabled = true,
            VoiceIDPlaying = 100,
            VoiceRevision = 1,
            StrategyIDActive = 200,
            StrategyRevision = 2,
            StrategyStatus = 1,
            StrategyStatusDescription = "Playing"
        };

        Assert.True(dto.OnlineStatus);
        Assert.True(dto.HAREnabled);
        Assert.Equal(100, dto.VoiceIDPlaying);
        Assert.Equal("Playing", dto.StrategyStatusDescription);
    }

    [Fact]
    public void HARSetStrategyCommandDto_DefaultValues()
    {
        var dto = new HARSetStrategyCommandDto();

        Assert.Equal(0, dto.StrategyID);
        Assert.Equal(0, dto.Revision);
        Assert.NotNull(dto.VoiceIDs);
        Assert.Empty(dto.VoiceIDs);
    }

    [Fact]
    public void HARSetStrategyReplyDto_SetAllProperties()
    {
        var dto = new HARSetStrategyReplyDto
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3 }
        };

        Assert.Equal(100, dto.StrategyID);
        Assert.Equal(3, dto.VoiceIDs.Count);
    }

    [Fact]
    public void HARActivateStrategyCommandDto_SetAllProperties()
    {
        var dto = new HARActivateStrategyCommandDto
        {
            StrategyID = 100
        };

        Assert.Equal(100, dto.StrategyID);
    }

    [Fact]
    public void HARSetPlanCommandDto_DefaultValues()
    {
        var dto = new HARSetPlanCommandDto();

        Assert.Equal(0, dto.PlanID);
        Assert.Equal(0, dto.Revision);
        Assert.Equal(0, dto.DayOfWeek);
        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    [Fact]
    public void HARSetPlanEntryDto_SetAllProperties()
    {
        var dto = new HARSetPlanEntryDto
        {
            StrategyID = 100,
            StartHour = 8,
            StartMinute = 30,
            StopHour = 17,
            StopMinute = 45
        };

        Assert.Equal(100, dto.StrategyID);
        Assert.Equal(8, dto.StartHour);
        Assert.Equal(30, dto.StartMinute);
        Assert.Equal(17, dto.StopHour);
        Assert.Equal(45, dto.StopMinute);
    }

    [Fact]
    public void HARSetPlanReplyDto_DefaultValues()
    {
        var dto = new HARSetPlanReplyDto();

        Assert.Equal(0, dto.PlanID);
        Assert.Equal(0, dto.Revision);
        Assert.Equal(0, dto.DayOfWeek);
        Assert.NotNull(dto.Entries);
        Assert.Empty(dto.Entries);
    }

    [Fact]
    public void HARRequestStoredCommandDto_SetAllProperties()
    {
        var dto = new HARRequestStoredCommandDto
        {
            RequestType = 1,
            RequestID = 100,
            SequenceNumber = 5
        };

        Assert.Equal(1, dto.RequestType);
        Assert.Equal(100, dto.RequestID);
        Assert.Equal(5, dto.SequenceNumber);
    }

    #endregion
}
