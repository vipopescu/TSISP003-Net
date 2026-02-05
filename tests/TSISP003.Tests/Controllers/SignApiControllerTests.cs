using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TSISP003.Configuration;
using TSISP003.Controllers;
using TSISP003.Domain.Entities;
using TSISP003.DTOs;
using TSISP003.Services;
using TSISP003.Utilities;

namespace TSISP003.Tests.Controllers;

/// <summary>
/// Helper class to create testable instances of SignApiController
/// </summary>
public class TestableSignApiController : SignApiController
{
    private readonly Dictionary<string, ISignControllerService> _testServices;

    public TestableSignApiController(
        ILogger<SignApiController> logger,
        SignControllerServiceFactory factory,
        Dictionary<string, ISignControllerService> testServices)
        : base(logger, factory)
    {
        _testServices = testServices;
    }
}

/// <summary>
/// Tests for SignApiController focusing on error handling and response formatting
/// </summary>
public class SignApiControllerTests
{
    private readonly Mock<ILogger<SignApiController>> _mockLogger;

    public SignApiControllerTests()
    {
        _mockLogger = new Mock<ILogger<SignApiController>>();
    }

    #region DTO Mapping Tests

    [Fact]
    public void SignSetTextFrameDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignSetTextFrameDto
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            Text = "TEST"
        };

        // Assert
        Assert.Equal(1, dto.FrameID);
        Assert.Equal(2, dto.Revision);
        Assert.Equal(3, dto.Font);
        Assert.Equal(4, dto.Colour);
        Assert.Equal(5, dto.Conspicuity);
        Assert.Equal("TEST", dto.Text);
    }

    [Fact]
    public void SignSetGraphicsFrameDto_CanBeCreated()
    {
        // Arrange & Act
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

        // Assert
        Assert.Equal(1, dto.FrameID);
        Assert.Equal(10, dto.NumberOfRows);
        Assert.Equal(20, dto.NumberOfColumns);
        Assert.Equal("FF00FF00", dto.GraphicsData);
    }

    [Fact]
    public void SignSetHighResolutionGraphicsFrameDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignSetHighResolutionGraphicsFrameDto
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 1000,
            NumberOfColumns = 2000,
            Colour = 0x0E,
            Conspicuity = 4,
            GraphicsData = "FF00FF00"
        };

        // Assert
        Assert.Equal(1, dto.FrameID);
        Assert.Equal((ushort)1000, dto.NumberOfRows);
        Assert.Equal((ushort)2000, dto.NumberOfColumns);
        Assert.Equal(0x0E, dto.Colour);
    }

    [Fact]
    public void SignSetMessageDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignSetMessageDto
        {
            MessageID = 1,
            Revision = 2,
            Frame1ID = 10,
            Frame1Time = 50
        };

        // Assert
        Assert.Equal(1, dto.MessageID);
        Assert.Equal(2, dto.Revision);
        Assert.Equal(10, dto.Frame1ID);
        Assert.Equal(50, dto.Frame1Time);
    }

    [Fact]
    public void SignSetPlanDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignSetPlanDto
        {
            PlanID = 1,
            Revision = 2,
            DayOfWeek = 0x7F
        };

        // Assert
        Assert.Equal(1, dto.PlanID);
        Assert.Equal(2, dto.Revision);
        Assert.Equal(0x7F, dto.DayOfWeek);
    }

    [Fact]
    public void SignDisplayFrameDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignDisplayFrameDto
        {
            SignID = 1,
            FrameID = 10
        };

        // Assert
        Assert.Equal(1, dto.SignID);
        Assert.Equal(10, dto.FrameID);
    }

    [Fact]
    public void SignDisplayMessageDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignDisplayMessageDto
        {
            GroupID = 1,
            MessageID = 10
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.MessageID);
    }

    [Fact]
    public void SignDisplayAtomicFrameDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignDisplayAtomicFrameDto
        {
            GroupID = 1,
            Frames = new List<SignDisplayFrameDto>
            {
                new SignDisplayFrameDto { SignID = 1, FrameID = 10 },
                new SignDisplayFrameDto { SignID = 2, FrameID = 20 }
            }
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.Equal(2, dto.Frames.Count);
    }

    #endregion

    #region Command DTO Tests

    [Fact]
    public void SystemResetCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SystemResetCommandDto
        {
            GroupID = 1,
            ResetLevel = 2
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.Equal(2, dto.ResetLevel);
    }

    [Fact]
    public void UpdateTimeCommandDto_CanBeCreated()
    {
        // Arrange
        var time = new DateTime(2024, 6, 15, 14, 30, 0);

        // Act
        var dto = new UpdateTimeCommandDto
        {
            DateTime = time
        };

        // Assert
        Assert.Equal(time, dto.DateTime);
    }

    [Fact]
    public void EnablePlanCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new EnablePlanCommandDto
        {
            GroupID = 1,
            PlanID = 10
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.PlanID);
    }

    [Fact]
    public void DisablePlanCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new DisablePlanCommandDto
        {
            GroupID = 1,
            PlanID = 10
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.Equal(10, dto.PlanID);
    }

    [Fact]
    public void PowerOnOffCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new PowerOnOffCommandDto
        {
            GroupID = 1,
            PoweredOn = true
        };

        // Assert
        Assert.Equal(1, dto.GroupID);
        Assert.True(dto.PoweredOn);
    }

    [Fact]
    public void SignSetDimmingLevelCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignSetDimmingLevelCommandDto
        {
            Entries = new List<DimmingLevelEntryDto>
            {
                new DimmingLevelEntryDto { GroupID = 1, DimmingMode = 0, LuminanceLevel = 8 },
                new DimmingLevelEntryDto { GroupID = 2, DimmingMode = 1, LuminanceLevel = 16 }
            }
        };

        // Assert
        Assert.Equal(2, dto.Entries.Count);
        Assert.Equal(1, dto.Entries[0].GroupID);
        Assert.Equal(0, dto.Entries[0].DimmingMode);
        Assert.Equal(8, dto.Entries[0].LuminanceLevel);
    }

    [Fact]
    public void DisableEnableDeviceCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new DisableEnableDeviceCommandDto
        {
            Entries = new List<DisableEnableDeviceEntryDto>
            {
                new DisableEnableDeviceEntryDto { GroupID = 1, Enabled = true },
                new DisableEnableDeviceEntryDto { GroupID = 2, Enabled = false }
            }
        };

        // Assert
        Assert.Equal(2, dto.Entries.Count);
        Assert.Equal(1, dto.Entries[0].GroupID);
        Assert.True(dto.Entries[0].Enabled);
        Assert.False(dto.Entries[1].Enabled);
    }

    #endregion

    #region HAR Command DTO Tests

    [Fact]
    public void HARSetStrategyCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new HARSetStrategyCommandDto
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3, 4, 5 }
        };

        // Assert
        Assert.Equal((ushort)100, dto.StrategyID);
        Assert.Equal(1, dto.Revision);
        Assert.Equal(5, dto.VoiceIDs.Count);
    }

    [Fact]
    public void HARActivateStrategyCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new HARActivateStrategyCommandDto
        {
            StrategyID = 100
        };

        // Assert
        Assert.Equal((ushort)100, dto.StrategyID);
    }

    [Fact]
    public void HARSetPlanCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new HARSetPlanCommandDto
        {
            PlanID = 1,
            Revision = 2,
            DayOfWeek = 0x7F,
            Entries = new List<HARSetPlanEntryDto>
            {
                new HARSetPlanEntryDto
                {
                    StrategyID = 100,
                    StartHour = 8,
                    StartMinute = 0,
                    StopHour = 17,
                    StopMinute = 0
                }
            }
        };

        // Assert
        Assert.Equal(1, dto.PlanID);
        Assert.Equal(2, dto.Revision);
        Assert.Equal(0x7F, dto.DayOfWeek);
        Assert.Single(dto.Entries);
    }

    [Fact]
    public void HARRequestStoredCommandDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new HARRequestStoredCommandDto
        {
            RequestType = 1,
            RequestID = 100,
            SequenceNumber = 0
        };

        // Assert
        Assert.Equal(1, dto.RequestType);
        Assert.Equal((ushort)100, dto.RequestID);
        Assert.Equal(0, dto.SequenceNumber);
    }

    #endregion

    #region Response DTO Tests

    [Fact]
    public void SignStatusReplyDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SignStatusReplyDto
        {
            OnlineStatus = true,
            ApplicationErrorCode = 0,
            dateTime = new DateTime(2024, 6, 15, 14, 30, 0),
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0,
            ControllerError = "No error",
            NumberOfSigns = 2
        };

        // Assert
        Assert.True(dto.OnlineStatus);
        Assert.Equal(0, dto.ApplicationErrorCode);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 0), dto.dateTime);
    }

    [Fact]
    public void FaultLogEntryDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new FaultLogEntryDto
        {
            EntryNumber = 1,
            EntryDateTime = new DateTime(2024, 6, 15, 14, 30, 0),
            ErrorCode = 0x01,
            ErrorDescription = "Test error",
            IsFaultCleared = false
        };

        // Assert
        Assert.Equal(1, dto.EntryNumber);
        Assert.Equal(0x01, dto.ErrorCode);
        Assert.False(dto.IsFaultCleared);
    }

    [Fact]
    public void RejectReplyDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new RejectReplyDto
        {
            ApplicationErrorCode = 0x21
        };

        // Assert
        Assert.Equal(0x21, dto.ApplicationErrorCode);
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void SignRequestRejectedException_CanBeCreated()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x21 };

        // Act
        var exception = new SignRequestRejectedException(rejectReply);

        // Assert
        Assert.NotNull(exception.RejectReply);
        Assert.Equal(0x21, exception.RejectReply.ApplicationErrorCode);
    }

    [Fact]
    public void SignRequestRejectedException_ContainsErrorCodeInMessage()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x21 };

        // Act
        var exception = new SignRequestRejectedException(rejectReply);

        // Assert
        Assert.Contains("33", exception.Message); // 0x21 = 33
    }

    #endregion

    #region Entity to DTO Extension Tests

    [Fact]
    public void FaultLogEntry_AsDto_MapsCorrectly()
    {
        // Arrange
        var entity = new FaultLogEntry
        {
            EntryNumber = 1,
            Day = 15,
            Month = 6,
            Year = 2024,
            Hour = 14,
            Minute = 30,
            Second = 45,
            ErrorCode = 0x01,
            IsFaultCleared = true
        };

        // Act
        var dto = entity.AsDto();

        // Assert
        Assert.Equal(1, dto.EntryNumber);
        Assert.Equal(0x01, dto.ErrorCode);
        Assert.True(dto.IsFaultCleared);
    }

    [Fact]
    public void RejectReply_AsDto_MapsCorrectly()
    {
        // Arrange
        var entity = new RejectReply { ApplicationErrorCode = 0x21 };

        // Act
        var dto = entity.AsDto();

        // Assert
        Assert.Equal(0x21, dto.ApplicationErrorCode);
    }

    [Fact]
    public void SignStatusReply_AsDto_MapsCorrectly()
    {
        // Arrange
        var entity = new SignStatusReply
        {
            OnlineStatus = true,
            ApplicationErrorCode = 0,
            Day = 15,
            Month = 6,
            Year = 2024,
            Hour = 14,
            Minute = 30,
            Second = 45,
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0,
            NumberOfSigns = 2
        };

        // Act
        var dto = entity.AsDto();

        // Assert
        Assert.True(dto.OnlineStatus);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 45), dto.dateTime);
    }

    [Fact]
    public void AckReply_AsDto_ReturnsObject()
    {
        // Arrange
        var entity = new AckReply();

        // Act
        var dto = entity.AsDto();

        // Assert
        Assert.NotNull(dto);
    }

    #endregion

    #region DTO to Entity Extension Tests

    [Fact]
    public void SignSetTextFrameDto_AsEntity_MapsCorrectly()
    {
        // Arrange
        var dto = new SignSetTextFrameDto
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            Text = "TEST"
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(1, entity.FrameID);
        Assert.Equal(2, entity.Revision);
        Assert.Equal(3, entity.Font);
        Assert.Equal(4, entity.Colour);
        Assert.Equal(5, entity.Conspicuity);
    }

    [Fact]
    public void SignSetMessageDto_AsEntity_MapsCorrectly()
    {
        // Arrange
        var dto = new SignSetMessageDto
        {
            MessageID = 1,
            Revision = 2,
            Frame1ID = 10,
            Frame1Time = 50
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(1, entity.MessageID);
        Assert.Equal(2, entity.Revision);
        Assert.Equal(10, entity.Frame1ID);
        Assert.Equal(50, entity.Frame1Time);
    }

    [Fact]
    public void SignDisplayFrameDto_AsEntity_MapsCorrectly()
    {
        // Arrange
        var dto = new SignDisplayFrameDto
        {
            SignID = 1,
            FrameID = 10
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(1, entity.SignID);
        Assert.Equal(10, entity.FrameID);
    }

    [Fact]
    public void SignDisplayMessageDto_AsEntity_MapsCorrectly()
    {
        // Arrange
        var dto = new SignDisplayMessageDto
        {
            GroupID = 1,
            MessageID = 10
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(1, entity.GroupID);
        Assert.Equal(10, entity.MessageID);
    }

    #endregion
}
