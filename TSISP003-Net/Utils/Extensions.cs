using TSISP003.Utils;
using TSISP003_Net.SignControllerDataStore.Entities;

namespace TSISP003_Net.Utils;

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

}