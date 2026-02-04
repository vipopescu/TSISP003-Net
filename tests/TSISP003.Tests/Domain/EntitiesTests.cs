using TSISP003.Domain.Entities;
using static TSISP003.Services.SignControllerServiceConfig;

namespace TSISP003.Tests.Domain;

public class EntitiesTests
{
    #region Sign Tests

    [Fact]
    public void Sign_DefaultValues()
    {
        // Act
        var sign = new Sign();

        // Assert
        Assert.Equal(0, sign.SignID);
        Assert.Equal(0, sign.SignErrorCode);
        Assert.False(sign.SignEnabled);
        Assert.Equal(0, sign.FrameID);
        Assert.Equal(0, sign.FrameRevision);
        Assert.Equal(0, sign.MessageID);
        Assert.Equal(0, sign.MessageRevision);
        Assert.Equal(0, sign.PlanID);
        Assert.Equal(0, sign.PlanRevision);
        Assert.Equal(0, sign.SignWidth);
        Assert.Equal(0, sign.SignHeight);
    }

    [Fact]
    public void Sign_SetAllProperties()
    {
        // Arrange & Act
        var sign = new Sign
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
            SignType = SignType.SING_TYPE_GRAPHIC_MONOCOLOR,
            SignWidth = 100,
            SignHeight = 50
        };

        // Assert
        Assert.Equal(1, sign.SignID);
        Assert.Equal(2, sign.SignErrorCode);
        Assert.True(sign.SignEnabled);
        Assert.Equal(3, sign.FrameID);
        Assert.Equal(4, sign.FrameRevision);
        Assert.Equal(5, sign.MessageID);
        Assert.Equal(6, sign.MessageRevision);
        Assert.Equal(7, sign.PlanID);
        Assert.Equal(8, sign.PlanRevision);
        Assert.Equal(SignType.SING_TYPE_GRAPHIC_MONOCOLOR, sign.SignType);
        Assert.Equal(100, sign.SignWidth);
        Assert.Equal(50, sign.SignHeight);
    }

    #endregion

    #region SignGroup Tests

    [Fact]
    public void SignGroup_DefaultValues()
    {
        // Act
        var group = new SignGroup();

        // Assert
        Assert.Equal(0, group.GroupID);
        Assert.NotNull(group.Signs);
        Assert.Empty(group.Signs);
        Assert.Equal(string.Empty, group.Signature);
    }

    [Fact]
    public void SignGroup_SetAllProperties()
    {
        // Arrange & Act
        var group = new SignGroup
        {
            GroupID = 1,
            Signature = "TEST_SIG",
            Signs = new Dictionary<byte, Sign>
            {
                { 1, new Sign { SignID = 1 } }
            }
        };

        // Assert
        Assert.Equal(1, group.GroupID);
        Assert.Equal("TEST_SIG", group.Signature);
        Assert.Single(group.Signs);
    }

    #endregion

    #region SignController Tests

    [Fact]
    public void SignController_DefaultValues()
    {
        // Act
        var controller = new SignController();

        // Assert
        Assert.False(controller.OnlineStatus);
        Assert.Equal(default, controller.DateChange);
        Assert.Equal(0, controller.ControllerChecksum);
        Assert.Equal(0, controller.ControllerErrorCode);
        Assert.Equal(0, controller.NumberOfGroups);
        Assert.NotNull(controller.Groups);
        Assert.Empty(controller.Groups);
    }

    [Fact]
    public void SignController_SetAllProperties()
    {
        // Arrange & Act
        var dateTime = new DateTime(2024, 6, 15);
        var controller = new SignController
        {
            OnlineStatus = true,
            DateChange = dateTime,
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0x01,
            NumberOfGroups = 2,
            Groups = new Dictionary<byte, SignGroup>
            {
                { 1, new SignGroup { GroupID = 1 } },
                { 2, new SignGroup { GroupID = 2 } }
            }
        };

        // Assert
        Assert.True(controller.OnlineStatus);
        Assert.Equal(dateTime, controller.DateChange);
        Assert.Equal(0x1234, controller.ControllerChecksum);
        Assert.Equal(0x01, controller.ControllerErrorCode);
        Assert.Equal(2, controller.NumberOfGroups);
        Assert.Equal(2, controller.Groups.Count);
    }

    #endregion

    #region FaultLogEntry Tests

    [Fact]
    public void FaultLogEntry_DefaultValues()
    {
        // Act
        var entry = new FaultLogEntry();

        // Assert
        Assert.Equal(0, entry.Id);
        Assert.Equal(0, entry.EntryNumber);
        Assert.Equal(0, entry.Day);
        Assert.Equal(0, entry.Month);
        Assert.Equal(0, entry.Year);
        Assert.Equal(0, entry.Hour);
        Assert.Equal(0, entry.Minute);
        Assert.Equal(0, entry.Second);
        Assert.Equal(0, entry.ErrorCode);
        Assert.False(entry.IsFaultCleared);
    }

    [Fact]
    public void FaultLogEntry_SetAllProperties()
    {
        // Arrange & Act
        var entry = new FaultLogEntry
        {
            Id = 1,
            EntryNumber = 10,
            Day = 15,
            Month = 6,
            Year = 2024,
            Hour = 14,
            Minute = 30,
            Second = 45,
            ErrorCode = 0x01,
            IsFaultCleared = true
        };

        // Assert
        Assert.Equal(1, entry.Id);
        Assert.Equal(10, entry.EntryNumber);
        Assert.Equal(15, entry.Day);
        Assert.Equal(6, entry.Month);
        Assert.Equal(2024, entry.Year);
        Assert.Equal(14, entry.Hour);
        Assert.Equal(30, entry.Minute);
        Assert.Equal(45, entry.Second);
        Assert.Equal(0x01, entry.ErrorCode);
        Assert.True(entry.IsFaultCleared);
    }

    #endregion

    #region SignStatus Tests

    [Fact]
    public void SignStatus_DefaultValues()
    {
        // Act
        var status = new SignStatus();

        // Assert
        Assert.Equal(0, status.SignID);
        Assert.Equal(0, status.SignErrorCode);
        Assert.False(status.SignEnabled);
        Assert.Equal(0, status.FrameID);
        Assert.Equal(0, status.FrameRevision);
        Assert.Equal(0, status.MessageID);
        Assert.Equal(0, status.MessageRevision);
        Assert.Equal(0, status.PlanID);
        Assert.Equal(0, status.PlanRevision);
    }

    [Fact]
    public void SignStatus_SetAllProperties()
    {
        // Arrange & Act
        var status = new SignStatus
        {
            SignID = 1,
            SignErrorCode = 0x06,
            SignEnabled = true,
            FrameID = 10,
            FrameRevision = 1,
            MessageID = 20,
            MessageRevision = 2,
            PlanID = 30,
            PlanRevision = 3
        };

        // Assert
        Assert.Equal(1, status.SignID);
        Assert.Equal(0x06, status.SignErrorCode);
        Assert.True(status.SignEnabled);
        Assert.Equal(10, status.FrameID);
        Assert.Equal(1, status.FrameRevision);
        Assert.Equal(20, status.MessageID);
        Assert.Equal(2, status.MessageRevision);
        Assert.Equal(30, status.PlanID);
        Assert.Equal(3, status.PlanRevision);
    }

    #endregion

    #region SignStatusReply Tests

    [Fact]
    public void SignStatusReply_DefaultValues()
    {
        // Act
        var reply = new SignStatusReply();

        // Assert
        Assert.False(reply.OnlineStatus);
        Assert.Equal(0, reply.ApplicationErrorCode);
        Assert.Equal(0, reply.Day);
        Assert.Equal(0, reply.Month);
        Assert.Equal(0, reply.Year);
        Assert.Equal(0, reply.Hour);
        Assert.Equal(0, reply.Minute);
        Assert.Equal(0, reply.Second);
        Assert.Equal(0, reply.ControllerChecksum);
        Assert.Equal(0, reply.ControllerErrorCode);
        Assert.Equal(0, reply.NumberOfSigns);
        Assert.NotNull(reply.Signs);
        Assert.Empty(reply.Signs);
    }

    [Fact]
    public void SignStatusReply_SetAllProperties()
    {
        // Arrange & Act
        var reply = new SignStatusReply
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
            NumberOfSigns = 2,
            Signs = new Dictionary<byte, SignStatus>
            {
                { 1, new SignStatus { SignID = 1 } },
                { 2, new SignStatus { SignID = 2 } }
            }
        };

        // Assert
        Assert.True(reply.OnlineStatus);
        Assert.Equal(15, reply.Day);
        Assert.Equal(6, reply.Month);
        Assert.Equal(2024, reply.Year);
        Assert.Equal(2, reply.NumberOfSigns);
        Assert.Equal(2, reply.Signs.Count);
    }

    #endregion

    #region SignSetTextFrame Tests

    [Fact]
    public void SignSetTextFrame_DefaultValues()
    {
        // Act
        var frame = new SignSetTextFrame { Text = "" };

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.Revision);
        Assert.Equal(0, frame.Font);
        Assert.Equal(0, frame.Colour);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal(0, frame.NumberOfCharsInText);
        Assert.Equal(0, frame.CRC);
    }

    [Fact]
    public void SignSetTextFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new SignSetTextFrame
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            NumberOfCharsInText = 10,
            Text = "48454C4C4F",
            CRC = 0x1234
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.Revision);
        Assert.Equal(3, frame.Font);
        Assert.Equal(4, frame.Colour);
        Assert.Equal(5, frame.Conspicuity);
        Assert.Equal(10, frame.NumberOfCharsInText);
        Assert.Equal("48454C4C4F", frame.Text);
        Assert.Equal(0x1234, frame.CRC);
    }

    [Fact]
    public void SignSetTextFrame_ImplementsISignResponse()
    {
        // Arrange
        var frame = new SignSetTextFrame { Text = "" };

        // Assert
        Assert.IsAssignableFrom<ISignResponse>(frame);
    }

    #endregion

    #region SignSetMessage Tests

    [Fact]
    public void SignSetMessage_DefaultValues()
    {
        // Act
        var message = new SignSetMessage();

        // Assert
        Assert.Equal(0, message.MessageID);
        Assert.Equal(0, message.Revision);
        Assert.Equal(0, message.TransitionTimeBetweenFrames);
        Assert.Equal(0, message.Frame1ID);
        Assert.Equal(0, message.Frame1Time);
        Assert.Equal(0, message.Frame6ID);
        Assert.Equal(0, message.Frame6Time);
    }

    [Fact]
    public void SignSetMessage_SetAllProperties()
    {
        // Arrange & Act
        var message = new SignSetMessage
        {
            MessageID = 1,
            Revision = 2,
            TransitionTimeBetweenFrames = 10,
            Frame1ID = 11,
            Frame1Time = 100,
            Frame2ID = 12,
            Frame2Time = 200,
            Frame3ID = 13,
            Frame3Time = 150,
            Frame4ID = 14,
            Frame4Time = 100,
            Frame5ID = 15,
            Frame5Time = 50,
            Frame6ID = 16,
            Frame6Time = 25
        };

        // Assert
        Assert.Equal(1, message.MessageID);
        Assert.Equal(2, message.Revision);
        Assert.Equal(10, message.TransitionTimeBetweenFrames);
        Assert.Equal(11, message.Frame1ID);
        Assert.Equal(100, message.Frame1Time);
        Assert.Equal(16, message.Frame6ID);
        Assert.Equal(25, message.Frame6Time);
    }

    [Fact]
    public void SignSetMessage_ImplementsISignResponse()
    {
        // Arrange
        var message = new SignSetMessage();

        // Assert
        Assert.IsAssignableFrom<ISignResponse>(message);
    }

    #endregion

    #region SignDisplayMessage Tests

    [Fact]
    public void SignDisplayMessage_DefaultValues()
    {
        // Act
        var message = new SignDisplayMessage();

        // Assert
        Assert.Equal(0, message.GroupID);
        Assert.Equal(0, message.MessageID);
    }

    [Fact]
    public void SignDisplayMessage_SetAllProperties()
    {
        // Arrange & Act
        var message = new SignDisplayMessage
        {
            GroupID = 1,
            MessageID = 10
        };

        // Assert
        Assert.Equal(1, message.GroupID);
        Assert.Equal(10, message.MessageID);
    }

    #endregion

    #region SignDisplayFrame Tests

    [Fact]
    public void SignDisplayFrame_DefaultValues()
    {
        // Act
        var frame = new SignDisplayFrame();

        // Assert
        Assert.Equal(0, frame.SignID);
        Assert.Equal(0, frame.FrameID);
    }

    [Fact]
    public void SignDisplayFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new SignDisplayFrame
        {
            SignID = 1,
            FrameID = 10
        };

        // Assert
        Assert.Equal(1, frame.SignID);
        Assert.Equal(10, frame.FrameID);
    }

    #endregion

    #region SignDisplayAtomicFrame Tests

    [Fact]
    public void SignDisplayAtomicFrame_DefaultValues()
    {
        // Act
        var atomic = new SignDisplayAtomicFrame();

        // Assert
        Assert.Equal(0, atomic.GroupID);
        Assert.Equal(0, atomic.NumbeOfSigns);
        Assert.NotNull(atomic.Frames);
        Assert.Empty(atomic.Frames);
    }

    [Fact]
    public void SignDisplayAtomicFrame_SetAllProperties()
    {
        // Arrange & Act
        var atomic = new SignDisplayAtomicFrame
        {
            GroupID = 1,
            NumbeOfSigns = 2,
            Frames = new List<SignDisplayFrame>
            {
                new SignDisplayFrame { SignID = 1, FrameID = 10 },
                new SignDisplayFrame { SignID = 2, FrameID = 20 }
            }
        };

        // Assert
        Assert.Equal(1, atomic.GroupID);
        Assert.Equal(2, atomic.NumbeOfSigns);
        Assert.Equal(2, atomic.Frames.Count);
    }

    #endregion

    #region AckReply Tests

    [Fact]
    public void AckReply_CanBeCreated()
    {
        // Act
        var ack = new AckReply();

        // Assert
        Assert.NotNull(ack);
    }

    #endregion

    #region RejectReply Tests

    [Fact]
    public void RejectReply_DefaultValues()
    {
        // Act
        var reject = new RejectReply();

        // Assert
        Assert.Equal(0, reject.ApplicationErrorCode);
    }

    [Fact]
    public void RejectReply_SetErrorCode()
    {
        // Arrange & Act
        var reject = new RejectReply
        {
            ApplicationErrorCode = 0x21 // Incorrect password
        };

        // Assert
        Assert.Equal(0x21, reject.ApplicationErrorCode);
    }

    #endregion

    #region SignSetGraphicsFrame Tests

    [Fact]
    public void SignSetGraphicsFrame_DefaultValues()
    {
        // Act
        var frame = new SignSetGraphicsFrame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.Revision);
        Assert.Equal(0, frame.NumberOfRows);
        Assert.Equal(0, frame.NumberOfColumns);
        Assert.Equal(0, frame.Colour);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal(0, frame.GraphicsLength);
    }

    [Fact]
    public void SignSetGraphicsFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new SignSetGraphicsFrame
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 10,
            NumberOfColumns = 20,
            Colour = 3,
            Conspicuity = 4,
            GraphicsLength = 100,
            GraphicsData = "FF00FF00"
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.Revision);
        Assert.Equal(10, frame.NumberOfRows);
        Assert.Equal(20, frame.NumberOfColumns);
        Assert.Equal(100, frame.GraphicsLength);
        Assert.Equal("FF00FF00", frame.GraphicsData);
    }

    #endregion

    #region SignSetHighResolutionGraphicsFrame Tests

    [Fact]
    public void SignSetHighResolutionGraphicsFrame_DefaultValues()
    {
        // Act
        var frame = new SignSetHighResolutionGraphicsFrame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.Revision);
        Assert.Equal((ushort)0, frame.NumberOfRows);
        Assert.Equal((ushort)0, frame.NumberOfColumns);
        Assert.Equal(0, frame.Colour);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal((uint)0, frame.GraphicsLength);
    }

    [Fact]
    public void SignSetHighResolutionGraphicsFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new SignSetHighResolutionGraphicsFrame
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 1000,
            NumberOfColumns = 2000,
            Colour = 0x0E, // 24-bit RGB
            Conspicuity = 4,
            GraphicsLength = 10000,
            GraphicsData = "FF00FF00FF00FF00"
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(1000, frame.NumberOfRows);
        Assert.Equal(2000, frame.NumberOfColumns);
        Assert.Equal(0x0E, frame.Colour);
        Assert.Equal((uint)10000, frame.GraphicsLength);
    }

    #endregion

    #region SignSetPlan Tests

    [Fact]
    public void SignSetPlan_DefaultValues()
    {
        // Act
        var plan = new SignSetPlan();

        // Assert
        Assert.Equal(0, plan.PlanID);
        Assert.Equal(0, plan.Revision);
        Assert.Equal(0, plan.DayOfWeek);
        Assert.NotNull(plan.Entries);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void SignSetPlan_SetAllProperties()
    {
        // Arrange & Act
        var plan = new SignSetPlan
        {
            PlanID = 1,
            Revision = 2,
            DayOfWeek = 0x7F, // Daily
            Entries = new List<SignSetPlanEntry>
            {
                new SignSetPlanEntry
                {
                    FrameMessageType = 1,
                    FrameMessageID = 10,
                    StartHour = 8,
                    StartMinute = 0,
                    StopHour = 17,
                    StopMinute = 30
                }
            }
        };

        // Assert
        Assert.Equal(1, plan.PlanID);
        Assert.Equal(2, plan.Revision);
        Assert.Equal(0x7F, plan.DayOfWeek);
        Assert.Single(plan.Entries);
    }

    #endregion

    #region SignSetPlanEntry Tests

    [Fact]
    public void SignSetPlanEntry_DefaultValues()
    {
        // Act
        var entry = new SignSetPlanEntry();

        // Assert
        Assert.Equal(0, entry.FrameMessageType);
        Assert.Equal(0, entry.FrameMessageID);
        Assert.Equal(0, entry.StartHour);
        Assert.Equal(0, entry.StartMinute);
        Assert.Equal(0, entry.StopHour);
        Assert.Equal(0, entry.StopMinute);
    }

    [Fact]
    public void SignSetPlanEntry_SetAllProperties()
    {
        // Arrange & Act
        var entry = new SignSetPlanEntry
        {
            FrameMessageType = 2,
            FrameMessageID = 15,
            StartHour = 9,
            StartMinute = 30,
            StopHour = 18,
            StopMinute = 0
        };

        // Assert
        Assert.Equal(2, entry.FrameMessageType);
        Assert.Equal(15, entry.FrameMessageID);
        Assert.Equal(9, entry.StartHour);
        Assert.Equal(30, entry.StartMinute);
        Assert.Equal(18, entry.StopHour);
        Assert.Equal(0, entry.StopMinute);
    }

    #endregion

    #region ReportEnabledPlans Tests

    [Fact]
    public void ReportEnabledPlans_DefaultValues()
    {
        // Act
        var report = new ReportEnabledPlans();

        // Assert
        Assert.NotNull(report.Entries);
        Assert.Empty(report.Entries);
    }

    [Fact]
    public void ReportEnabledPlans_SetEntries()
    {
        // Arrange & Act
        var report = new ReportEnabledPlans
        {
            Entries = new List<EnabledPlanEntry>
            {
                new EnabledPlanEntry { GroupID = 1, PlanID = 10 },
                new EnabledPlanEntry { GroupID = 2, PlanID = 20 }
            }
        };

        // Assert
        Assert.Equal(2, report.Entries.Count);
    }

    #endregion

    #region EnabledPlanEntry Tests

    [Fact]
    public void EnabledPlanEntry_DefaultValues()
    {
        // Act
        var entry = new EnabledPlanEntry();

        // Assert
        Assert.Equal(0, entry.GroupID);
        Assert.Equal(0, entry.PlanID);
    }

    [Fact]
    public void EnabledPlanEntry_SetAllProperties()
    {
        // Arrange & Act
        var entry = new EnabledPlanEntry
        {
            GroupID = 1,
            PlanID = 10
        };

        // Assert
        Assert.Equal(1, entry.GroupID);
        Assert.Equal(10, entry.PlanID);
    }

    #endregion

    #region SignExtendedStatusReply Tests

    [Fact]
    public void SignExtendedStatusReply_DefaultValues()
    {
        // Act
        var reply = new SignExtendedStatusReply();

        // Assert
        Assert.False(reply.OnlineStatus);
        Assert.Equal(0, reply.ApplicationErrorCode);
        Assert.Equal(0, reply.NumberOfSigns);
        Assert.NotNull(reply.Signs);
        Assert.Empty(reply.Signs);
    }

    [Fact]
    public void SignExtendedStatusReply_SetAllProperties()
    {
        // Arrange & Act
        var reply = new SignExtendedStatusReply
        {
            OnlineStatus = true,
            ApplicationErrorCode = 0,
            ManufacturerCode = "MANU",
            Day = 15,
            Month = 6,
            Year = 2024,
            Hour = 14,
            Minute = 30,
            Second = 45,
            ControllerErrorCode = 0,
            NumberOfSigns = 1,
            Signs = new Dictionary<byte, SignExtendedStatus>
            {
                { 1, new SignExtendedStatus { SignID = 1 } }
            }
        };

        // Assert
        Assert.True(reply.OnlineStatus);
        Assert.Equal("MANU", reply.ManufacturerCode);
        Assert.Single(reply.Signs);
    }

    #endregion

    #region SignExtendedStatus Tests

    [Fact]
    public void SignExtendedStatus_DefaultValues()
    {
        // Act
        var status = new SignExtendedStatus();

        // Assert
        Assert.Equal(0, status.SignID);
        Assert.Equal(0, status.SignType);
        Assert.Equal(0, status.NumberOfRows);
        Assert.Equal(0, status.NumberOfColumns);
        Assert.Equal(0, status.SignErrorCode);
        Assert.Equal(0, status.DimmingMode);
        Assert.Equal(0, status.LuminanceLevel);
    }

    [Fact]
    public void SignExtendedStatus_SetAllProperties()
    {
        // Arrange & Act
        var status = new SignExtendedStatus
        {
            SignID = 1,
            SignType = 1, // Graphics
            NumberOfRows = 10,
            NumberOfColumns = 20,
            SignErrorCode = 0,
            DimmingMode = 1, // Manual
            LuminanceLevel = 8,
            LampLedStatus = "FF00"
        };

        // Assert
        Assert.Equal(1, status.SignID);
        Assert.Equal(1, status.SignType);
        Assert.Equal(10, status.NumberOfRows);
        Assert.Equal(20, status.NumberOfColumns);
        Assert.Equal(1, status.DimmingMode);
        Assert.Equal(8, status.LuminanceLevel);
        Assert.Equal("FF00", status.LampLedStatus);
    }

    #endregion

    #region HARSetStrategy Tests

    [Fact]
    public void HARSetStrategy_DefaultValues()
    {
        // Act
        var strategy = new HARSetStrategy();

        // Assert
        Assert.Equal(0, strategy.StrategyID);
        Assert.Equal(0, strategy.Revision);
        Assert.NotNull(strategy.VoiceIDs);
        Assert.Empty(strategy.VoiceIDs);
    }

    [Fact]
    public void HARSetStrategy_SetAllProperties()
    {
        // Arrange & Act
        var strategy = new HARSetStrategy
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3, 4, 5 }
        };

        // Assert
        Assert.Equal(100, strategy.StrategyID);
        Assert.Equal(1, strategy.Revision);
        Assert.Equal(5, strategy.VoiceIDs.Count);
    }

    #endregion

    #region HARSetPlan Tests

    [Fact]
    public void HARSetPlan_DefaultValues()
    {
        // Act
        var plan = new HARSetPlan();

        // Assert
        Assert.Equal(0, plan.PlanID);
        Assert.Equal(0, plan.Revision);
        Assert.Equal(0, plan.DayOfWeek);
        Assert.NotNull(plan.Entries);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void HARSetPlan_SetAllProperties()
    {
        // Arrange & Act
        var plan = new HARSetPlan
        {
            PlanID = 1,
            Revision = 2,
            DayOfWeek = 0x7F,
            Entries = new List<HARSetPlanEntry>
            {
                new HARSetPlanEntry
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
        Assert.Equal(1, plan.PlanID);
        Assert.Equal(2, plan.Revision);
        Assert.Equal(0x7F, plan.DayOfWeek);
        Assert.Single(plan.Entries);
    }

    #endregion

    #region HARSetPlanEntry Tests

    [Fact]
    public void HARSetPlanEntry_DefaultValues()
    {
        // Act
        var entry = new HARSetPlanEntry();

        // Assert
        Assert.Equal(0, entry.StrategyID);
        Assert.Equal(0, entry.StartHour);
        Assert.Equal(0, entry.StartMinute);
        Assert.Equal(0, entry.StopHour);
        Assert.Equal(0, entry.StopMinute);
    }

    [Fact]
    public void HARSetPlanEntry_SetAllProperties()
    {
        // Arrange & Act
        var entry = new HARSetPlanEntry
        {
            StrategyID = 100,
            StartHour = 8,
            StartMinute = 30,
            StopHour = 17,
            StopMinute = 45
        };

        // Assert
        Assert.Equal(100, entry.StrategyID);
        Assert.Equal(8, entry.StartHour);
        Assert.Equal(30, entry.StartMinute);
        Assert.Equal(17, entry.StopHour);
        Assert.Equal(45, entry.StopMinute);
    }

    #endregion

    #region HARStatusReply Tests

    [Fact]
    public void HARStatusReply_DefaultValues()
    {
        // Act
        var reply = new HARStatusReply();

        // Assert
        Assert.False(reply.OnlineStatus);
        Assert.Equal(0, reply.ApplicationErrorCode);
        Assert.False(reply.HAREnabled);
        Assert.Equal(0, reply.VoiceIDPlaying);
        Assert.Equal(0, reply.VoiceRevision);
        Assert.Equal(0, reply.StrategyIDActive);
        Assert.Equal(0, reply.StrategyRevision);
        Assert.Equal(0, reply.StrategyStatus);
    }

    [Fact]
    public void HARStatusReply_SetAllProperties()
    {
        // Arrange & Act
        var reply = new HARStatusReply
        {
            OnlineStatus = true,
            ApplicationErrorCode = 0,
            Day = 15,
            Month = 6,
            Year = 2024,
            Hour = 14,
            Minute = 30,
            Second = 0,
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0,
            HAREnabled = true,
            VoiceIDPlaying = 100,
            VoiceRevision = 1,
            StrategyIDActive = 200,
            StrategyRevision = 2,
            StrategyStatus = 1
        };

        // Assert
        Assert.True(reply.OnlineStatus);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 0), reply.DateTime);
        Assert.True(reply.HAREnabled);
        Assert.Equal(100, reply.VoiceIDPlaying);
        Assert.Equal(200, reply.StrategyIDActive);
        Assert.Equal(1, reply.StrategyStatus);
    }

    #endregion
}
