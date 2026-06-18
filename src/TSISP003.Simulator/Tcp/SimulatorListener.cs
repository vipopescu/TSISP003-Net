using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Session;
using TSISP003.Simulator.Storage;

namespace TSISP003.Simulator.Tcp;

public class SimulatorListener(SimulatorOptions options, ILogger<SimulatorListener> logger)
    : BackgroundService
{
    private readonly TcpListener _listener = new(IPAddress.Any, options.Port);
    private readonly SimulatorMemory _memory = new();

    public int BoundPort => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public void Start()
    {
        _listener.Start();
        StartAsync(CancellationToken.None);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_listener.Server.IsBound) _listener.Start();
        logger.LogInformation("TSISP003 simulator listening on port {Port}", BoundPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }

            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var replies = new SimulatorReplyBuilder(options);
        var session = new SimulatorSession(_memory, replies, options, () => DateTime.Now);
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];
            try
            {
                int read;
                while ((read = await stream.ReadAsync(buffer, token)) > 0)
                {
                    string incoming = Encoding.ASCII.GetString(buffer, 0, read);
                    foreach (var packet in session.Handle(incoming))
                    {
                        byte[] outBytes = Encoding.ASCII.GetBytes(packet);
                        await stream.WriteAsync(outBytes, token);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                logger.LogDebug("Client disconnected: {Message}", ex.Message);
            }
        }
    }

    public override void Dispose()
    {
        _listener.Dispose();
        base.Dispose();
    }
}
