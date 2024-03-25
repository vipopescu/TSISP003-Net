using System.Net.Sockets;
using System.Text;

namespace TSISP003.TCP
{
    public class TCPClient
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private TcpClient _client;
        private SemaphoreSlim _readSemaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);

        public TCPClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _client = new TcpClient();
        }

        private async Task ConnectAsync()
        {
            Disconnect();
            if (_client == null) _client = new TcpClient();
            await _client.ConnectAsync(_ipAddress, _port);
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
        }

        public async Task SendAsync(string message)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                if (_client == null || !_client.Connected)
                    await ConnectAsync();

                if (_client.Connected)
                {
                    NetworkStream stream = _client.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(message);

                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task<string> ReadAsync()
        {
            await _readSemaphore.WaitAsync();
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource(250);

                if (!_client.Connected)
                    await ConnectAsync();

                NetworkStream stream = _client.GetStream();
                byte[] buffer = new byte[4096];
                using (MemoryStream ms = new MemoryStream())
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    if (bytesRead > 0) ms.Write(buffer, 0, bytesRead);
                    return Encoding.ASCII.GetString(ms.ToArray());
                }
            }
            finally
            {
                _readSemaphore.Release();
            }
        }

    }

}