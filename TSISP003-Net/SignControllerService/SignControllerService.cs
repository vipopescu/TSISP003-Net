
using System.Runtime.CompilerServices;
using System.Text;
using TSISP003.ProtocolUtils;
using TSISP003.Settings;
using TSISP003.TCP;

namespace TSISP003.SignControllerService
{
    public class SignControllerService(TCPClient tcpClient, SignControllerConnectionOptions deviceSettings) : ISignControllerService, IDisposable
    {
        private Task heartBeatPollTask;
        private Task startSessionTask;
        private readonly TCPClient _tcpClient = tcpClient;
        private readonly SignControllerConnectionOptions _deviceSettings = deviceSettings;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);


            startSessionTask = Task.Run(() => StartSessionTask(_cancellationTokenSource.Token));

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            heartBeatPollTask.Dispose();
            throw new NotImplementedException();
        }

        private async void StartSessionTask(CancellationToken cancellationToken)
        {
            bool sessionStarted = false;
            while (!sessionStarted && !cancellationToken.IsCancellationRequested)
            {
                try
                {

                    // 1 - Send the start session
                    await StartSession();

                    // 2 - Receive an Ack and the password seed
                    string response = await _tcpClient.ReadAsync();
                    bool isAcknowledged = false;
                    string passwordSeed = string.Empty;

                    foreach (var packet in Utils.GetChunks(response))
                    {
                        // Iterate over both messages, we need to receive ACK and password seed response
                        if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                            isAcknowledged = packet[0] == SignControllerServiceConfig.ACK;
                        else if (packet[0] == SignControllerServiceConfig.SOH
                                    && Convert.ToInt32(packet[8..10], 16) == SignControllerServiceConfig.MI_PASSWORD_SEED)
                            passwordSeed = packet[8..10];
                    }

                    if (!isAcknowledged || string.IsNullOrEmpty(passwordSeed)) continue;

                    // 3 - Send the password command 
                    await Password(passwordSeed);

                    // 4 - Receive an ACK and ACK* mi code
                    response = await _tcpClient.ReadAsync();
                    isAcknowledged = false;
                    bool isAckProtocolReceived = false;

                    foreach (var packet in Utils.GetChunks(response))
                    {
                        // Iterate over both messages, we need to receive ACK and ACK from the protocol
                        if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                            isAcknowledged = packet[0] == SignControllerServiceConfig.ACK;
                        else if (packet[0] == SignControllerServiceConfig.MI_ACK_MESSAGE)
                            isAckProtocolReceived = true;
                    }

                    // 5 - If successful, get out
                    sessionStarted = isAcknowledged && isAckProtocolReceived;
                }
                catch (System.Net.Sockets.SocketException soex)
                {
                    Console.WriteLine($"Starting session failed: {soex.Message}");
                }
                catch (System.IO.IOException ioex)
                {
                    Console.WriteLine($"Starting session failed: {ioex.Message}");
                }
                catch (System.OperationCanceledException opex)
                {
                    Console.WriteLine($"Starting session failed: {opex.Message}");
                }
                finally
                {
                    if (!sessionStarted)
                    {
                        _tcpClient.Disconnect();
                        await Task.Delay(3000, cancellationToken);
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
                heartBeatPollTask = Task.Run(() => HeartBeatPollTask(_cancellationTokenSource.Token));
        }


        private async void HeartBeatPollTask(CancellationToken cancellationToken)
        {
            int failedAttempts = 0;
            const int maxAttempts = 3;

            while (!cancellationToken.IsCancellationRequested && failedAttempts < maxAttempts)
            {
                try
                {
                    await HeartbeatPoll();
                    await ReadStream();

                    failedAttempts = 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read from the socket: {ex.Message}");
                    failedAttempts++;
                    await Task.Delay(3000, cancellationToken);
                }
            }

            if (!cancellationToken.IsCancellationRequested && failedAttempts >= maxAttempts)
            {
                Console.WriteLine($"Cancelling the pool. Restarting the session...");
                startSessionTask = Task.Run(() => StartSessionTask(_cancellationTokenSource.Token));
            }
        }


        private async Task ReadStream()
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
            // build body
            string message = SignControllerServiceConfig.SOH // Start of header
                        + "00" + "00" // NS and NR
                        + _deviceSettings.Address // ADDR
                        + SignControllerServiceConfig.STX
                        + SignControllerServiceConfig.MI_HEARTBEAT_POLL.ToString("X2");

            // append crc and end of message
            message = message
                        + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                        + SignControllerServiceConfig.ETX;

            await _tcpClient.SendAsync(message);
        }

        public async Task StartSession()
        {
            // build body
            string message = SignControllerServiceConfig.SOH // Start of header
                        + "00" + "00" // NS and NR
                        + _deviceSettings.Address // ADDR
                        + SignControllerServiceConfig.STX
                        + SignControllerServiceConfig.MI_START_SESSION.ToString("X2");

            // append crc and end of message
            message = message
                        + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                        + SignControllerServiceConfig.ETX;

            await _tcpClient.SendAsync(message);
        }

        public async Task Password(string passwordSeed)
        {
            // build body
            string message = SignControllerServiceConfig.SOH // Start of header
                        + "00" + "00" // NS and NR
                        + _deviceSettings.Address // ADDR
                        + SignControllerServiceConfig.STX
                        + SignControllerServiceConfig.MI_PASSWORD.ToString("X2")
                        + Utils.GeneratePassword(passwordSeed, _deviceSettings.PasswordOffset);

            // append crc and end of message
            message = message
                        + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                        + SignControllerServiceConfig.ETX;

            await _tcpClient.SendAsync(message);
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