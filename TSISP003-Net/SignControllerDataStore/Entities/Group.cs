namespace TSISP003_Net.SignControllerDataStore.Entities;

public class Group
{
    public byte GroupID { get; set; }
    public List<Sign> Signs { get; set; } = [];
}