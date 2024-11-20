using TSISP003_Net.SignControllerDataStore.Entities;

namespace TSISP003_Net;

public static class DTOs
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
}

public class SignDto
{
    public byte SignID { get; set; }
    public byte SignErrorCode { get; set; }
    public bool SignEnabled { get; set; }
    public byte FrameID { get; set; }
    public byte FrameRevision { get; set; }
    public byte MessageID { get; set; }
    public byte MessageRevision { get; set; }
    public byte PlanID { get; set; }
    public byte PlanRevision { get; set; }
    public required string SignType { get; set; }
    public short SignWidth { get; set; }
    public short SignHeight { get; set; }
}

public class SignGroupDto
{
    public byte GroupId { get; set; }
    public Dictionary<byte, SignDto> Signs { get; set; } = new();
    public string? Signature { get; set; }
}

public class SignControllerDto
{
    public bool OnlineStatus { get; set; }
    public DateTime DateChange { get; set; }
    public ushort ControllerChecksum { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfGroups { get; set; }
    public Dictionary<byte, SignGroupDto> Groups { get; set; } = [];
}