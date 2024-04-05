using static TSISP003.SignControllerService.SignControllerServiceConfig;

namespace TSISP003_Net.SignControllerDataStore.Entities;

public class Sign
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
    public SignType SignType { get; set; }
    public short SignWidth { get; set; }
    public short SignHeight { get; set; }
}