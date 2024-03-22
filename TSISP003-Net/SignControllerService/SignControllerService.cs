
using System.Runtime.CompilerServices;
using System.Text;
using TSISP003.ProtocolUtils;
using TSISP003.TCP;

namespace TSISP003.SignControllerService
{
    public class SignControllerService(TCPClient tcpClient) : ISignControllerService, IDisposable
    {
        private Task heartBeatPollTask;
        private readonly TCPClient _tcpClient = tcpClient;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            heartBeatPollTask = Task.Run(() => HeartBeatPollTask(_cancellationTokenSource.Token));

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            heartBeatPollTask.Dispose();
            throw new NotImplementedException();
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
                    ProcessResponses(response);
                }
            }
            catch (Exception ex)
            {
                // Ignore 
                Thread.Sleep(3000);
            }
        }

        private void ProcessResponses(string responses)
        {
            foreach (var packet in Utils.GetChunks(responses))
            {
                if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                    ProcessNonDataPacket(packet);
                else if (packet[0] == SignControllerServiceConfig.SOH)
                    DispatchDataPacket(packet);
                else Console.WriteLine("Unable to determine type of the packet.");
            }
        }

        private void ProcessNonDataPacket(string packet)
        {
            // TODO: get the NS from here
            //Console.WriteLine(Utils.PacketCRC(Encoding.ASCII.GetBytes(packet[0..5])));
            //Console.WriteLine("Non Data Packet: " + packet);
        }

        private void DispatchDataPacket(string packet)
        {
            // packet[0]                        -> SOH
            // packet[1]                        -> NR
            // packet[2]                        -> NS
            // packet[4..6]                     -> SOH
            // packet[7]                        -> STX
            // packet[(packet.Length-5)..^1]    -> CRC
            // packet[8..^5]                    -> Packet Data 
            // packet[^1]                       -> ETX

            string applicationData = packet[8..^5];
            int miCode = Convert.ToInt32(packet[8..10], 16);

            if (miCode == SignControllerServiceConfig.MI_SIGN_STATUS_REPLY)
                ProcessSignStatusReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_PASSWORD_SEED)
                ProcessPasswordSeed(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_HAR_STATUS_REPLY)
                ProcessHARStatusReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY)
                ProcessEnvironmentalWeatherStatusReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REPLY)
                ProcessSignConfigurationReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_REPORT_ENABLED_PLANS)
                ProcessReportEnabledPlans(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_EXTENDED_STATUS_REPLY)
                ProcessSignExtendedStatusReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_FAULT_LOG_REPLY)
                ProcessFaultLogReply(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_TEXT_FRAME)
                ProcessSignSetTextFrame(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_GRAPHIC_FRAME)
                ProcessSignSetGraphicsFrame(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME)
                ProcessSignSetHighResolutionGraphicsFrame(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_MESSAGE)
                ProcessSignSetMessage(applicationData);
            else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_PLAN)
                ProcessSignSetMessage(applicationData);
            else
                Console.WriteLine("Unexpected mi code: " + miCode);
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

        public Task ProcessSignSetTextFrame(string applicationData)
        {
            throw new NotImplementedException();
        }


        public Task SignSetGraphicsFrame()
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignSetGraphicsFrame(string applicationData)
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

        public Task ProcessPasswordSeed(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAcknowledge(string applicationData)
        {
            // ?? not sure if I need this...
            throw new NotImplementedException();
        }

        public Task ProcessSignStatusReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARStatusReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherStatusReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignConfigurationReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessReportEnabledPlans(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignExtendedStatusReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessFaultLogReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARVoiceDataAck(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessHARVoiceDataNak(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherValuesReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherThresholdDefinitionReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessEnvironmentalWeatherEventLogReply(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignSetHighResolutionGraphicsFrame(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignSetMessage(string applicationData)
        {
            throw new NotImplementedException();
        }

        public Task ProcessSignSetPlan(string applicationData)
        {
            throw new NotImplementedException();
        }
    }
}