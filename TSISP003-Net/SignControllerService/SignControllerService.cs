
using TSISP003.ProtocolUtils;
using TSISP003.TCP;

namespace TSISP003.SignControllerService
{
    public class SignControllerService : ISignControllerService
    {
        private Task heartBeatPollTask;
        private Task socketReaderTask;

        private readonly TCPClient _tcpClient;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SignControllerService(TCPClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            heartBeatPollTask = Task.Run(() => HeartBeatPollTask(_cancellationTokenSource.Token));

            return Task.CompletedTask;
        }

        private async void HeartBeatPollTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await HeartbeatPoll();
                ReadStream();
                Thread.Sleep(3000);
            }
        }

        private async void ReadStream()
        {
            try
            {
                string response = await _tcpClient.ReadAsync();
                if (!string.IsNullOrEmpty(response))
                {
                    Console.WriteLine("Processing response");
                    ProcessResponse(response);
                }
                else
                {
                    Console.WriteLine("Empty response");
                }
            }
            catch (Exception ex)
            {
                // Ignore 
                Thread.Sleep(3000);
            }
        }

        private void ProcessResponse(string response)
        {
            foreach (var command in Utils.GetChunks(response))
            {
                Console.WriteLine("Response: " + command);
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public async Task HeartbeatPoll()
        {
            Console.WriteLine("Sending poll");
            await _tcpClient.SendAsync("310210210212");
        }

        public Task StartSession()
        {
            throw new NotImplementedException();
        }

        public Task Password()
        {
            throw new NotImplementedException();
        }

        public Task EndSession()
        {
            throw new NotImplementedException();
        }

        public Task SystemReset()
        {
            throw new NotImplementedException();
        }

        public Task UpdateTime()
        {
            throw new NotImplementedException();
        }

        public Task SignSetTextFrame()
        {
            throw new NotImplementedException();
        }

        public Task SignSetGraphicsFrame()
        {
            throw new NotImplementedException();
        }

        public Task SignSetHighResolutionGraphicsFrame()
        {
            throw new NotImplementedException();
        }

        public Task SignConfigurationRequest()
        {
            throw new NotImplementedException();
        }

        public Task SignDisplayAtomicFrames()
        {
            throw new NotImplementedException();
        }

        public Task SignSetMessage()
        {
            throw new NotImplementedException();
        }

        public Task SignSetPlan()
        {
            throw new NotImplementedException();
        }

        public Task SignDisplayFrame()
        {
            throw new NotImplementedException();
        }

        public Task SignDisplayMessage()
        {
            throw new NotImplementedException();
        }

        public Task EnablePlan()
        {
            throw new NotImplementedException();
        }

        public Task DisablePlan()
        {
            throw new NotImplementedException();
        }

        public Task RequestEnabledPlans()
        {
            throw new NotImplementedException();
        }

        public Task SignSetDimmingLevel()
        {
            throw new NotImplementedException();
        }

        public Task PowerOnOff()
        {
            throw new NotImplementedException();
        }

        public Task DisableEnableDevice()
        {
            throw new NotImplementedException();
        }

        public Task SignRequestStoredFrameMessagePlan()
        {
            throw new NotImplementedException();
        }

        public Task SignExtendedStatusRequest()
        {
            throw new NotImplementedException();
        }

        public Task RetrieveFaultLog()
        {
            throw new NotImplementedException();
        }

        public Task ResetFaultLog()
        {
            throw new NotImplementedException();
        }

        public Task HARSetVoiceDataIncomplete()
        {
            throw new NotImplementedException();
        }

        public Task HARSetVoiceDataComplete()
        {
            throw new NotImplementedException();
        }

        public Task HARSetStrategy()
        {
            throw new NotImplementedException();
        }

        public Task HARActivateStrategy()
        {
            throw new NotImplementedException();
        }

        public Task HARSetPlan()
        {
            throw new NotImplementedException();
        }

        public Task HARRequestStoredVoiceStrategyPlan()
        {
            throw new NotImplementedException();
        }

        public Task RequestEnvironmentalWeatherValues()
        {
            throw new NotImplementedException();
        }

        public Task EnvironmentalWeatherValues()
        {
            throw new NotImplementedException();
        }

        public Task EnvironmentalWeatherThresholdDefinition()
        {
            throw new NotImplementedException();
        }

        public Task RequestThresholdDefinition()
        {
            throw new NotImplementedException();
        }

        public Task RequestEnvironmentalWeatherEventLog()
        {
            throw new NotImplementedException();
        }

        public Task ResetEnvironmentalWeatherEventLog()
        {
            throw new NotImplementedException();
        }

        public Task ProcessPasswordSeed()
        {
            throw new NotImplementedException();
        }

        public Task ProcessAcknowledge()
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignStatusReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARStatusReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherStatusReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignConfigurationReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessReportEnabledPlans()
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignExtendedStatusReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessFaultLogReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARVoiceDataAck()
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARVoiceDataNak()
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherValuesReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherThresholdDefinitionReply()
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherEventLogReply()
        {
            throw new NotImplementedException();
        }
    }
}