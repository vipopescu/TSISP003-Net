

namespace TSISP003.Domain.Entities;

public class TextFrame : Frame
{
    public byte Font { get; set; }
    public string Text { get; set; } = string.Empty;
}
