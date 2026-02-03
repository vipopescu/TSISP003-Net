using System.Net.Sockets;
using System.Text;

namespace TSISP003.Infrastructure.Tcp;

/// <summary>
/// Provides a TCP client with retry and timeout logic for reliable send/receive.
/// </summary>
public class TCPClient : IDisposable
{
    private readonly string _ipAddress;
    private readonly int _port;
    private TcpClient? _client;
    private readonly string _name;
    private readonly SemaphoreSlim _readSemaphore = new(1, 1);
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);

    public TCPClient(string ipAddress, int port, string name)
    {
        _ipAddress = ipAddress;
        _port = port;
        _client = null;
        _name = name;
    }

    private async Task ConnectAsync()
    {
        Disconnect();
        for (var attempt = 1; ; attempt++)
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(_ipAddress, _port);
                return;
            }
            catch (SocketException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay);
            }
        }
    }

    /// <summary>
    /// Closes the active TCP connection, if any.
    /// </summary>
    public void Disconnect()
    {
        if (_client != null)
        {
            try { _client.Close(); }
            catch { }
            finally { _client = null; }
        }
    }

    /// <summary>
    /// Sends the specified message over TCP, with automatic retry on failure.
    /// </summary>
    /// <param name="message">ASCII message to send.</param>
    public async Task SendAsync(string message)
    {
        await _writeSemaphore.WaitAsync();
        try
        {
            var data = Encoding.ASCII.GetBytes(message);
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (_client == null || !_client.Connected)
                    await ConnectAsync();

                if (_client?.Connected != true)
                    continue;

                try
                {
                    var stream = _client.GetStream();
                    Console.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss.fff} {_name} => {BytesToHexString(data, data.Length)}");
                    await stream.WriteAsync(data, 0, data.Length);
                    return;
                }
                catch (IOException) when (attempt < MaxRetries)
                {
                    Disconnect();
                    await Task.Delay(RetryDelay);
                }
            }
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// Reads data from the TCP stream, retrying on transient errors. Returns null on timeout or disconnect.
    /// </summary>
    /// <returns>Received ASCII string, or null if no data.</returns>
    public async Task<string?> ReadAsync()
    {
        await _readSemaphore.WaitAsync();
        try
        {
            var buffer = new byte[4096];
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (_client == null || !_client.Connected)
                    await ConnectAsync();

                if (_client?.Connected != true)
                    continue;

                try
                {
                    using var cts = new CancellationTokenSource(ReadTimeout);
                    var stream = _client.GetStream();
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    if (bytesRead <= 0)
                        return null;

                    Console.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss.fff} {_name} <= {BytesToHexString(buffer, bytesRead)}");
                    return Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
                catch (OperationCanceledException) when (attempt < MaxRetries)
                {
                    // timeout, retry
                }
                catch (IOException) when (attempt < MaxRetries)
                {
                    Disconnect();
                    await Task.Delay(RetryDelay);
                }
            }
            return null;
        }
        finally
        {
            _readSemaphore.Release();
        }
    }


    private static string BytesToHexString(byte[] bytes, int length)
    {
        StringBuilder hex = new StringBuilder(length * 2);
        for (int i = 0; i < length; i++)
            hex.AppendFormat("{0:X2} ", bytes[i]);
        return hex.ToString().Trim();  // Trim to remove the trailing space
    }

    /// <summary>
    /// Releases internal resources and closes the TCP connection.
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        _readSemaphore.Dispose();
        _writeSemaphore.Dispose();
    }
}
