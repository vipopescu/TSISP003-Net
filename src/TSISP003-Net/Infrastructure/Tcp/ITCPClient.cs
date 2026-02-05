namespace TSISP003.Infrastructure.Tcp;

/// <summary>
/// Interface for TCP client operations to enable mocking in tests.
/// </summary>
public interface ITCPClient : IDisposable
{
    /// <summary>
    /// Sends the specified message over TCP.
    /// </summary>
    /// <param name="message">ASCII message to send.</param>
    Task SendAsync(string message);

    /// <summary>
    /// Reads data from the TCP stream.
    /// </summary>
    /// <returns>Received ASCII string, or null if no data.</returns>
    Task<string?> ReadAsync();

    /// <summary>
    /// Closes the active TCP connection.
    /// </summary>
    void Disconnect();
}
