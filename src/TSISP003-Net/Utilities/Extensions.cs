using TSISP003.DTOs;
using TSISP003.Utilities.Constants;
using System.Text;
using TSISP003.Domain.Entities;

namespace TSISP003.Utilities;

public static class Extensions
{


    /// <summary>
    /// Convert a Sign to a SignDto
    /// </summary>
    /// <param name="sign"></param>
    /// <returns></returns>
    public static SignDto AsDto(this Sign sign)
    {
        return new SignDto
        {
            SignID = sign.SignID,
            SignErrorCode = sign.SignErrorCode,
            SignEnabled = sign.SignEnabled,
            FrameID = sign.FrameID,
            FrameRevision = sign.FrameRevision,
            MessageID = sign.MessageID,
            MessageRevision = sign.MessageRevision,
            PlanID = sign.PlanID,
            PlanRevision = sign.PlanRevision,
            SignType = sign.SignType.ToString(),
            SignWidth = sign.SignWidth,
            SignHeight = sign.SignHeight
        };
    }

    /// <summary>
    /// Convert a SignGroup to a SignGroupDto
    /// </summary>
    /// <param name="signGroup"></param>
    /// <returns></returns>
    public static SignGroupDto AsDto(this SignGroup signGroup)
    {
        return new SignGroupDto
        {
            GroupId = signGroup.GroupID,
            Signature = signGroup.Signature,
            Signs = signGroup.Signs?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.AsDto()
            ) ?? new()
        };
    }

    /// <summary>
    /// Convert a SignController to a SignControllerDto
    /// </summary>
    /// <param name="controller"></param>
    /// <returns></returns>
    public static SignControllerDto AsDto(this SignController controller)
    {
        return new SignControllerDto
        {
            OnlineStatus = controller.OnlineStatus,
            DateChange = controller.DateChange,
            ControllerChecksum = controller.ControllerChecksum,
            ControllerErrorCode = controller.ControllerErrorCode,
            NumberOfGroups = controller.NumberOfGroups,
            Groups = controller.Groups.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.AsDto()
            )
        };
    }

    /// <summary>
    /// Convert a FaultLogEntry to a FaultLogEntryDto
    /// </summary>
    /// <param name="faultLogEntry"></param>
    /// <returns></returns>
    public static FaultLogEntryDto AsDto(this FaultLogEntry faultLogEntry)
    {
        // Attempt to parse the DateTime from the fault log entry
        DateTime entryDateTime;
        try
        {
            entryDateTime = new DateTime(
                faultLogEntry.Year,
                faultLogEntry.Month,
                faultLogEntry.Day,
                faultLogEntry.Hour,
                faultLogEntry.Minute,
                faultLogEntry.Second);
        }
        catch
        {
            // If the DateTime cannot be parsed, set it to a default value
            entryDateTime = new DateTime(1900, 1, 1);
        }

        // Return the FaultLogEntryDto with the parsed DateTime
        return new FaultLogEntryDto
        {
            Id = faultLogEntry.Id,
            EntryNumber = faultLogEntry.EntryNumber,
            ErrorCode = faultLogEntry.ErrorCode,
            ErrorDescription = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(faultLogEntry.ErrorCode, "Unknown error code"),
            IsFaultCleared = faultLogEntry.IsFaultCleared,
            EntryDateTime = entryDateTime // New property holding the parsed DateTime
        };
    }


    public static RejectReplyDto AsDto(this RejectReply rejectReply)
    {
        return new RejectReplyDto
        {
            ApplicationErrorCode = rejectReply.ApplicationErrorCode,
            ApplicationErrorDescription = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault(rejectReply.ApplicationErrorCode, "Unknown error code")
        };
    }

    public static SignStatusDto AsDto(this SignStatus signStatus)
    {
        return new SignStatusDto
        {
            SignID = signStatus.SignID,
            SignErrorCode = signStatus.SignErrorCode,
            SignError = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(signStatus.SignErrorCode, "Unknown error code"),
            SignEnabled = signStatus.SignEnabled,
            FrameID = signStatus.FrameID,
            FrameRevision = signStatus.FrameRevision,
            MessageID = signStatus.MessageID,
            MessageRevision = signStatus.MessageRevision,
            PlanID = signStatus.PlanID,
            PlanRevision = signStatus.PlanRevision
        };
    }

    public static SignStatusReplyDto AsDto(this SignStatusReply signStatusReply)
    {
        // Attempt to parse the DateTime from the fault log entry
        DateTime dateTime;
        try
        {
            dateTime = new DateTime(
                signStatusReply.Year,
                signStatusReply.Month,
                signStatusReply.Day,
                signStatusReply.Hour,
                signStatusReply.Minute,
                signStatusReply.Second);
        }
        catch
        {
            // If the DateTime cannot be parsed, set it to a default value
            dateTime = new DateTime(1900, 1, 1);
        }

        return new SignStatusReplyDto
        {
            OnlineStatus = signStatusReply.OnlineStatus,
            ApplicationErrorCode = signStatusReply.ApplicationErrorCode,
            dateTime = dateTime,
            ControllerChecksum = signStatusReply.ControllerChecksum,
            ControllerErrorCode = signStatusReply.ControllerErrorCode,
            ControllerError = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(signStatusReply.ControllerErrorCode, "Unknown error code"),
            NumberOfSigns = signStatusReply.NumberOfSigns,
            Signs = signStatusReply.Signs.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.AsDto()
            )
        };
    }

    public static SignExtendedStatusReplyDto AsDto(this SignExtendedStatusReply signExtendedStatusReply)
    {
        // Attempt to parse the DateTime
        DateTime dateTime;
        try
        {
            dateTime = new DateTime(
                signExtendedStatusReply.Year,
                signExtendedStatusReply.Month,
                signExtendedStatusReply.Day,
                signExtendedStatusReply.Hour,
                signExtendedStatusReply.Minute,
                signExtendedStatusReply.Second);
        }
        catch
        {
            dateTime = new DateTime(1900, 1, 1);
        }

        return new SignExtendedStatusReplyDto
        {
            OnlineStatus = signExtendedStatusReply.OnlineStatus,
            ApplicationErrorCode = signExtendedStatusReply.ApplicationErrorCode,
            ApplicationError = ErrorCodes.ApplicationErrorCodes.GetValueOrDefault(signExtendedStatusReply.ApplicationErrorCode, "Unknown error code"),
            ManufacturerCode = signExtendedStatusReply.ManufacturerCode,
            DateTime = dateTime,
            ControllerErrorCode = signExtendedStatusReply.ControllerErrorCode,
            ControllerError = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(signExtendedStatusReply.ControllerErrorCode, "Unknown error code"),
            NumberOfSigns = signExtendedStatusReply.NumberOfSigns,
            Signs = signExtendedStatusReply.Signs.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.AsDto()
            )
        };
    }

    public static SignExtendedStatusDto AsDto(this SignExtendedStatus signExtendedStatus)
    {
        string signTypeDescription = signExtendedStatus.SignType switch
        {
            0 => "Text",
            1 => "Graphics",
            2 => "Advanced Graphics",
            _ => "Unknown"
        };

        string dimmingModeDescription = signExtendedStatus.DimmingMode switch
        {
            0 => "Automatic",
            1 => "Manual",
            _ => "Unknown"
        };

        return new SignExtendedStatusDto
        {
            SignID = signExtendedStatus.SignID,
            SignType = signExtendedStatus.SignType,
            SignTypeDescription = signTypeDescription,
            NumberOfRows = signExtendedStatus.NumberOfRows,
            NumberOfColumns = signExtendedStatus.NumberOfColumns,
            SignErrorCode = signExtendedStatus.SignErrorCode,
            SignError = ErrorCodes.ControllerDeviceErrorCodes.GetValueOrDefault(signExtendedStatus.SignErrorCode, "Unknown error code"),
            DimmingMode = signExtendedStatus.DimmingMode,
            DimmingModeDescription = dimmingModeDescription,
            LuminanceLevel = signExtendedStatus.LuminanceLevel,
            LampLedStatus = signExtendedStatus.LampLedStatus
        };
    }

    public static SignSetTextFrameDto AsDto(this SignSetTextFrame signSetTextFrame)
    {
        string asciiText = "";
        try
        {
            asciiText = Functions.HexToAscii(signSetTextFrame.Text);
        }
        catch (System.Exception)
        {

        }
        return new SignSetTextFrameDto
        {
            FrameID = signSetTextFrame.FrameID,
            Revision = signSetTextFrame.Revision,
            Font = signSetTextFrame.Font,
            Colour = signSetTextFrame.Colour,
            Conspicuity = signSetTextFrame.Conspicuity,
            Text = asciiText,
        };
    }

    public static SignSetMessageDto AsDto(this SignSetMessage signSetMessage)
    {
        return new SignSetMessageDto
        {
            MessageID = signSetMessage.MessageID,
            Revision = signSetMessage.Revision,
            TransitionTimeBetweenFrames = signSetMessage.TransitionTimeBetweenFrames,
            Frame1ID = signSetMessage.Frame1ID,
            Frame1Time = signSetMessage.Frame1Time,
            Frame2ID = signSetMessage.Frame2ID,
            Frame2Time = signSetMessage.Frame2Time,
            Frame3ID = signSetMessage.Frame3ID,
            Frame3Time = signSetMessage.Frame3Time,
            Frame4ID = signSetMessage.Frame4ID,
            Frame4Time = signSetMessage.Frame4Time,
            Frame5ID = signSetMessage.Frame5ID,
            Frame5Time = signSetMessage.Frame5Time,
            Frame6ID = signSetMessage.Frame6ID,
            Frame6Time = signSetMessage.Frame6Time,
        };
    }

    public static SignSetMessage AsEntity(this SignSetMessageDto signSetMessageDto)
    {
        return new SignSetMessage
        {
            MessageID = signSetMessageDto.MessageID,
            Revision = signSetMessageDto.Revision,
            TransitionTimeBetweenFrames = signSetMessageDto.TransitionTimeBetweenFrames,
            Frame1ID = signSetMessageDto.Frame1ID,
            Frame1Time = signSetMessageDto.Frame1Time,
            Frame2ID = signSetMessageDto.Frame2ID,
            Frame2Time = signSetMessageDto.Frame2Time,
            Frame3ID = signSetMessageDto.Frame3ID,
            Frame3Time = signSetMessageDto.Frame3Time,
            Frame4ID = signSetMessageDto.Frame4ID,
            Frame4Time = signSetMessageDto.Frame4Time,
            Frame5ID = signSetMessageDto.Frame5ID,
            Frame5Time = signSetMessageDto.Frame5Time,
            Frame6ID = signSetMessageDto.Frame6ID,
            Frame6Time = signSetMessageDto.Frame6Time,
        };
    }

    public static SignSetTextFrame AsEntity(this SignSetTextFrameDto signSetTextFrameDto)
    {
        string hexText = "";
        try
        {
            hexText = Functions.AsciiToHex(signSetTextFrameDto.Text);
        }
        catch (Exception)
        {

        }

        return new SignSetTextFrame
        {
            FrameID = signSetTextFrameDto.FrameID,
            Revision = signSetTextFrameDto.Revision,
            Font = signSetTextFrameDto.Font,
            Colour = signSetTextFrameDto.Colour,
            Conspicuity = signSetTextFrameDto.Conspicuity,
            Text = hexText,
            NumberOfCharsInText = (byte)(hexText.Length / 2),
            CRC = Functions.PacketCRCushort(Encoding.ASCII.GetBytes(hexText))
        };
    }

    public static SignDisplayMessageDto AsDto(this SignDisplayMessage signDisplayMessage)
    {
        return new SignDisplayMessageDto
        {
            GroupID = signDisplayMessage.GroupID,
            MessageID = signDisplayMessage.MessageID
        };
    }

    public static SignDisplayMessage AsEntity(this SignDisplayMessageDto signDisplayMessageDto)
    {
        return new SignDisplayMessage
        {
            GroupID = signDisplayMessageDto.GroupID,
            MessageID = signDisplayMessageDto.MessageID
        };
    }

    public static SignDisplayAtomicFrameDto AsDto(this SignDisplayAtomicFrame signDisplayAtomicFrame)
    {
        return new SignDisplayAtomicFrameDto
        {
            GroupID = signDisplayAtomicFrame.GroupID,
            NumbeOfSigns = signDisplayAtomicFrame.NumbeOfSigns,
            Frames = signDisplayAtomicFrame.Frames.Select(frame => frame.AsDto()).ToList()
        };
    }

    public static SignDisplayAtomicFrame AsEntity(this SignDisplayAtomicFrameDto signDisplayAtomicFrameDto)
    {
        return new SignDisplayAtomicFrame
        {
            GroupID = signDisplayAtomicFrameDto.GroupID,
            NumbeOfSigns = signDisplayAtomicFrameDto.NumbeOfSigns,
            Frames = signDisplayAtomicFrameDto.Frames.Select(frame => frame.AsEntity()).ToList()
        };
    }

    public static SignDisplayFrame AsEntity(this SignDisplayFrameDto signDisplayFrameDto)
    {
        return new SignDisplayFrame
        {
            SignID = signDisplayFrameDto.SignID,
            FrameID = signDisplayFrameDto.FrameID
        };
    }

    public static SignDisplayFrameDto AsDto(this SignDisplayFrame signDisplayFrame)
    {
        return new SignDisplayFrameDto
        {
            SignID = signDisplayFrame.SignID,
            FrameID = signDisplayFrame.FrameID
        };
    }

    public static AckReplyDto AsDto(this AckReply ackReply)
    {
        return new AckReplyDto();
    }

    public static AckReply AsEntity(this AckReplyDto ackReplyDto)
    {
        return new AckReply();
    }


    public static SignSetTextFrame AsSignSetTextFrame(this ExtendedTextFrameDto extendedTextFrameDto, byte frameid, byte revision)
    {
        string hexText = Functions.AsciiToHex(extendedTextFrameDto.Text);
        return new SignSetTextFrame
        {
            FrameID = frameid,
            Revision = revision,
            Font = extendedTextFrameDto.Font,
            Colour = extendedTextFrameDto.Colour,
            Conspicuity = extendedTextFrameDto.Conspicuity,
            Text = hexText,
            NumberOfCharsInText = (byte)(extendedTextFrameDto.Text.Length)
        };
    }

    public static SignSetGraphicsFrameDto AsDto(this SignSetGraphicsFrame signSetGraphicsFrame)
    {
        return new SignSetGraphicsFrameDto
        {
            FrameID = signSetGraphicsFrame.FrameID,
            Revision = signSetGraphicsFrame.Revision,
            NumberOfRows = signSetGraphicsFrame.NumberOfRows,
            NumberOfColumns = signSetGraphicsFrame.NumberOfColumns,
            Colour = signSetGraphicsFrame.Colour,
            Conspicuity = signSetGraphicsFrame.Conspicuity,
            GraphicsData = signSetGraphicsFrame.GraphicsData
        };
    }

    public static SignSetGraphicsFrame AsEntity(this SignSetGraphicsFrameDto signSetGraphicsFrameDto)
    {
        // Graphics data should already be in hex format
        string graphicsData = signSetGraphicsFrameDto.GraphicsData ?? string.Empty;
        ushort graphicsLength = (ushort)(graphicsData.Length / 2);

        return new SignSetGraphicsFrame
        {
            FrameID = signSetGraphicsFrameDto.FrameID,
            Revision = signSetGraphicsFrameDto.Revision,
            NumberOfRows = signSetGraphicsFrameDto.NumberOfRows,
            NumberOfColumns = signSetGraphicsFrameDto.NumberOfColumns,
            Colour = signSetGraphicsFrameDto.Colour,
            Conspicuity = signSetGraphicsFrameDto.Conspicuity,
            GraphicsLength = graphicsLength,
            GraphicsData = graphicsData
        };
    }

    public static SignSetHighResolutionGraphicsFrameDto AsDto(this SignSetHighResolutionGraphicsFrame signSetHighResolutionGraphicsFrame)
    {
        return new SignSetHighResolutionGraphicsFrameDto
        {
            FrameID = signSetHighResolutionGraphicsFrame.FrameID,
            Revision = signSetHighResolutionGraphicsFrame.Revision,
            NumberOfRows = signSetHighResolutionGraphicsFrame.NumberOfRows,
            NumberOfColumns = signSetHighResolutionGraphicsFrame.NumberOfColumns,
            Colour = signSetHighResolutionGraphicsFrame.Colour,
            Conspicuity = signSetHighResolutionGraphicsFrame.Conspicuity,
            GraphicsData = signSetHighResolutionGraphicsFrame.GraphicsData
        };
    }

    public static SignSetHighResolutionGraphicsFrame AsEntity(this SignSetHighResolutionGraphicsFrameDto signSetHighResolutionGraphicsFrameDto)
    {
        // Graphics data should already be in hex format
        string graphicsData = signSetHighResolutionGraphicsFrameDto.GraphicsData ?? string.Empty;
        uint graphicsLength = (uint)(graphicsData.Length / 2);

        return new SignSetHighResolutionGraphicsFrame
        {
            FrameID = signSetHighResolutionGraphicsFrameDto.FrameID,
            Revision = signSetHighResolutionGraphicsFrameDto.Revision,
            NumberOfRows = signSetHighResolutionGraphicsFrameDto.NumberOfRows,
            NumberOfColumns = signSetHighResolutionGraphicsFrameDto.NumberOfColumns,
            Colour = signSetHighResolutionGraphicsFrameDto.Colour,
            Conspicuity = signSetHighResolutionGraphicsFrameDto.Conspicuity,
            GraphicsLength = graphicsLength,
            GraphicsData = graphicsData
        };
    }

    public static SignSetPlanDto AsDto(this SignSetPlan signSetPlan)
    {
        return new SignSetPlanDto
        {
            PlanID = signSetPlan.PlanID,
            Revision = signSetPlan.Revision,
            DayOfWeek = signSetPlan.DayOfWeek,
            Entries = signSetPlan.Entries.Select(e => e.AsDto()).ToList()
        };
    }

    public static SignSetPlanEntryDto AsDto(this SignSetPlanEntry signSetPlanEntry)
    {
        return new SignSetPlanEntryDto
        {
            FrameMessageType = signSetPlanEntry.FrameMessageType,
            FrameMessageID = signSetPlanEntry.FrameMessageID,
            StartHour = signSetPlanEntry.StartHour,
            StartMinute = signSetPlanEntry.StartMinute,
            StopHour = signSetPlanEntry.StopHour,
            StopMinute = signSetPlanEntry.StopMinute
        };
    }

    public static SignSetPlan AsEntity(this SignSetPlanDto signSetPlanDto)
    {
        return new SignSetPlan
        {
            PlanID = signSetPlanDto.PlanID,
            Revision = signSetPlanDto.Revision,
            DayOfWeek = signSetPlanDto.DayOfWeek,
            Entries = signSetPlanDto.Entries.Select(e => e.AsEntity()).ToList()
        };
    }

    public static SignSetPlanEntry AsEntity(this SignSetPlanEntryDto signSetPlanEntryDto)
    {
        return new SignSetPlanEntry
        {
            FrameMessageType = signSetPlanEntryDto.FrameMessageType,
            FrameMessageID = signSetPlanEntryDto.FrameMessageID,
            StartHour = signSetPlanEntryDto.StartHour,
            StartMinute = signSetPlanEntryDto.StartMinute,
            StopHour = signSetPlanEntryDto.StopHour,
            StopMinute = signSetPlanEntryDto.StopMinute
        };
    }

    public static ReportEnabledPlansDto AsDto(this ReportEnabledPlans reportEnabledPlans)
    {
        return new ReportEnabledPlansDto
        {
            Entries = reportEnabledPlans.Entries.Select(e => e.AsDto()).ToList()
        };
    }

    public static EnabledPlanEntryDto AsDto(this EnabledPlanEntry enabledPlanEntry)
    {
        return new EnabledPlanEntryDto
        {
            GroupID = enabledPlanEntry.GroupID,
            PlanID = enabledPlanEntry.PlanID
        };
    }

}



