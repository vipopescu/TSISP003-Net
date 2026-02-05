using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TSISP003.Infrastructure.Tcp;

/// <summary>
/// Provides a TCP client with retry and timeout logic for reliable send/receive.
/// </summary>
public class TCPClient : ITCPClient
{
    private readonly string _ipAddress;
    private readonly int _port;
    private TcpClient? _client;
    private readonly string _name;
    private readonly ILogger<TCPClient>? _logger;
    private readonly SemaphoreSlim _readSemaphore = new(1, 1);
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);

    public TCPClient(string ipAddress, int port, string name, ILogger<TCPClient>? logger = null)
    {
        _ipAddress = ipAddress;
        _port = port;
        _client = null;
        _name = name;
        _logger = logger;
    }

    private async Task ConnectAsync()
    {
        Disconnect();
        for (var attempt = 1; ; attempt++)
        {
            _client = new TcpClient();
            try
            {
                _logger?.LogDebug("{Name} connecting to {IpAddress}:{Port} (attempt {Attempt})", _name, _ipAddress, _port, attempt);
                await _client.ConnectAsync(_ipAddress, _port);
                _logger?.LogInformation("{Name} connected to {IpAddress}:{Port}", _name, _ipAddress, _port);
                return;
            }
            catch (SocketException ex) when (attempt < MaxRetries)
            {
                _logger?.LogWarning(ex, "{Name} connection attempt {Attempt} failed, retrying...", _name, attempt);
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
            _logger?.LogDebug("{Name} disconnecting from {IpAddress}:{Port}", _name, _ipAddress, _port);
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
                    _logger?.LogDebug("{Timestamp} {Name} => {Data}", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), _name, BytesToHexString(data, data.Length));
                    await stream.WriteAsync(data, 0, data.Length);
                    return;
                }
                catch (IOException ex) when (attempt < MaxRetries)
                {
                    _logger?.LogWarning(ex, "{Name} send attempt {Attempt} failed, retrying...", _name, attempt);
                    Disconnect();
                    await Task.Delay(RetryDelay);
                }
            }
            _logger?.LogError("{Name} failed to send after {MaxRetries} attempts", _name, MaxRetries);
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

                    _logger?.LogDebug("{Timestamp} {Name} <= {Data}", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), _name, BytesToHexString(buffer, bytesRead));
                    return Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
                catch (OperationCanceledException) when (attempt < MaxRetries)
                {
                    _logger?.LogDebug("{Name} read timeout on attempt {Attempt}, retrying...", _name, attempt);
                }
                catch (IOException ex) when (attempt < MaxRetries)
                {
                    _logger?.LogWarning(ex, "{Name} read attempt {Attempt} failed, retrying...", _name, attempt);
                    Disconnect();
                    await Task.Delay(RetryDelay);
                }
            }
            _logger?.LogDebug("{Name} read returned no data after {MaxRetries} attempts", _name, MaxRetries);
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
