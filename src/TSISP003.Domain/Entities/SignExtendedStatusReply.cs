using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class SignExtendedStatusReply : ISignResponse
{
    public bool OnlineStatus { get; set; }
    public byte ApplicationErrorCode { get; set; }
    public string ManufacturerCode { get; set; } = string.Empty;
    public byte Day { get; set; }
    public byte Month { get; set; }
    public ushort Year { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
    public byte Second { get; set; }
    public byte ControllerErrorCode { get; set; }
    public byte NumberOfSigns { get; set; }
    public Dictionary<byte, SignExtendedStatus> Signs { get; set; } = new();
    public ushort CRC { get; set; }
}

public class SignExtendedStatus
{
    public byte SignID { get; set; }
    public byte SignType { get; set; }
    public byte NumberOfRows { get; set; }
    public byte NumberOfColumns { get; set; }
    public byte SignErrorCode { get; set; }
    public byte DimmingMode { get; set; }
    public byte LuminanceLevel { get; set; }
    public byte LampLedStatusLength { get; set; }
    public string LampLedStatus { get; set; } = string.Empty;
}
