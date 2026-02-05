namespace TSISP003.Domain.Entities;

/// <summary>
/// Represents a graphics frame to be stored in the sign controller's memory.
/// Used for signs with dimensions up to 255 x 255 pixels.
/// </summary>
public class SignSetGraphicsFrame : ISignResponse
{
    /// <summary>
    /// Frame ID - identifies the frame as it is stored in the sign controller's memory (1-255)
    /// </summary>
    public byte FrameID { get; set; }

    /// <summary>
    /// Revision - identifies the modification level of the frame
    /// </summary>
    public byte Revision { get; set; }

    /// <summary>
    /// Number of rows of pixels in sign (1-255)
    /// </summary>
    public byte NumberOfRows { get; set; }

    /// <summary>
    /// Number of columns of pixels in sign (1-255)
    /// </summary>
    public byte NumberOfColumns { get; set; }

    /// <summary>
    /// Colour code (0=Default, 1=Red, 2=Yellow, 3=Green, 4=Cyan, 5=Blue, 6=Magenta, 7=White, 8=Orange, 9=Amber, 0x0D=Multiple colours)
    /// </summary>
    public byte Colour { get; set; }

    /// <summary>
    /// Conspicuity devices configuration byte
    /// </summary>
    public byte Conspicuity { get; set; }

    /// <summary>
    /// Length of the graphics frame data in bytes
    /// </summary>
    public ushort GraphicsLength { get; set; }

    /// <summary>
    /// Graphics frame data as hex string
    /// </summary>
    public string GraphicsData { get; set; } = string.Empty;

    /// <summary>
    /// Message CRC - calculated for all the bytes in the application message
    /// </summary>
    public ushort CRC { get; set; }
}
