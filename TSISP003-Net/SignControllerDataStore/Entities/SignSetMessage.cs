namespace TSISP003_Net.SignControllerDataStore.Entities;

public class SignSetMessage : ISignResponse
{
    public byte MessageID { get; set; }
    public byte Revision { get; set; }
    public byte TransitionTimeBetweenFrames { get; set; }
    public byte Frame1ID { get; set; }
    public byte Frame1Time { get; set; }
    public byte Frame2ID { get; set; }
    public byte Frame2Time { get; set; }
    public byte Frame3ID { get; set; }
    public byte Frame3Time { get; set; }
    public byte Frame4ID { get; set; }
    public byte Frame4Time { get; set; }
    public byte Frame5ID { get; set; }
    public byte Frame5Time { get; set; }
    public byte Frame6ID { get; set; }
    public byte Frame6Time { get; set; }
}