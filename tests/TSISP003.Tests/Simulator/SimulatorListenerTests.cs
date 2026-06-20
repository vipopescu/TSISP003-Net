using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TSISP003.Protocol;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Tcp;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorListenerTests
{
    [Fact]
    public async Task StartSession_OverRealSocket_ReturnsSeed()
    {
        var options = new SimulatorOptions { Address = "01", Port = 0, Seed = 0x12 };
        using var listener = new SimulatorListener(options, NullLogger<SimulatorListener>.Instance);
        listener.Start();
        int port = listener.BoundPort;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        string start = PacketCodec.BuildData(0, 0, "01", "02");
        byte[] outBytes = Encoding.ASCII.GetBytes(start);
        await stream.WriteAsync(outBytes);

        // Read until we have received both the link-ACK and the data packet with the seed.
        // Two WriteAsync calls in the listener may arrive in one or two reads.
        var buffer = new byte[1024];
        string response = string.Empty;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!response.Contains("0312"))
        {
            int read = await stream.ReadAsync(buffer, cts.Token);
            if (read == 0) break;
            response += Encoding.ASCII.GetString(buffer, 0, read);
        }

        // Response should contain the password-seed data packet "0312".
        Assert.Contains("0312", response);

        await listener.StopAsync(default);
    }
}
