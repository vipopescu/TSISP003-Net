using TSISP003.Application.DTOs;

namespace TSISP003.Application.Interfaces;

/// <summary>
/// "Extended" operations layered on top of the TSI-SP-003 protocol — management
/// conveniences that are NOT part of the protocol itself, but orchestrate several
/// protocol operations together.
/// </summary>
public interface IExtendedSignService
{
    /// <summary>
    /// Builds text frames from the request under rolling IDs, assembles a message from them,
    /// and displays that message on group 1 of the device.
    /// </summary>
    Task BuildAndDisplayMessageAsync(string device, ExtendedRequestMessageDto request);

    /// <summary>
    /// Returns the device's sign status enriched with the resolved content (frames and decoded
    /// text) of each sign's active message/frame, fetched live from the controller via
    /// Request-Stored. Returns null if no status is available.
    /// </summary>
    Task<ExtendedStatusReplyDto?> GetExtendedStatusAsync(string device);
}
