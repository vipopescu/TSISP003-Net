using System.Net.Sockets;
using System.Text;
using TSISP003.ProtocolUtils;
using TSISP003.Settings;
using TSISP003.TCP;
using TSISP003_Net.SignControllerDataStore.Entities;

namespace TSISP003.SignControllerService;

public class SignControllerService(TCPClient tcpClient, SignControllerConnectionOptions deviceSettings) : ISignControllerService, IDisposable
{
    private Task? heartBeatPollTask;
    private Task? startSessionTask;
    private readonly TCPClient _tcpClient = tcpClient;
    private readonly SignControllerConnectionOptions _deviceSettings = deviceSettings;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private SignController _signController = null;

    public bool SignConfigurationReceived { get; set; } = false;

    // TODO: Implement lock when setting the NS
    private int _ns;
    public int NS
    {
        get
        {
            return _ns;
        }
        set
        {
            _ns = value;
        }
    }

    // TODO: Implement lock when setting the NR
    private int _nr;
    public int NR
    {
        get
        {

            return _nr;
        }
        set
        {
            _nr = value;
        }
    }

    /// <summary>
    /// Start the session and start the heartbeat poll
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        startSessionTask = Task.Run(() => StartSessionTask(_cancellationTokenSource.Token));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose the SignControllerService
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        heartBeatPollTask?.Dispose();
    }

    /// <summary>
    /// Start the session task
    /// </summary>
    /// <param name="cancellationToken"></param>
    private async void StartSessionTask(CancellationToken cancellationToken)
    {
        bool sessionStarted = false;
        SignConfigurationReceived = false;

        while (!sessionStarted && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 1 - Send the start session
                await StartSession();

                Thread.Sleep(1000);

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
                        passwordSeed = packet[10..12];
                }

                if (!isAcknowledged || string.IsNullOrEmpty(passwordSeed)) continue;

                // 3 - Send the password command 
                await Password(passwordSeed);

                Thread.Sleep(1000);

                // 4 - Receive an ACK and ACK* mi code
                response = await _tcpClient.ReadAsync();
                isAcknowledged = false;
                bool isAckProtocolReceived = false;

                foreach (var packet in Utils.GetChunks(response))
                {
                    // Iterate over both messages, we need to receive ACK and ACK from the protocol
                    if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                        isAcknowledged = packet[0] == SignControllerServiceConfig.ACK;
                    else if (packet[8..10] == SignControllerServiceConfig.MI_ACK_MESSAGE.ToString("X2"))
                        isAckProtocolReceived = true;
                }

                // 5 - If successful, get out
                sessionStarted = isAcknowledged && isAckProtocolReceived;

                if (sessionStarted)
                {
                    Console.WriteLine("Session started successfully");
                    break;
                }
            }
            catch (SocketException soex)
            {
                Console.WriteLine($"Starting session failed: {soex.Message}");
            }
            catch (IOException ioex)
            {
                Console.WriteLine($"Starting session failed: {ioex.Message}");
            }
            catch (OperationCanceledException opex)
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

    /// <summary>
    /// Heartbeat poll task
    /// </summary>
    /// <param name="cancellationToken"></param>
    private async void HeartBeatPollTask(CancellationToken cancellationToken)
    {
        int failedAttempts = 0;
        const int maxAttempts = 3;
        NS = 0;
        NR = 0;

        // We request the configuration and read it until we receive it
        while (!cancellationToken.IsCancellationRequested && !SignConfigurationReceived)
        {
            await SignConfigurationRequest();
            Thread.Sleep(5000);
            await ReadStream();
        }

        // We start the hearbeat
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
            }
            finally
            {
                await Task.Delay(3000, cancellationToken);
            }
        }

        // If we reach the max attempts, we restart the session
        if (!cancellationToken.IsCancellationRequested && failedAttempts >= maxAttempts)
        {
            Console.WriteLine($"Cancelling the pool. Restarting the session...");
            startSessionTask = Task.Run(() => StartSessionTask(_cancellationTokenSource.Token));
        }
    }

    /// <summary>
    /// Read the stream and process the responses
    /// </summary>
    /// <returns></returns>
    private async Task ReadStream()
    {
        try
        {
            string response = await _tcpClient.ReadAsync();
            if (!string.IsNullOrEmpty(response))
            {
                ProcessResponses(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read from the socket: {ex.Message}");
            // Ignore 
            Thread.Sleep(3000);
        }
    }

    /// <summary>
    /// Process the responses
    /// </summary>
    /// <param name="responses"></param>
    private void ProcessResponses(string responses)
    {
        foreach (var packet in Utils.GetChunks(responses))
        {
            // Check if it's data or non data packet
            if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                ProcessNonDataPacket(packet);
            else if (packet[0] == SignControllerServiceConfig.SOH)
                DispatchDataPacket(packet);
            else Console.WriteLine("Unable to determine type of the packet.");
        }
    }

    /// <summary>
    /// Process non data packets
    /// </summary>
    /// <param name="packet"></param>
    private void ProcessNonDataPacket(string packet)
    {
        if (packet[0] == SignControllerServiceConfig.ACK)
        {
            NR = int.Parse(packet[1..3]);
            NS++;
        }
        else if (packet[0] == SignControllerServiceConfig.NAK)
        {
            // TODO
        }

        // TODO: get the NS from here
        //Console.WriteLine(Utils.PacketCRC(Encoding.ASCII.GetBytes(packet[0..5])));
        //Console.WriteLine("Non Data Packet: " + packet);
    }

    /// <summary>
    /// Dispatch data packets
    /// </summary>
    /// <param name="packet"></param>
    private void DispatchDataPacket(string packet)
    {
        // packet[0]                        -> SOH
        // packet[1..3]                     -> NR
        // packet[4..6]                     -> NS
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
        else if (miCode == SignControllerServiceConfig.MI_REJECT_MESSAGE)
            ProcessRejectMessage(applicationData);
        else
            Console.WriteLine("Unexpected mi code: " + miCode);
    }

    /// <summary>
    /// Stop the session and the heartbeat poll
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a heartbeat poll
    /// </summary>
    /// <returns></returns>
    public async Task HeartbeatPoll()
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_HEARTBEAT_POLL.ToString("X2");

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// Start a session
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Send the password command
    /// </summary>
    /// <param name="passwordSeed"></param>
    /// <returns></returns>
    public async Task Password(string passwordSeed)
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + "00" + "00" // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_PASSWORD.ToString("X2")
                    + Utils.GeneratePassword(passwordSeed, _deviceSettings.SeedOffset, _deviceSettings.PasswordOffset)[^4..];

        // append crc and end of message
        string crc = Utils.PacketCRC(Encoding.ASCII.GetBytes(message));
        message = message + crc
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// End the current session
    /// </summary>
    /// <returns></returns>
    /// TODO: We need a logic here to not start again the session if we manually stopped it
    public async Task EndSession()
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_END_SESSION.ToString("X2");

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// Send system reset command
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="resetLevel"></param>
    /// <returns></returns>
    public async Task SystemReset(byte groupId, byte resetLevel)
    {
        // Todo: validate data

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_END_SESSION.ToString("X2");

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    public Task UpdateTime()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignSetTextFrame()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignSetTextFrame(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }


    public Task SignSetGraphicsFrame()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignSetGraphicsFrame(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignSetHighResolutionGraphicsFrame()
    {
        // TODO
        throw new NotImplementedException();
    }

    public async Task SignConfigurationRequest()
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REQUEST.ToString("X2");

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    public Task SignDisplayAtomicFrames()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignSetMessage()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignSetPlan()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignDisplayFrame()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignDisplayMessage()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task EnablePlan()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task DisablePlan()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task RequestEnabledPlans()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task SignSetDimmingLevel()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task PowerOnOff()
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task DisableEnableDevice()
    {
        // TODO
        throw new NotImplementedException();
    }

    public async Task SignRequestStoredFrameMessagePlan()
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN.ToString("X2")
                    + "00" + "03";

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    public Task SignExtendedStatusRequest()
    {
        throw new NotImplementedException();
    }

    public async Task RetrieveFaultLog()
    {
        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_RETRIEVE_FAULT_LOG.ToString("X2");

        // append crc and end of message
        message = message
                    + Utils.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
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

    public async Task ProcessSignStatusReply(string applicationData)
    {
        bool errorCodeChanged = false;

        _signController.OnlineStatus = bool.Parse(applicationData[2..4]);
        _signController.DateChange = new DateTime(
            year: int.Parse(applicationData[8..12]),
            month: int.Parse(applicationData[6..8]),
            day: int.Parse(applicationData[4..6]),
            hour: int.Parse(applicationData[12..14]),
            minute: int.Parse(applicationData[14..16]),
            second: int.Parse(applicationData[16..18])
        );

        //applicationData[18..22] Controller Checksum -> We ignore the checksum

        byte errorCode = byte.Parse(applicationData[22..24]);
        if (errorCode != _signController.ControllerErrorCode) errorCodeChanged = true;
        _signController.ControllerErrorCode = errorCode;

        byte numberOfSigns = byte.Parse(applicationData[24..26]);

        byte baseSign = 26;
        for (int i = 0; i < numberOfSigns; i++)
        {
            // applicationData[26..28] - Sign ID
            // applicationData[28..30] - Sign Error Code
            // applicationData[30..32] - Sign Enabled Disabled
            // applicationData[32..34] - Frame ID Displayed
            // applicationData[34..36] - Frame Revision
            // applicationData[36..38] - Message ID Displayed
            // applicationData[38..40] - Message Revision
            // applicationData[40..42] - Plan ID Displayed
            // applicationData[42..44] - Plan Revision
            baseSign += 18;
        }

        if (errorCodeChanged) await RetrieveFaultLog();
    }

    public Task ProcessHARStatusReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessEnvironmentalWeatherStatusReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Process the sign configuration reply
    /// </summary>
    /// <param name="applicationData"></param>
    /// <returns></returns>
    public Task ProcessSignConfigurationReply(string applicationData)
    {
        try
        {
            _signController = new SignController();

            byte numberOfGroups = Convert.ToByte(applicationData[22..24], 16);
            _signController.NumberOfGroups = numberOfGroups;

            short baseGroup = 22;
            for (byte i = 0; i < numberOfGroups; i++) // Iterate over all groups
            {
                // Build group with ID
                SignGroup signGroup = new SignGroup
                {
                    GroupID = Convert.ToByte(applicationData[baseGroup..(baseGroup + 2)], 16)
                };

                byte numberOfSigns = Convert.ToByte(applicationData[(baseGroup + 2)..(baseGroup + 4)], 16);

                // Get all signs for the group
                short baseSign = (short)(baseGroup + 4);
                for (int nSign = 1; nSign <= numberOfSigns; nSign++) // Iterate over all signs
                {
                    // Create new sign
                    Sign sign = new Sign
                    {
                        SignID = Convert.ToByte(applicationData[baseSign..(baseSign + 2)], 16),
                        SignType = (SignControllerServiceConfig.SignType)Convert.ToByte(applicationData[(baseSign + 2)..(baseSign + 4)], 16),
                        SignWidth = Convert.ToInt16(applicationData[(baseSign + 4)..(baseSign + 8)], 16),
                        SignHeight = Convert.ToInt16(applicationData[(baseSign + 8)..(baseSign + 12)], 16)
                    };
                    signGroup.Signs.Add(sign.SignID, sign);

                    baseSign = (short)(baseSign + 12);
                }

                // Get Signature and add to list
                baseGroup = baseSign;
                byte signatureNumberOfBytes = Convert.ToByte(applicationData[baseGroup..(baseGroup + 2)], 16);
                signGroup.Signature = applicationData[(baseGroup + 2)..(baseGroup + 2 + signatureNumberOfBytes * 2)];
                _signController.Groups.Add(signGroup.GroupID, signGroup);

                // Continue with the next group
                baseGroup = (short)(baseGroup + 2 + (signatureNumberOfBytes * 2));
            }

            SignConfigurationReceived = true;
        }
        catch (System.Exception)
        {
            // TODO: Implement logger
        }
        return Task.CompletedTask;
    }

    public Task ProcessReportEnabledPlans(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignExtendedStatusReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessFaultLogReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessHARVoiceDataAck(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessHARVoiceDataNak(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessEnvironmentalWeatherValuesReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessEnvironmentalWeatherThresholdDefinitionReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessEnvironmentalWeatherEventLogReply(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignSetHighResolutionGraphicsFrame(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignSetMessage(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessSignSetPlan(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task ProcessRejectMessage(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    private bool GroupIDExists(byte groupId)
    {
        // TODO: I need to get this from the configuration that I read from the controller.
        throw new NotImplementedException();
    }

    /// <summary>
    /// Validate reset level
    /// </summary>
    /// <param name="resetLevel"></param>
    /// <returns></returns>
    private bool IsValidResetLevel(byte resetLevel)
    {
        return resetLevel == (byte)SignControllerServiceConfig.ResetLevel.RESET_LEVEL_ZERO
                || resetLevel == (byte)SignControllerServiceConfig.ResetLevel.RESET_LEVEL_ONE
                || resetLevel == (byte)SignControllerServiceConfig.ResetLevel.RESET_LEVEL_TWO
                || resetLevel == (byte)SignControllerServiceConfig.ResetLevel.RESET_LEVEL_THREE
                || resetLevel == (byte)SignControllerServiceConfig.ResetLevel.RESET_LEVEL_FACTORY;
    }

    /// <summary>
    /// Get the controller configuration
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task<SignController> GetControllerConfigurationAsync()
    {
        try
        {
            return Task.FromResult(_signController);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to get controller configuration", ex);
        }
    }
}