
using TSISP003.TCP;

namespace TSISP003.SignControllerService
{
    public class SignControllerService : ISignControllerService
    {
        private readonly TCPClient _tcpClient;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SignControllerService(TCPClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await KeepAliveRoutine(_cancellationTokenSource.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async Task DoSomethingElse(CancellationToken cancellationToken)
        {
            await _tcpClient.SendAsync("TODO");

        }

        private async Task KeepAliveRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _tcpClient.SendAsync("TODO");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // Example keep-alive interval
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation
                    break;
                }
                catch (Exception ex)
                {
                    // Handle other exceptions, possibly logging them
                    Console.WriteLine($"Error in KeepAliveRoutine: {ex.Message}");
                }
            }
        }
    }
}