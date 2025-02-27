namespace TSISP003_Net.SignControllerDataStore.Entities;

public class FaultLogEntry
{
    /// <summary>
    /// *ID – depends on controller type.
    /// </summary>
    public byte Id { get; set; }

    /// <summary>
    /// *entry number field – cycles between 0 and 255.
    /// </summary>
    public byte EntryNumber { get; set; }

    /// <summary>
    /// Day of month (1-31).
    /// </summary>
    public byte Day { get; set; }

    /// <summary>
    /// Month (1-12).
    /// </summary>
    public byte Month { get; set; }

    /// <summary>
    /// Year (1-9999). 
    /// </summary>
    public short Year { get; set; }

    /// <summary>
    /// Hour (0-23).
    /// </summary>
    public byte Hour { get; set; }

    /// <summary>
    /// Minute (1-59).
    /// </summary>
    public byte Minute { get; set; }

    /// <summary>
    /// Second (1-59).
    /// </summary>
    public byte Second { get; set; }

    /// <summary>
    /// Error code (see appendix C).
    /// </summary>
    public byte ErrorCode { get; set; }

    /// <summary>
    /// Fault clearance/fault onset (0/1).
    /// If 0 = false, 1 = true.
    /// </summary>
    public bool IsFaultCleared { get; set; }
}
