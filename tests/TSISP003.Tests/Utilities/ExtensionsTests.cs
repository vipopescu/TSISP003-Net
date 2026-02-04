using TSISP003.Domain.Entities;
using TSISP003.DTOs;
using TSISP003.Utilities;
using static TSISP003.Services.SignControllerServiceConfig;

namespace TSISP003.Tests.Utilities;

public class ExtensionsTests
{
    #region Sign Extensions

    [Fact]
    public void Sign_AsDto_MapsAllProperties()
    {
        // Arrange
        var sign = new Sign
        {
            SignID = 1,
            SignErrorCode = 0,
            SignEnabled = true,
            FrameID = 10,
            FrameRevision = 1,
            MessageID = 20,
            MessageRevision = 2,
            PlanID = 30,
            PlanRevision = 3,
            SignType = SignType.SING_TYPE_TEXT,
            SignWidth = 100,
            SignHeight = 50
        };

        // Act
        var dto = sign.AsDto();

        // Assert
        Assert.Equal(sign.SignID, dto.SignID);
        Assert.Equal(sign.SignErrorCode, dto.SignErrorCode);
        Assert.Equal(sign.SignEnabled, dto.SignEnabled);
        Assert.Equal(sign.FrameID, dto.FrameID);
        Assert.Equal(sign.FrameRevision, dto.FrameRevision);
        Assert.Equal(sign.MessageID, dto.MessageID);
        Assert.Equal(sign.MessageRevision, dto.MessageRevision);
        Assert.Equal(sign.PlanID, dto.PlanID);
        Assert.Equal(sign.PlanRevision, dto.PlanRevision);
        Assert.Equal("SING_TYPE_TEXT", dto.SignType);
        Assert.Equal(sign.SignWidth, dto.SignWidth);
        Assert.Equal(sign.SignHeight, dto.SignHeight);
    }

    [Fact]
    public void Sign_AsDto_DifferentSignTypes()
    {
        // Test Graphics sign type
        var graphicsSign = new Sign { SignType = SignType.SING_TYPE_GRAPHIC_MONOCOLOR };
        Assert.Equal("SING_TYPE_GRAPHIC_MONOCOLOR", graphicsSign.AsDto().SignType);

        // Test AdvancedGraphics sign type
        var advancedSign = new Sign { SignType = SignType.SING_TYPE_GRAPHIC_MULTICOLOR };
        Assert.Equal("SING_TYPE_GRAPHIC_MULTICOLOR", advancedSign.AsDto().SignType);
    }

    #endregion

    #region SignGroup Extensions

    [Fact]
    public void SignGroup_AsDto_MapsAllProperties()
    {
        // Arrange
        var signGroup = new SignGroup
        {
            GroupID = 1,
            Signature = "TEST_SIG",
            Signs = new Dictionary<byte, Sign>
            {
                { 1, new Sign { SignID = 1, SignType = SignType.SING_TYPE_TEXT } },
                { 2, new Sign { SignID = 2, SignType = SignType.SING_TYPE_GRAPHIC_MONOCOLOR } }
            }
        };

        // Act
        var dto = signGroup.AsDto();

        // Assert
        Assert.Equal(signGroup.GroupID, dto.GroupId);
        Assert.Equal(signGroup.Signature, dto.Signature);
        Assert.Equal(2, dto.Signs.Count);
        Assert.Equal(1, dto.Signs[1].SignID);
        Assert.Equal(2, dto.Signs[2].SignID);
    }

    [Fact]
    public void SignGroup_AsDto_EmptySigns_ReturnsEmptyDictionary()
    {
        // Arrange
        var signGroup = new SignGroup
        {
            GroupID = 1,
            Signs = null
        };

        // Act
        var dto = signGroup.AsDto();

        // Assert
        Assert.NotNull(dto.Signs);
        Assert.Empty(dto.Signs);
    }

    #endregion

    #region SignController Extensions

    [Fact]
    public void SignController_AsDto_MapsAllProperties()
    {
        // Arrange
        var controller = new SignController
        {
            OnlineStatus = true,
            DateChange = new DateTime(2024, 1, 15),
            ControllerChecksum = 0x1234,
            ControllerErrorCode = 0,
            NumberOfGroups = 1,
            Groups = new Dictionary<byte, SignGroup>
            {
                { 1, new SignGroup { GroupID = 1 } }
            }
        };

        // Act
        var dto = controller.AsDto();

        // Assert
        Assert.Equal(controller.OnlineStatus, dto.OnlineStatus);
        Assert.Equal(controller.DateChange, dto.DateChange);
        Assert.Equal(controller.ControllerChecksum, dto.ControllerChecksum);
        Assert.Equal(controller.ControllerErrorCode, dto.ControllerErrorCode);
        Assert.Equal(controller.NumberOfGroups, dto.NumberOfGroups);
        Assert.Single(dto.Groups);
    }

    #endregion

    #region FaultLogEntry Extensions

    [Fact]
    public void FaultLogEntry_AsDto_MapsAllProperties()
    {
        // Arrange
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
            ErrorCode = 0x01, // Power failure
            IsFaultCleared = false
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal(entry.Id, dto.Id);
        Assert.Equal(entry.EntryNumber, dto.EntryNumber);
        Assert.Equal(entry.ErrorCode, dto.ErrorCode);
        Assert.Equal("Power failure", dto.ErrorDescription);
        Assert.Equal(entry.IsFaultCleared, dto.IsFaultCleared);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 45), dto.EntryDateTime);
    }

    [Fact]
    public void FaultLogEntry_AsDto_InvalidDate_ReturnsDefaultDate()
    {
        // Arrange
        var entry = new FaultLogEntry
        {
            Day = 32, // Invalid day
            Month = 13, // Invalid month
            Year = 2024,
            ErrorCode = 0x00
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal(new DateTime(1900, 1, 1), dto.EntryDateTime);
    }

    [Fact]
    public void FaultLogEntry_AsDto_UnknownErrorCode_ReturnsUnknown()
    {
        // Arrange
        var entry = new FaultLogEntry
        {
            Day = 1,
            Month = 1,
            Year = 2024,
            ErrorCode = 0xFF // Unknown code
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal("Unknown error code", dto.ErrorDescription);
    }

    #endregion

    #region RejectReply Extensions

    [Fact]
    public void RejectReply_AsDto_MapsAllProperties()
    {
        // Arrange
        var reject = new RejectReply
        {
            ApplicationErrorCode = 0x02 // Syntax error
        };

        // Act
        var dto = reject.AsDto();

        // Assert
        Assert.Equal(reject.ApplicationErrorCode, dto.ApplicationErrorCode);
        Assert.Equal("Syntax error in command", dto.ApplicationErrorDescription);
    }

    [Fact]
    public void RejectReply_AsDto_UnknownErrorCode_ReturnsUnknown()
    {
        // Arrange
        var reject = new RejectReply
        {
            ApplicationErrorCode = 0xFF
        };

        // Act
        var dto = reject.AsDto();

        // Assert
        Assert.Equal("Unknown error code", dto.ApplicationErrorDescription);
    }

    #endregion

    #region SignStatus Extensions

    [Fact]
    public void SignStatus_AsDto_MapsAllProperties()
    {
        // Arrange
        var status = new SignStatus
        {
            SignID = 1,
            SignErrorCode = 0x06, // Sign lamp failure
            SignEnabled = true,
            FrameID = 10,
            FrameRevision = 1,
            MessageID = 20,
            MessageRevision = 2,
            PlanID = 30,
            PlanRevision = 3
        };

        // Act
        var dto = status.AsDto();

        // Assert
        Assert.Equal(status.SignID, dto.SignID);
        Assert.Equal(status.SignErrorCode, dto.SignErrorCode);
        Assert.Equal("Sign lamp failure", dto.SignError);
        Assert.Equal(status.SignEnabled, dto.SignEnabled);
        Assert.Equal(status.FrameID, dto.FrameID);
        Assert.Equal(status.FrameRevision, dto.FrameRevision);
        Assert.Equal(status.MessageID, dto.MessageID);
        Assert.Equal(status.MessageRevision, dto.MessageRevision);
        Assert.Equal(status.PlanID, dto.PlanID);
        Assert.Equal(status.PlanRevision, dto.PlanRevision);
    }

    #endregion

    #region SignStatusReply Extensions

    [Fact]
    public void SignStatusReply_AsDto_MapsAllProperties()
    {
        // Arrange
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
            ControllerErrorCode = 0x00,
            NumberOfSigns = 1,
            Signs = new Dictionary<byte, SignStatus>
            {
                { 1, new SignStatus { SignID = 1, SignErrorCode = 0 } }
            }
        };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal(reply.OnlineStatus, dto.OnlineStatus);
        Assert.Equal(reply.ApplicationErrorCode, dto.ApplicationErrorCode);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 45), dto.dateTime);
        Assert.Equal(reply.ControllerChecksum, dto.ControllerChecksum);
        Assert.Equal(reply.ControllerErrorCode, dto.ControllerErrorCode);
        Assert.Equal("No error", dto.ControllerError);
        Assert.Equal(reply.NumberOfSigns, dto.NumberOfSigns);
        Assert.Single(dto.Signs);
    }

    [Fact]
    public void SignStatusReply_AsDto_InvalidDate_ReturnsDefaultDate()
    {
        // Arrange
        var reply = new SignStatusReply
        {
            Day = 32,
            Month = 13,
            Year = 2024,
            Signs = new Dictionary<byte, SignStatus>()
        };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal(new DateTime(1900, 1, 1), dto.dateTime);
    }

    #endregion

    #region SignExtendedStatusReply Extensions

    [Fact]
    public void SignExtendedStatusReply_AsDto_MapsAllProperties()
    {
        // Arrange
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
                { 1, new SignExtendedStatus { SignID = 1, SignType = 0, DimmingMode = 0, SignErrorCode = 0 } }
            }
        };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal(reply.OnlineStatus, dto.OnlineStatus);
        Assert.Equal(reply.ManufacturerCode, dto.ManufacturerCode);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 45), dto.DateTime);
        Assert.Single(dto.Signs);
    }

    [Fact]
    public void SignExtendedStatusReply_AsDto_InvalidDate_ReturnsDefaultDate()
    {
        // Arrange
        var reply = new SignExtendedStatusReply
        {
            Day = 32,
            Month = 13,
            Year = 2024,
            Signs = new Dictionary<byte, SignExtendedStatus>()
        };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal(new DateTime(1900, 1, 1), dto.DateTime);
    }

    #endregion

    #region SignExtendedStatus Extensions

    [Fact]
    public void SignExtendedStatus_AsDto_TextType()
    {
        // Arrange
        var status = new SignExtendedStatus
        {
            SignID = 1,
            SignType = 0, // Text
            DimmingMode = 0, // Automatic
            SignErrorCode = 0
        };

        // Act
        var dto = status.AsDto();

        // Assert
        Assert.Equal("Text", dto.SignTypeDescription);
        Assert.Equal("Automatic", dto.DimmingModeDescription);
    }

    [Fact]
    public void SignExtendedStatus_AsDto_GraphicsType()
    {
        // Arrange
        var status = new SignExtendedStatus
        {
            SignType = 1, // Graphics
            DimmingMode = 1, // Manual
            SignErrorCode = 0
        };

        // Act
        var dto = status.AsDto();

        // Assert
        Assert.Equal("Graphics", dto.SignTypeDescription);
        Assert.Equal("Manual", dto.DimmingModeDescription);
    }

    [Fact]
    public void SignExtendedStatus_AsDto_AdvancedGraphicsType()
    {
        // Arrange
        var status = new SignExtendedStatus
        {
            SignType = 2, // Advanced Graphics
            SignErrorCode = 0
        };

        // Act
        var dto = status.AsDto();

        // Assert
        Assert.Equal("Advanced Graphics", dto.SignTypeDescription);
    }

    [Fact]
    public void SignExtendedStatus_AsDto_UnknownTypes()
    {
        // Arrange
        var status = new SignExtendedStatus
        {
            SignType = 99, // Unknown
            DimmingMode = 99, // Unknown
            SignErrorCode = 0
        };

        // Act
        var dto = status.AsDto();

        // Assert
        Assert.Equal("Unknown", dto.SignTypeDescription);
        Assert.Equal("Unknown", dto.DimmingModeDescription);
    }

    #endregion

    #region SignSetTextFrame Extensions

    [Fact]
    public void SignSetTextFrame_AsDto_MapsAllProperties()
    {
        // Arrange
        var frame = new SignSetTextFrame
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            Text = "48454C4C4F" // "HELLO" in hex
        };

        // Act
        var dto = frame.AsDto();

        // Assert
        Assert.Equal(frame.FrameID, dto.FrameID);
        Assert.Equal(frame.Revision, dto.Revision);
        Assert.Equal(frame.Font, dto.Font);
        Assert.Equal(frame.Colour, dto.Colour);
        Assert.Equal(frame.Conspicuity, dto.Conspicuity);
        Assert.Equal("HELLO", dto.Text);
    }

    [Fact]
    public void SignSetTextFrame_AsDto_InvalidHex_ReturnsEmptyText()
    {
        // Arrange
        var frame = new SignSetTextFrame
        {
            FrameID = 1,
            Text = "INVALID_HEX_ODD" // Invalid hex (odd length)
        };

        // Act
        var dto = frame.AsDto();

        // Assert
        Assert.Equal("", dto.Text);
    }

    [Fact]
    public void SignSetTextFrameDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignSetTextFrameDto
        {
            FrameID = 1,
            Revision = 2,
            Font = 3,
            Colour = 4,
            Conspicuity = 5,
            Text = "HELLO"
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.FrameID, entity.FrameID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.Font, entity.Font);
        Assert.Equal(dto.Colour, entity.Colour);
        Assert.Equal(dto.Conspicuity, entity.Conspicuity);
        Assert.Equal("48454C4C4F", entity.Text); // "HELLO" in hex
        Assert.Equal(5, entity.NumberOfCharsInText);
    }

    [Fact]
    public void SignSetTextFrameDto_AsEntity_EmptyText()
    {
        // Arrange
        var dto = new SignSetTextFrameDto
        {
            FrameID = 1,
            Text = ""
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal("", entity.Text);
        Assert.Equal(0, entity.NumberOfCharsInText);
    }

    #endregion

    #region SignSetMessage Extensions

    [Fact]
    public void SignSetMessage_AsDto_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var dto = message.AsDto();

        // Assert
        Assert.Equal(message.MessageID, dto.MessageID);
        Assert.Equal(message.Revision, dto.Revision);
        Assert.Equal(message.TransitionTimeBetweenFrames, dto.TransitionTimeBetweenFrames);
        Assert.Equal(message.Frame1ID, dto.Frame1ID);
        Assert.Equal(message.Frame1Time, dto.Frame1Time);
        Assert.Equal(message.Frame2ID, dto.Frame2ID);
        Assert.Equal(message.Frame2Time, dto.Frame2Time);
        Assert.Equal(message.Frame3ID, dto.Frame3ID);
        Assert.Equal(message.Frame3Time, dto.Frame3Time);
        Assert.Equal(message.Frame4ID, dto.Frame4ID);
        Assert.Equal(message.Frame4Time, dto.Frame4Time);
        Assert.Equal(message.Frame5ID, dto.Frame5ID);
        Assert.Equal(message.Frame5Time, dto.Frame5Time);
        Assert.Equal(message.Frame6ID, dto.Frame6ID);
        Assert.Equal(message.Frame6Time, dto.Frame6Time);
    }

    [Fact]
    public void SignSetMessageDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignSetMessageDto
        {
            MessageID = 1,
            Revision = 2,
            TransitionTimeBetweenFrames = 10,
            Frame1ID = 11,
            Frame1Time = 100
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.MessageID, entity.MessageID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.TransitionTimeBetweenFrames, entity.TransitionTimeBetweenFrames);
        Assert.Equal(dto.Frame1ID, entity.Frame1ID);
        Assert.Equal(dto.Frame1Time, entity.Frame1Time);
    }

    #endregion

    #region SignDisplayMessage Extensions

    [Fact]
    public void SignDisplayMessage_AsDto_MapsAllProperties()
    {
        // Arrange
        var message = new SignDisplayMessage
        {
            GroupID = 1,
            MessageID = 10
        };

        // Act
        var dto = message.AsDto();

        // Assert
        Assert.Equal(message.GroupID, dto.GroupID);
        Assert.Equal(message.MessageID, dto.MessageID);
    }

    [Fact]
    public void SignDisplayMessageDto_AsEntity_MapsAllProperties()
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
        Assert.Equal(dto.GroupID, entity.GroupID);
        Assert.Equal(dto.MessageID, entity.MessageID);
    }

    #endregion

    #region SignDisplayFrame Extensions

    [Fact]
    public void SignDisplayFrame_AsDto_MapsAllProperties()
    {
        // Arrange
        var frame = new SignDisplayFrame
        {
            SignID = 1,
            FrameID = 10
        };

        // Act
        var dto = frame.AsDto();

        // Assert
        Assert.Equal(frame.SignID, dto.SignID);
        Assert.Equal(frame.FrameID, dto.FrameID);
    }

    [Fact]
    public void SignDisplayFrameDto_AsEntity_MapsAllProperties()
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
        Assert.Equal(dto.SignID, entity.SignID);
        Assert.Equal(dto.FrameID, entity.FrameID);
    }

    #endregion

    #region SignDisplayAtomicFrame Extensions

    [Fact]
    public void SignDisplayAtomicFrame_AsDto_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var dto = atomic.AsDto();

        // Assert
        Assert.Equal(atomic.GroupID, dto.GroupID);
        Assert.Equal(atomic.NumbeOfSigns, dto.NumbeOfSigns);
        Assert.Equal(2, dto.Frames.Count);
    }

    [Fact]
    public void SignDisplayAtomicFrameDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignDisplayAtomicFrameDto
        {
            GroupID = 1,
            NumbeOfSigns = 2,
            Frames = new List<SignDisplayFrameDto>
            {
                new SignDisplayFrameDto { SignID = 1, FrameID = 10 },
                new SignDisplayFrameDto { SignID = 2, FrameID = 20 }
            }
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.GroupID, entity.GroupID);
        Assert.Equal(dto.NumbeOfSigns, entity.NumbeOfSigns);
        Assert.Equal(2, entity.Frames.Count);
    }

    #endregion

    #region AckReply Extensions

    [Fact]
    public void AckReply_AsDto_ReturnsNewDto()
    {
        // Arrange
        var ack = new AckReply();

        // Act
        var dto = ack.AsDto();

        // Assert
        Assert.NotNull(dto);
    }

    [Fact]
    public void AckReplyDto_AsEntity_ReturnsNewEntity()
    {
        // Arrange
        var dto = new AckReplyDto();

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.NotNull(entity);
    }

    #endregion

    #region ExtendedTextFrame Extensions

    [Fact]
    public void ExtendedTextFrameDto_AsSignSetTextFrame_MapsAllProperties()
    {
        // Arrange
        var dto = new ExtendedTextFrameDto
        {
            Font = 1,
            Colour = 2,
            Conspicuity = 3,
            Text = "HELLO"
        };

        // Act
        var entity = dto.AsSignSetTextFrame(10, 1);

        // Assert
        Assert.Equal(10, entity.FrameID);
        Assert.Equal(1, entity.Revision);
        Assert.Equal(dto.Font, entity.Font);
        Assert.Equal(dto.Colour, entity.Colour);
        Assert.Equal(dto.Conspicuity, entity.Conspicuity);
        Assert.Equal("48454C4C4F", entity.Text); // "HELLO" in hex
        Assert.Equal(5, entity.NumberOfCharsInText);
    }

    #endregion

    #region SignSetGraphicsFrame Extensions

    [Fact]
    public void SignSetGraphicsFrame_AsDto_MapsAllProperties()
    {
        // Arrange
        var frame = new SignSetGraphicsFrame
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 10,
            NumberOfColumns = 20,
            Colour = 3,
            Conspicuity = 4,
            GraphicsData = "FF00FF00"
        };

        // Act
        var dto = frame.AsDto();

        // Assert
        Assert.Equal(frame.FrameID, dto.FrameID);
        Assert.Equal(frame.Revision, dto.Revision);
        Assert.Equal(frame.NumberOfRows, dto.NumberOfRows);
        Assert.Equal(frame.NumberOfColumns, dto.NumberOfColumns);
        Assert.Equal(frame.Colour, dto.Colour);
        Assert.Equal(frame.Conspicuity, dto.Conspicuity);
        Assert.Equal(frame.GraphicsData, dto.GraphicsData);
    }

    [Fact]
    public void SignSetGraphicsFrameDto_AsEntity_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.FrameID, entity.FrameID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.NumberOfRows, entity.NumberOfRows);
        Assert.Equal(dto.NumberOfColumns, entity.NumberOfColumns);
        Assert.Equal(dto.Colour, entity.Colour);
        Assert.Equal(dto.Conspicuity, entity.Conspicuity);
        Assert.Equal(dto.GraphicsData, entity.GraphicsData);
        Assert.Equal((ushort)4, entity.GraphicsLength); // 8 hex chars = 4 bytes
    }

    [Fact]
    public void SignSetGraphicsFrameDto_AsEntity_NullGraphicsData()
    {
        // Arrange
        var dto = new SignSetGraphicsFrameDto
        {
            FrameID = 1,
            GraphicsData = null!
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal("", entity.GraphicsData);
        Assert.Equal((ushort)0, entity.GraphicsLength);
    }

    #endregion

    #region SignSetHighResolutionGraphicsFrame Extensions

    [Fact]
    public void SignSetHighResolutionGraphicsFrame_AsDto_MapsAllProperties()
    {
        // Arrange
        var frame = new SignSetHighResolutionGraphicsFrame
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 1000,
            NumberOfColumns = 2000,
            Colour = 3,
            Conspicuity = 4,
            GraphicsData = "FF00FF00FF00FF00"
        };

        // Act
        var dto = frame.AsDto();

        // Assert
        Assert.Equal(frame.FrameID, dto.FrameID);
        Assert.Equal(frame.Revision, dto.Revision);
        Assert.Equal(frame.NumberOfRows, dto.NumberOfRows);
        Assert.Equal(frame.NumberOfColumns, dto.NumberOfColumns);
        Assert.Equal(frame.Colour, dto.Colour);
        Assert.Equal(frame.Conspicuity, dto.Conspicuity);
        Assert.Equal(frame.GraphicsData, dto.GraphicsData);
    }

    [Fact]
    public void SignSetHighResolutionGraphicsFrameDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignSetHighResolutionGraphicsFrameDto
        {
            FrameID = 1,
            Revision = 2,
            NumberOfRows = 1000,
            NumberOfColumns = 2000,
            Colour = 3,
            Conspicuity = 4,
            GraphicsData = "FF00FF00FF00FF00"
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.FrameID, entity.FrameID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.NumberOfRows, entity.NumberOfRows);
        Assert.Equal(dto.NumberOfColumns, entity.NumberOfColumns);
        Assert.Equal(dto.Colour, entity.Colour);
        Assert.Equal(dto.Conspicuity, entity.Conspicuity);
        Assert.Equal(dto.GraphicsData, entity.GraphicsData);
        Assert.Equal((uint)8, entity.GraphicsLength); // 16 hex chars = 8 bytes
    }

    [Fact]
    public void SignSetHighResolutionGraphicsFrameDto_AsEntity_NullGraphicsData()
    {
        // Arrange
        var dto = new SignSetHighResolutionGraphicsFrameDto
        {
            FrameID = 1,
            GraphicsData = null!
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal("", entity.GraphicsData);
        Assert.Equal((uint)0, entity.GraphicsLength);
    }

    #endregion

    #region SignSetPlan Extensions

    [Fact]
    public void SignSetPlan_AsDto_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var dto = plan.AsDto();

        // Assert
        Assert.Equal(plan.PlanID, dto.PlanID);
        Assert.Equal(plan.Revision, dto.Revision);
        Assert.Equal(plan.DayOfWeek, dto.DayOfWeek);
        Assert.Single(dto.Entries);
        Assert.Equal(10, dto.Entries[0].FrameMessageID);
    }

    [Fact]
    public void SignSetPlanDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignSetPlanDto
        {
            PlanID = 1,
            Revision = 2,
            DayOfWeek = 0x7F,
            Entries = new List<SignSetPlanEntryDto>
            {
                new SignSetPlanEntryDto
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

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.PlanID, entity.PlanID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.DayOfWeek, entity.DayOfWeek);
        Assert.Single(entity.Entries);
    }

    [Fact]
    public void SignSetPlanEntry_AsDto_MapsAllProperties()
    {
        // Arrange
        var entry = new SignSetPlanEntry
        {
            FrameMessageType = 2,
            FrameMessageID = 15,
            StartHour = 9,
            StartMinute = 30,
            StopHour = 18,
            StopMinute = 0
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal(entry.FrameMessageType, dto.FrameMessageType);
        Assert.Equal(entry.FrameMessageID, dto.FrameMessageID);
        Assert.Equal(entry.StartHour, dto.StartHour);
        Assert.Equal(entry.StartMinute, dto.StartMinute);
        Assert.Equal(entry.StopHour, dto.StopHour);
        Assert.Equal(entry.StopMinute, dto.StopMinute);
    }

    [Fact]
    public void SignSetPlanEntryDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new SignSetPlanEntryDto
        {
            FrameMessageType = 2,
            FrameMessageID = 15,
            StartHour = 9,
            StartMinute = 30,
            StopHour = 18,
            StopMinute = 0
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.FrameMessageType, entity.FrameMessageType);
        Assert.Equal(dto.FrameMessageID, entity.FrameMessageID);
        Assert.Equal(dto.StartHour, entity.StartHour);
        Assert.Equal(dto.StartMinute, entity.StartMinute);
        Assert.Equal(dto.StopHour, entity.StopHour);
        Assert.Equal(dto.StopMinute, entity.StopMinute);
    }

    #endregion

    #region ReportEnabledPlans Extensions

    [Fact]
    public void ReportEnabledPlans_AsDto_MapsAllProperties()
    {
        // Arrange
        var report = new ReportEnabledPlans
        {
            Entries = new List<EnabledPlanEntry>
            {
                new EnabledPlanEntry { GroupID = 1, PlanID = 10 },
                new EnabledPlanEntry { GroupID = 2, PlanID = 20 }
            }
        };

        // Act
        var dto = report.AsDto();

        // Assert
        Assert.Equal(2, dto.Entries.Count);
        Assert.Equal(1, dto.Entries[0].GroupID);
        Assert.Equal(10, dto.Entries[0].PlanID);
    }

    [Fact]
    public void EnabledPlanEntry_AsDto_MapsAllProperties()
    {
        // Arrange
        var entry = new EnabledPlanEntry
        {
            GroupID = 1,
            PlanID = 10
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal(entry.GroupID, dto.GroupID);
        Assert.Equal(entry.PlanID, dto.PlanID);
    }

    #endregion

    #region HAR Extensions

    [Fact]
    public void HARStatusReply_AsDto_MapsAllProperties()
    {
        // Arrange
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
            StrategyStatus = 1 // Playing
        };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal(reply.OnlineStatus, dto.OnlineStatus);
        Assert.Equal(reply.HAREnabled, dto.HAREnabled);
        Assert.Equal(reply.VoiceIDPlaying, dto.VoiceIDPlaying);
        Assert.Equal(reply.StrategyIDActive, dto.StrategyIDActive);
        Assert.Equal("Strategy is playing", dto.StrategyStatusDescription);
    }

    [Fact]
    public void HARStatusReply_AsDto_StrategyPreparing()
    {
        // Arrange
        var reply = new HARStatusReply { Day = 1, Month = 1, Year = 2024, StrategyStatus = 2 };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal("Strategy is preparing to play", dto.StrategyStatusDescription);
    }

    [Fact]
    public void HARStatusReply_AsDto_StrategyNotPlaying()
    {
        // Arrange
        var reply = new HARStatusReply { Day = 1, Month = 1, Year = 2024, StrategyStatus = 3 };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal("Strategy is not playing", dto.StrategyStatusDescription);
    }

    [Fact]
    public void HARStatusReply_AsDto_UnknownStatus()
    {
        // Arrange
        var reply = new HARStatusReply { Day = 1, Month = 1, Year = 2024, StrategyStatus = 99 };

        // Act
        var dto = reply.AsDto();

        // Assert
        Assert.Equal("Unknown", dto.StrategyStatusDescription);
    }

    [Fact]
    public void HARSetStrategy_AsDto_MapsAllProperties()
    {
        // Arrange
        var strategy = new HARSetStrategy
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3 }
        };

        // Act
        var dto = strategy.AsDto();

        // Assert
        Assert.Equal(strategy.StrategyID, dto.StrategyID);
        Assert.Equal(strategy.Revision, dto.Revision);
        Assert.Equal(3, dto.VoiceIDs.Count);
    }

    [Fact]
    public void HARSetStrategyCommandDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new HARSetStrategyCommandDto
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3 }
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.StrategyID, entity.StrategyID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(3, entity.VoiceIDs.Count);
    }

    [Fact]
    public void HARSetPlan_AsDto_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var dto = plan.AsDto();

        // Assert
        Assert.Equal(plan.PlanID, dto.PlanID);
        Assert.Equal(plan.Revision, dto.Revision);
        Assert.Equal(plan.DayOfWeek, dto.DayOfWeek);
        Assert.Single(dto.Entries);
    }

    [Fact]
    public void HARSetPlanCommandDto_AsEntity_MapsAllProperties()
    {
        // Arrange
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

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.PlanID, entity.PlanID);
        Assert.Equal(dto.Revision, entity.Revision);
        Assert.Equal(dto.DayOfWeek, entity.DayOfWeek);
        Assert.Single(entity.Entries);
    }

    [Fact]
    public void HARSetPlanEntry_AsDto_MapsAllProperties()
    {
        // Arrange
        var entry = new HARSetPlanEntry
        {
            StrategyID = 100,
            StartHour = 8,
            StartMinute = 30,
            StopHour = 17,
            StopMinute = 45
        };

        // Act
        var dto = entry.AsDto();

        // Assert
        Assert.Equal(entry.StrategyID, dto.StrategyID);
        Assert.Equal(entry.StartHour, dto.StartHour);
        Assert.Equal(entry.StartMinute, dto.StartMinute);
        Assert.Equal(entry.StopHour, dto.StopHour);
        Assert.Equal(entry.StopMinute, dto.StopMinute);
    }

    [Fact]
    public void HARSetPlanEntryDto_AsEntity_MapsAllProperties()
    {
        // Arrange
        var dto = new HARSetPlanEntryDto
        {
            StrategyID = 100,
            StartHour = 8,
            StartMinute = 30,
            StopHour = 17,
            StopMinute = 45
        };

        // Act
        var entity = dto.AsEntity();

        // Assert
        Assert.Equal(dto.StrategyID, entity.StrategyID);
        Assert.Equal(dto.StartHour, entity.StartHour);
        Assert.Equal(dto.StartMinute, entity.StartMinute);
        Assert.Equal(dto.StopHour, entity.StopHour);
        Assert.Equal(dto.StopMinute, entity.StopMinute);
    }

    #endregion
}
