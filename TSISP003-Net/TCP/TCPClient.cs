using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync(_ipAddress, _port);
        }

        public void Disconnect()
        {
            _client.Close();
        }

        public async Task SendAsync(string message)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                if (!_client.Connected)
                    await ConnectAsync();

                NetworkStream stream = _client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);

                await stream.WriteAsync(data, 0, data.Length);
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
                if (!_client.Connected)
                    await ConnectAsync();

                NetworkStream stream = _client.GetStream();
                byte[] data = new byte[1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int numBytesRead;
                    while ((numBytesRead = await stream.ReadAsync(data, 0, data.Length)) > 0)
                    {
                        ms.Write(data, 0, numBytesRead);
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            finally
            {
                _readSemaphore.Release();
            }
        }
    }

}