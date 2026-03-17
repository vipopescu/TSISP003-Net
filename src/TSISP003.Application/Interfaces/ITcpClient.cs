namespace TSISP003.Application.Interfaces;

public interface ITcpClient : IDisposable
{
    Task SendAsync(string message);
    Task<string?> ReadAsync();
    void Disconnect();
}
