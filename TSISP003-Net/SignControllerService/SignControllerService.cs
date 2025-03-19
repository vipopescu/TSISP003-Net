using System.Net.Sockets;
using System.Text;
using TSISP003.Settings;
using TSISP003.TCP;
using TSISP003.Utils;
using TSISP003_Net.SignControllerDataStore.Entities;
using TSISP003_Net.Utils;

namespace TSISP003.SignControllerService;

public class SignControllerService(TCPClient tcpClient, SignControllerConnectionOptions deviceSettings) : ISignControllerService, IDisposable
{
    private TaskCompletionSource<List<FaultLogEntry>>? _faultLogReplyTaskCompletion;
    private TaskCompletionSource<SignStatusReply>? _signStatusReplyTaskCompletion;
    private TaskCompletionSource<SignSetTextFrame>? _signSetTextFrameTaskCompletion;
    private TaskCompletionSource<SignSetMessage>? _signSetMessageTaskCompletion;
    private TaskCompletionSource<RejectReply>? _rejectReplyCompletion;
    private TaskCompletionSource<AckReply>? _ackReplyCompletion;

    private Task? heartBeatPollTask;
    private Task? startSessionTask;
    private readonly TCPClient _tcpClient = tcpClient;
    private readonly SignControllerConnectionOptions _deviceSettings = deviceSettings;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private SignStatusReply _signController = null;

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

                foreach (var packet in Functions.GetChunks(response, out _))
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

                foreach (var packet in Functions.GetChunks(response, out _))
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
            //await SignConfigurationRequest();
            //Thread.Sleep(5000);
            //await ReadStream();

            SignConfigurationReceived = true;
        }

        // We start the hearbeat
        while (!cancellationToken.IsCancellationRequested && failedAttempts < maxAttempts)
        {
            try
            {
                await HeartbeatPoll();
                Thread.Sleep(100);
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
                await Task.Delay(1000, cancellationToken);
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
            // Console.WriteLine($"Failed to read from the socket: {ex.Message}");
            // // Ignore 
            // Thread.Sleep(3000);
        }
    }

    // This buffer should persist between invocations.
    private string _incompleteBuffer = string.Empty;

    public void ProcessResponses(string responses)
    {
        // Combine previous incomplete data with the new responses.
        string data = _incompleteBuffer + responses;

        // Extract chunks and get any remaining incomplete part.
        string remaining;
        var chunks = Functions.GetChunks(data, out remaining);

        // Process each complete chunk.
        foreach (var packet in chunks)
        {
            // Determine the packet type by its starting character.
            if (packet[0] == SignControllerServiceConfig.ACK || packet[0] == SignControllerServiceConfig.NAK)
                ProcessNonDataPacket(packet);
            else if (packet[0] == SignControllerServiceConfig.SOH)
                DispatchDataPacket(packet);
            else
                Console.WriteLine("Unable to determine type of the packet.");
        }

        // Save the incomplete data for the next response.
        _incompleteBuffer = remaining;
    }


    /// <summary>
    /// Process non data packets
    /// </summary>
    /// <param name="packet"></param>
    private void ProcessNonDataPacket(string packet)
    {
        if (packet[0] == SignControllerServiceConfig.ACK)
        {
            NR = int.Parse(packet[1..3], System.Globalization.NumberStyles.HexNumber);
            NS++;
        }
        else if (packet[0] == SignControllerServiceConfig.NAK)
        {
            // TODO
        }

        // TODO: get the NS from here
        //Console.WriteLine(Functions.PacketCRC(Encoding.ASCII.GetBytes(packet[0..5])));
        //Console.WriteLine("Non Data Packet: " + packet);
    }

    /// <summary>
    /// Dispatch data packets
    /// </summary>
    /// <param name="packet"></param>
    private async void DispatchDataPacket(string packet)
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
            await ProcessSignStatusReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_PASSWORD_SEED)
            await ProcessPasswordSeed(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_HAR_STATUS_REPLY)
            await ProcessHARStatusReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY)
            await ProcessEnvironmentalWeatherStatusReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REPLY || miCode == SignControllerServiceConfig.MI_DISABLE_ENABLE_DEVICE)
            await ProcessSignConfigurationReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_REPORT_ENABLED_PLANS)
            await ProcessReportEnabledPlans(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_EXTENDED_STATUS_REPLY)
            await ProcessSignExtendedStatusReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_FAULT_LOG_REPLY)
            await ProcessFaultLogReply(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_TEXT_FRAME)
            await ProcessSignSetTextFrame(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_GRAPHIC_FRAME)
            await ProcessSignSetGraphicsFrame(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME)
            await ProcessSignSetHighResolutionGraphicsFrame(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_MESSAGE)
            await ProcessSignSetMessage(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_SIGN_SET_PLAN)
            await ProcessSignSetPlan(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_REJECT_MESSAGE)
            await ProcessRejectMessage(applicationData);
        else if (miCode == SignControllerServiceConfig.MI_ACK_MESSAGE)
            await ProcessAckMessage(applicationData);
        else
            Console.WriteLine("Unexpected mi code: " + miCode);

        int nr = Convert.ToInt32(packet[3..5], 16);
        NR = nr;
        NS = nr;

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
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
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
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
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
                    + Functions.GeneratePassword(passwordSeed, _deviceSettings.SeedOffset, _deviceSettings.PasswordOffset)[^4..];

        // append crc and end of message
        string crc = Functions.PacketCRC(Encoding.ASCII.GetBytes(message));
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
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
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
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    public Task UpdateTime()
    {
        // TODO
        throw new NotImplementedException();
    }

    public async Task<SignStatusReply> SignSetTextFrame(SignSetTextFrame request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        string applicationMessage = SignControllerServiceConfig.MI_SIGN_SET_TEXT_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2") + request.Revision.ToString("X2")
                   + request.Font.ToString("X2") + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2") + request.NumberOfCharsInText.ToString("X2")
                   + request.Text;

        applicationMessage = applicationMessage + Functions.PacketCRC(Encoding.ASCII.GetBytes(Functions.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign status reply timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _signStatusReplyTaskCompletion.Task;
    }

    public Task ProcessSignSetTextFrame(string applicationData)
    {
        try
        {
            SignSetTextFrame signSetTextFrameFeedback = new SignSetTextFrame() { Text = string.Empty };

            signSetTextFrameFeedback.FrameID = Convert.ToByte(applicationData[2..4], 16);
            signSetTextFrameFeedback.Revision = Convert.ToByte(applicationData[4..6], 16);
            signSetTextFrameFeedback.Font = Convert.ToByte(applicationData[6..8], 16);
            signSetTextFrameFeedback.Colour = Convert.ToByte(applicationData[8..10], 16);
            signSetTextFrameFeedback.Conspicuity = Convert.ToByte(applicationData[10..12], 16);
            signSetTextFrameFeedback.NumberOfCharsInText = Convert.ToByte(applicationData[12..14], 16);
            signSetTextFrameFeedback.Text = applicationData[14..(14 + signSetTextFrameFeedback.NumberOfCharsInText * 2)];
            signSetTextFrameFeedback.CRC = Convert.ToUInt16(applicationData[(14 + (signSetTextFrameFeedback.NumberOfCharsInText * 2))..(14 + (signSetTextFrameFeedback.NumberOfCharsInText * 2) + 4)], 16);

            _signSetTextFrameTaskCompletion?.TrySetResult(signSetTextFrameFeedback);

            return Task.CompletedTask;
        }
        catch (System.Exception)
        {

        }

        return Task.CompletedTask;
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
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);
    }

    public Task SignDisplayAtomicFrames()
    {
        // TODO
        throw new NotImplementedException();
    }

    public async Task<SignStatusReply> SignSetMessage(SignSetMessage request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        string message = header + SignControllerServiceConfig.MI_SIGN_SET_MESSAGE.ToString("X2")
                   + request.MessageID.ToString("X2") + request.Revision.ToString("X2")
                     + request.TransitionTimeBetweenFrames.ToString("X2")
                        + request.Frame1ID.ToString("X2") + request.Frame1Time.ToString("X2")
                        + request.Frame2ID.ToString("X2") + request.Frame2Time.ToString("X2")
                        + request.Frame3ID.ToString("X2") + request.Frame3Time.ToString("X2")
                        + request.Frame4ID.ToString("X2") + request.Frame4Time.ToString("X2")
                        + request.Frame5ID.ToString("X2") + request.Frame5Time.ToString("X2")
                        + request.Frame6ID.ToString("X2") + request.Frame6Time.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign status reply timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _signStatusReplyTaskCompletion.Task;
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

    public async Task<AckReply> SignDisplayMessage(SignDisplayMessage request)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        string message = header + SignControllerServiceConfig.MI_SIGN_DISPLAY_MESSAGE.ToString("X2")
                   + request.GroupID.ToString("X2") + request.MessageID.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign status reply timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _ackReplyCompletion.Task;
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

    /// <summary>
    /// Send a request to the sign controller to store a frame, message or plan
    /// </summary>
    /// <param name="requestType"></param>
    /// <param name="requestID"></param>
    /// <returns></returns>
    /// <exception cref="TimeoutException"></exception>
    public async Task<ISignResponse> SignRequestStoredFrameMessagePlan(Enums.RequestType requestType, byte requestID)
    {
        _signSetTextFrameTaskCompletion = new TaskCompletionSource<SignSetTextFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetMessageTaskCompletion = new TaskCompletionSource<SignSetMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN.ToString("X2")
                    + ((byte)requestType).ToString("X2") + requestID.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signSetTextFrameTaskCompletion.Task, _signSetMessageTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Fault log reply timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        if (completedTask == _signSetMessageTaskCompletion.Task)
        {
            return await _signSetMessageTaskCompletion.Task;
        }

        if (completedTask == _signSetTextFrameTaskCompletion.Task)
        {
            return await _signSetTextFrameTaskCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
    }

    public Task SignExtendedStatusRequest()
    {
        throw new NotImplementedException();
    }

    public async Task<List<FaultLogEntry>> RetrieveFaultLog()
    {
        _faultLogReplyTaskCompletion = new TaskCompletionSource<List<FaultLogEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_RETRIEVE_FAULT_LOG.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_faultLogReplyTaskCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Fault log reply timed out.");
        }

        return await _faultLogReplyTaskCompletion.Task;
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
        try
        {
            //private SignStatusReply _signController = null;

            if (_signController == null) _signController = new SignStatusReply();

            // Note: Protocol says 0 = Offline, 1 = Online, but some controllers use 0 = Offline, >1 = Online
            _signController.OnlineStatus = int.Parse(applicationData[2..4], System.Globalization.NumberStyles.HexNumber) > 0;

            _signController.Day = Convert.ToByte(applicationData[6..8], 16);
            _signController.Month = Convert.ToByte(applicationData[8..10], 16);
            _signController.Year = Convert.ToInt16(applicationData[10..14], 16);
            _signController.Hour = Convert.ToByte(applicationData[14..16], 16);
            _signController.Minute = Convert.ToByte(applicationData[16..18], 16);
            _signController.Second = Convert.ToByte(applicationData[18..20], 16);
            //applicationData[18..22] Controller Checksum -> We ignore the checksum

            byte errorCode = byte.Parse(applicationData[24..26]);

            _signController.ControllerErrorCode = errorCode;

            _signController.NumberOfSigns = byte.Parse(applicationData[26..28]);

            byte baseByte = 0;
            for (int i = 0; i < _signController.NumberOfSigns; i++)
            {
                baseByte = (byte)(28 + i * 18);
                byte signID = Convert.ToByte(applicationData[baseByte..(baseByte + 2)], 16);

                // Check if the sign is already in the list and add it if not
                SignStatus signStatus;
                if (_signController.Signs.ContainsKey(signID))
                    signStatus = _signController.Signs[signID];
                else
                {
                    signStatus = new SignStatus();
                    _signController.Signs.Add(signID, signStatus);
                }

                // Update the sign status
                signStatus.SignID = Convert.ToByte(applicationData[baseByte..(baseByte + 2)], 16);
                signStatus.SignErrorCode = Convert.ToByte(applicationData[(baseByte + 2)..(baseByte + 4)], 16);
                signStatus.SignEnabled = Convert.ToByte(applicationData[(baseByte + 4)..(baseByte + 6)], 16) == 1;
                signStatus.FrameID = Convert.ToByte(applicationData[(baseByte + 6)..(baseByte + 8)], 16);
                signStatus.FrameRevision = Convert.ToByte(applicationData[(baseByte + 8)..(baseByte + 10)], 16);
                signStatus.MessageID = Convert.ToByte(applicationData[(baseByte + 10)..(baseByte + 12)], 16);
                signStatus.MessageRevision = Convert.ToByte(applicationData[(baseByte + 12)..(baseByte + 14)], 16);
                signStatus.PlanID = Convert.ToByte(applicationData[(baseByte + 14)..(baseByte + 16)], 16);
                signStatus.PlanRevision = Convert.ToByte(applicationData[(baseByte + 16)..(baseByte + 18)], 16);
            }

            // if (errorCodeChanged) await RetrieveFaultLog();

        }
        catch (System.Exception)
        {
            Console.WriteLine("Failed to process sign status reply");
        }
        finally
        {
            _signStatusReplyTaskCompletion?.TrySetResult(_signController);
        }
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
            // _signController = new SignController();

            // byte numberOfGroups = Convert.ToByte(applicationData[22..24], 16);
            // _signController.NumberOfGroups = numberOfGroups;

            // short baseGroup = 22;
            // for (byte i = 0; i < numberOfGroups; i++) // Iterate over all groups
            // {
            //     // Build group with ID
            //     SignGroup signGroup = new SignGroup
            //     {
            //         GroupID = Convert.ToByte(applicationData[baseGroup..(baseGroup + 2)], 16)
            //     };

            //     byte numberOfSigns = Convert.ToByte(applicationData[(baseGroup + 2)..(baseGroup + 4)], 16);

            //     // Get all signs for the group
            //     short baseSign = (short)(baseGroup + 4);
            //     for (int nSign = 1; nSign <= numberOfSigns; nSign++) // Iterate over all signs
            //     {
            //         // Create new sign
            //         Sign sign = new Sign
            //         {
            //             SignID = Convert.ToByte(applicationData[baseSign..(baseSign + 2)], 16),
            //             SignType = (SignControllerServiceConfig.SignType)Convert.ToByte(applicationData[(baseSign + 2)..(baseSign + 4)], 16),
            //             SignWidth = Convert.ToInt16(applicationData[(baseSign + 4)..(baseSign + 8)], 16),
            //             SignHeight = Convert.ToInt16(applicationData[(baseSign + 8)..(baseSign + 12)], 16)
            //         };
            //         signGroup.Signs.Add(sign.SignID, sign);

            //         baseSign = (short)(baseSign + 12);
            //     }

            //     // Get Signature and add to list
            //     baseGroup = baseSign;
            //     byte signatureNumberOfBytes = Convert.ToByte(applicationData[baseGroup..(baseGroup + 2)], 16);
            //     signGroup.Signature = applicationData[(baseGroup + 2)..(baseGroup + 2 + signatureNumberOfBytes * 2)];
            //     _signController.Groups.Add(signGroup.GroupID, signGroup);

            //     // Continue with the next group
            //     baseGroup = (short)(baseGroup + 2 + (signatureNumberOfBytes * 2));
            // }

            // SignConfigurationReceived = true;

        }
        catch (System.Exception)
        {
            // TODO: Implement logger
        }
        finally
        {
            _signStatusReplyTaskCompletion?.TrySetResult(_signController);
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

    /// <summary>
    /// Process the fault log reply
    /// </summary>
    /// <param name="applicationData"></param>
    /// <returns></returns>
    public Task ProcessFaultLogReply(string applicationData)
    {
        List<FaultLogEntry> faultLogEntries = new List<FaultLogEntry>();

        byte numberOfEntries = Convert.ToByte(applicationData[2..4], 16);

        for (byte i = 0; i < numberOfEntries; i++)
        {
            byte baseByte = (byte)(4 + i * 22);


            byte groupId = Convert.ToByte(applicationData[baseByte..(baseByte + 2)], 16);
            byte entryNumberOfField = Convert.ToByte(applicationData[(baseByte + 2)..(baseByte + 4)], 16);
            byte dayOfMonth = Convert.ToByte(applicationData[(baseByte + 4)..(baseByte + 6)], 16);
            byte month = Convert.ToByte(applicationData[(baseByte + 6)..(baseByte + 8)], 16);
            short year = Convert.ToInt16(applicationData[(baseByte + 8)..(baseByte + 12)], 16);
            byte hour = Convert.ToByte(applicationData[(baseByte + 12)..(baseByte + 14)], 16);
            byte minute = Convert.ToByte(applicationData[(baseByte + 14)..(baseByte + 16)], 16);
            byte second = Convert.ToByte(applicationData[(baseByte + 16)..(baseByte + 18)], 16);
            byte errorCode = Convert.ToByte(applicationData[(baseByte + 18)..(baseByte + 20)], 16);
            bool faultCleared = Convert.ToByte(applicationData[(baseByte + 20)..(baseByte + 22)], 16) == 1;

            faultLogEntries.Add(new FaultLogEntry
            {
                Id = groupId,
                EntryNumber = entryNumberOfField,
                Day = dayOfMonth,
                Month = month,
                Year = year,
                Hour = hour,
                Minute = minute,
                Second = second,
                ErrorCode = errorCode,
                IsFaultCleared = faultCleared
            });
        }

        _faultLogReplyTaskCompletion?.TrySetResult(faultLogEntries);

        return Task.CompletedTask;
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

    /// <summary>
    /// Process the sign set message
    /// </summary>
    /// <param name="applicationData"></param>
    /// <returns></returns>
    public Task ProcessSignSetMessage(string applicationData)
    {
        try
        {
            SignSetMessage signSetMessageFeedback = new SignSetMessage();

            signSetMessageFeedback.MessageID = Convert.ToByte(applicationData[2..4], 16);
            signSetMessageFeedback.Revision = Convert.ToByte(applicationData[4..6], 16);
            signSetMessageFeedback.TransitionTimeBetweenFrames = Convert.ToByte(applicationData[6..8], 16);
            
            int index = 8;
            for (int i = 1; i <= 6; i++) // Loop through frames 1 to 6
            {
                byte frameID = Convert.ToByte(applicationData[index..(index + 2)], 16);
                if (frameID == 0) break; // Stop processing if FrameID is 0

                byte frameTime = Convert.ToByte(applicationData[(index + 2)..(index + 4)], 16);

                // Assign values dynamically
                switch (i)
                {
                    case 1:
                        signSetMessageFeedback.Frame1ID = frameID;
                        signSetMessageFeedback.Frame1Time = frameTime;
                        break;
                    case 2:
                        signSetMessageFeedback.Frame2ID = frameID;
                        signSetMessageFeedback.Frame2Time = frameTime;
                        break;
                    case 3:
                        signSetMessageFeedback.Frame3ID = frameID;
                        signSetMessageFeedback.Frame3Time = frameTime;
                        break;
                    case 4:
                        signSetMessageFeedback.Frame4ID = frameID;
                        signSetMessageFeedback.Frame4Time = frameTime;
                        break;
                    case 5:
                        signSetMessageFeedback.Frame5ID = frameID;
                        signSetMessageFeedback.Frame5Time = frameTime;
                        break;
                    case 6:
                        signSetMessageFeedback.Frame6ID = frameID;
                        signSetMessageFeedback.Frame6Time = frameTime;
                        break;
                }

                index += 4; // Move to the next frame
                if (index >= applicationData.Length) break; // Prevent index out of range
            }

            _signSetMessageTaskCompletion?.TrySetResult(signSetMessageFeedback);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process sign set message: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task ProcessSignSetPlan(string applicationData)
    {
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Process the reject message
    /// </summary>
    /// <param name="applicationData"></param>
    /// <returns></returns>
    public Task ProcessRejectMessage(string applicationData)
    {
        _rejectReplyCompletion?.TrySetResult(new RejectReply
        {
            RejectedMiCode = Convert.ToByte(applicationData[2..4], 16),
            ApplicationErrorCode = Convert.ToByte(applicationData[4..6], 16)
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process the ack message
    /// </summary>
    /// <param name="applicationData"></param>
    /// <returns></returns>
    public Task ProcessAckMessage(string applicationData)
    {
        _ackReplyCompletion?.TrySetResult(new AckReply());
        return Task.CompletedTask;
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
            // To reimplememt
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to get controller configuration", ex);
        }
    }

    /// <summary>
    /// Get the status of the controller
    /// </summary>
    /// <returns></returns>
    public Task<SignStatusReply> GetStatus()
    {
        return Task.FromResult(_signController);
    }
}