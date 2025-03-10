using TSISP003.SignControllerDataStore.Entities;

namespace TSISP003_Net.SignControllerDataStore.Entities;

public class GraphicFrame : Frame
{
    public ushort NumberOfRows { get; set; }
    public ushort NumberOfColumns { get; set; }
    public string Graphic { get; set; } = string.Empty;
}