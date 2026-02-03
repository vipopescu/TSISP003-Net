using TSISP003.DTOs;
using System.Net.Sockets;
using System.Text;
using TSISP003.Configuration;
using TSISP003.Infrastructure.Tcp;
using TSISP003.Utilities;
using TSISP003.Domain.Entities;

namespace TSISP003.Services;

public class SignControllerService(TCPClient tcpClient, SignControllerConnectionOptions deviceSettings) : ISignControllerService, IDisposable
{
    private TaskCompletionSource<List<FaultLogEntry>>? _faultLogReplyTaskCompletion;
    private TaskCompletionSource<SignStatusReply>? _signStatusReplyTaskCompletion;
    private TaskCompletionSource<SignSetTextFrame>? _signSetTextFrameTaskCompletion;
    private TaskCompletionSource<SignSetGraphicsFrame>? _signSetGraphicsFrameTaskCompletion;
    private TaskCompletionSource<SignSetHighResolutionGraphicsFrame>? _signSetHighResGraphicsFrameTaskCompletion;
    private TaskCompletionSource<SignSetMessage>? _signSetMessageTaskCompletion;
    private TaskCompletionSource<SignSetPlan>? _signSetPlanTaskCompletion;
    private TaskCompletionSource<ReportEnabledPlans>? _reportEnabledPlansTaskCompletion;
    private TaskCompletionSource<SignExtendedStatusReply>? _signExtendedStatusReplyTaskCompletion;
    private TaskCompletionSource<RejectReply>? _rejectReplyCompletion;
    private TaskCompletionSource<AckReply>? _ackReplyCompletion;

    private Task? heartBeatPollTask;
    private Task? startSessionTask;
    private readonly TCPClient _tcpClient = tcpClient;
    private readonly SignControllerConnectionOptions _deviceSettings = deviceSettings;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private SignStatusReply? _signController;

    public bool SignConfigurationReceived { get; set; } = false;

    // Lock object for thread-safe access to NS and NR sequence numbers
    private readonly Lock _sequenceLock = new();

    private int _ns;
    /// <summary>
    /// N(S) - Send Sequence Number. The sequence number of data packets sent by the master.
    /// Cycles between 1 and 255 after initial 0 at link establishment.
    /// </summary>
    public int NS
    {
        get
        {
            lock (_sequenceLock)
            {
                return _ns;
            }
        }
        set
        {
            lock (_sequenceLock)
            {
                _ns = value;
            }
        }
    }

    private int _nr;
    /// <summary>
    /// N(R) - Receive Sequence Number. The number of valid data packets received from the slave.
    /// Used to indicate to the slave which packet we expect next.
    /// Cycles between 1 and 255 after initial 0 at link establishment.
    /// </summary>
    public int NR
    {
        get
        {
            lock (_sequenceLock)
            {
                return _nr;
            }
        }
        set
        {
            lock (_sequenceLock)
            {
                _nr = value;
            }
        }
    }

    /// <summary>
    /// Increment a sequence number with wrap-around (0 -> 1 -> 2 -> ... -> 255 -> 1)
    /// Per TSI-SP-003: sequence numbers cycle between 1 and 255 after initial 0
    /// </summary>
    private static int IncrementSequenceNumber(int current)
    {
        if (current == 0 || current >= 255)
            return 1;
        return current + 1;
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
                var response = await _tcpClient.ReadAsync();
                if (response is null) continue;

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
                if (response is null) continue;

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
            string? response;
            // Continue reading until ReadAsync returns null
            while ((response = await _tcpClient.ReadAsync()) is not null)
            {
                if (!string.IsNullOrEmpty(response))
                {
                    ProcessResponses(response);
                }
            }
        }
        catch
        {
            // Handle exceptions as needed
            // Optionally, add error handling or retries here
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
    /// Process non data packets (ACK or NAK from slave)
    /// Non-data packet format: ACK/NAK | N(R) | ADDR | CRC | ETX
    /// </summary>
    /// <param name="packet"></param>
    private void ProcessNonDataPacket(string packet)
    {
        if (packet[0] == SignControllerServiceConfig.ACK)
        {
            // Slave's ACK contains N(R) which indicates how many valid data packets
            // the slave has received from us. This confirms our last packet was received.
            int slaveNR = int.Parse(packet[1..3], System.Globalization.NumberStyles.HexNumber);

            // Increment our send sequence number for the next packet we send
            NS = IncrementSequenceNumber(NS);
        }
        else if (packet[0] == SignControllerServiceConfig.NAK)
        {
            // NAK received - slave rejected our packet (sequence error or corrupted data)
            // We should retransmit the last packet (not incrementing NS)
            // TODO: Implement retransmission logic
        }
    }

    /// <summary>
    /// Dispatch data packets received from the slave
    /// Data packet format: SOH | N(S) | N(R) | ADDR | STX | Application Message | CRC | ETX
    /// </summary>
    /// <param name="packet"></param>
    private async void DispatchDataPacket(string packet)
    {
        // Data packet structure (ASCII Hex encoded except control chars):
        // packet[0]      -> SOH (Start of Header)
        // packet[1..3]   -> N(S) - Slave's send sequence number (2 hex chars = 1 byte)
        // packet[3..5]   -> N(R) - Slave's receive count (2 hex chars = 1 byte)
        // packet[5..7]   -> ADDR - Device address (2 hex chars = 1 byte)
        // packet[7]      -> STX (Start of Text)
        // packet[8..^5]  -> Application Message
        // packet[^5..^1] -> CRC (4 hex chars = 2 bytes)
        // packet[^1]     -> ETX (End of Text)

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
        else if (miCode == SignControllerServiceConfig.MI_SIGN_CONFIGURATION_REPLY)
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

        // Extract slave's N(S) from the received packet
        // Slave's N(R) at packet[3..5] indicates how many packets it received from us (not needed here)
        int slaveNS = Convert.ToInt32(packet[1..3], 16);

        // Update our N(R) to indicate we've received this data packet from the slave
        // Our N(R) should be one more than the slave's N(S) to indicate we expect the next one
        NR = IncrementSequenceNumber(slaveNS);
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
    /// <param name="groupId">Group ID - zero for device controller or non-zero for a specific device or group of devices</param>
    /// <param name="resetLevel">Reset level (0, 1, 2, 3, or 255 for factory reset)</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    /// <exception cref="ArgumentException">Thrown when an invalid reset level is provided</exception>
    public async Task<AckReply> SystemReset(byte groupId, byte resetLevel)
    {
        // Validate reset level
        if (!IsValidResetLevel(resetLevel))
        {
            throw new ArgumentException($"Invalid reset level: {resetLevel}. Valid values are 0, 1, 2, 3, or 255.", nameof(resetLevel));
        }

        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SYSTEM_RESET.ToString("X2")
                    + groupId.ToString("X2")
                    + resetLevel.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the ack reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("System reset request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _ackReplyCompletion.Task;
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


    /// <summary>
    /// Send a graphics frame to be stored in the sign controller's memory
    /// </summary>
    /// <param name="request">The graphics frame to store</param>
    /// <returns>SignStatusReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<SignStatusReply> SignSetGraphicsFrame(SignSetGraphicsFrame request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build header
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        // Build application message:
        // MI Code (0B) + FrameID + Revision + NumberOfRows + NumberOfColumns + Colour + Conspicuity + GraphicsLength (2 bytes) + GraphicsData
        string applicationMessage = SignControllerServiceConfig.MI_SIGN_SET_GRAPHIC_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2")
                   + request.Revision.ToString("X2")
                   + request.NumberOfRows.ToString("X2")
                   + request.NumberOfColumns.ToString("X2")
                   + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2")
                   + request.GraphicsLength.ToString("X4") // 2 bytes (WORD) for length
                   + request.GraphicsData;

        // Calculate application message CRC
        applicationMessage = applicationMessage + Functions.PacketCRC(Encoding.ASCII.GetBytes(Functions.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append packet crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the sign status reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign set graphics frame request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _signStatusReplyTaskCompletion.Task;
    }

    /// <summary>
    /// Process the sign set graphics frame response
    /// </summary>
    /// <param name="applicationData">The application data from the response</param>
    /// <returns></returns>
    public Task ProcessSignSetGraphicsFrame(string applicationData)
    {
        try
        {
            SignSetGraphicsFrame signSetGraphicsFrameFeedback = new SignSetGraphicsFrame();

            // Parse the application data according to the protocol:
            // Position 1: MI Code (0B) - already parsed by dispatcher
            // Position 2: Frame ID (1 byte)
            // Position 3: Revision (1 byte)
            // Position 4: Number of rows (1 byte)
            // Position 5: Number of columns (1 byte)
            // Position 6: Colour (1 byte)
            // Position 7: Conspicuity (1 byte)
            // Position 8-9: Graphics length (2 bytes, WORD)
            // Position 10+: Graphics data (variable)
            // Last 4 chars: CRC (2 bytes)

            signSetGraphicsFrameFeedback.FrameID = Convert.ToByte(applicationData[2..4], 16);
            signSetGraphicsFrameFeedback.Revision = Convert.ToByte(applicationData[4..6], 16);
            signSetGraphicsFrameFeedback.NumberOfRows = Convert.ToByte(applicationData[6..8], 16);
            signSetGraphicsFrameFeedback.NumberOfColumns = Convert.ToByte(applicationData[8..10], 16);
            signSetGraphicsFrameFeedback.Colour = Convert.ToByte(applicationData[10..12], 16);
            signSetGraphicsFrameFeedback.Conspicuity = Convert.ToByte(applicationData[12..14], 16);
            signSetGraphicsFrameFeedback.GraphicsLength = Convert.ToUInt16(applicationData[14..18], 16);

            // Graphics data starts at position 18 and goes for GraphicsLength * 2 hex characters
            int graphicsDataLength = signSetGraphicsFrameFeedback.GraphicsLength * 2;
            signSetGraphicsFrameFeedback.GraphicsData = applicationData[18..(18 + graphicsDataLength)];

            // CRC is the last 4 hex characters after the graphics data
            signSetGraphicsFrameFeedback.CRC = Convert.ToUInt16(applicationData[(18 + graphicsDataLength)..(18 + graphicsDataLength + 4)], 16);

            _signSetGraphicsFrameTaskCompletion?.TrySetResult(signSetGraphicsFrameFeedback);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process sign set graphics frame: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a high resolution graphics frame to be stored in the sign controller's memory
    /// Used for displays up to 65535 x 65535 pixels
    /// </summary>
    /// <param name="request">The high resolution graphics frame to store</param>
    /// <returns>SignStatusReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<SignStatusReply> SignSetHighResolutionGraphicsFrame(SignSetHighResolutionGraphicsFrame request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build header
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        // Build application message:
        // MI Code (1D) + FrameID + Revision + NumberOfRows (2 bytes) + NumberOfColumns (2 bytes) + Colour + Conspicuity + GraphicsLength (4 bytes) + GraphicsData
        string applicationMessage = SignControllerServiceConfig.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2")
                   + request.Revision.ToString("X2")
                   + request.NumberOfRows.ToString("X4") // 2 bytes (WORD) for rows
                   + request.NumberOfColumns.ToString("X4") // 2 bytes (WORD) for columns
                   + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2")
                   + request.GraphicsLength.ToString("X8") // 4 bytes (DWORD) for length
                   + request.GraphicsData;

        // Calculate application message CRC
        applicationMessage = applicationMessage + Functions.PacketCRC(Encoding.ASCII.GetBytes(Functions.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append packet crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the sign status reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign set high resolution graphics frame request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _signStatusReplyTaskCompletion.Task;
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

    /// <summary>
    /// Send a request to the sign controller to display atomic frames
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="TimeoutException"></exception>
    /// <exception cref="SignRequestRejectedException"></exception>
    public async Task<AckReply> SignDisplayAtomicFrames(SignDisplayAtomicFrame request)
    {

        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        string message = header + SignControllerServiceConfig.MI_SIGN_DISPLAY_ATOMIC_FRAMES.ToString("X2")
            + request.GroupID.ToString("X2") + request.NumbeOfSigns.ToString("X2");

        foreach (var frame in request.Frames)
        {
            message += frame.SignID.ToString("X2") + frame.FrameID.ToString("X2");
        }

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
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

    /// <summary>
    /// Send a plan to be stored in the sign controller's memory
    /// A plan can contain up to 6 frames or messages scheduled by time and day of week
    /// </summary>
    /// <param name="request">The plan to store</param>
    /// <returns>SignStatusReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<SignStatusReply> SignSetPlan(SignSetPlan request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build header
        string header = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX;

        // Build application message:
        // MI Code (0D) + PlanID + Revision + DayOfWeek + [FrameMessageType + FrameMessageID + StartHour + StartMinute + StopHour + StopMinute]...
        string applicationMessage = SignControllerServiceConfig.MI_SIGN_SET_PLAN.ToString("X2")
                   + request.PlanID.ToString("X2")
                   + request.Revision.ToString("X2")
                   + request.DayOfWeek.ToString("X2");

        // Add each entry
        foreach (var entry in request.Entries)
        {
            applicationMessage += entry.FrameMessageType.ToString("X2")
                               + entry.FrameMessageID.ToString("X2")
                               + entry.StartHour.ToString("X2")
                               + entry.StartMinute.ToString("X2")
                               + entry.StopHour.ToString("X2")
                               + entry.StopMinute.ToString("X2");
        }

        // Add terminating zero if less than 6 entries (indicates end of plan)
        if (request.Entries.Count < 6)
        {
            applicationMessage += "00"; // FrameMessageType = 0 indicates end of plan
        }

        var message = header + applicationMessage;

        // append packet crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the sign status reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign set plan request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _signStatusReplyTaskCompletion.Task;
    }

    public async Task<AckReply> SignDisplayFrame(SignDisplayFrame request)
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

    /// <summary>
    /// Enable a pre-stored plan in a specified group
    /// </summary>
    /// <param name="groupId">Group ID - the group where the plan is to be enabled</param>
    /// <param name="planId">Plan ID - identifies the plan as stored in the device controller's memory.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> EnablePlan(byte groupId, byte planId)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_ENABLE_PLAN.ToString("X2")
                    + groupId.ToString("X2")
                    + planId.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the ack reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Enable plan request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _ackReplyCompletion.Task;
    }

    /// <summary>
    /// Disable a pre-stored plan in a specified group
    /// </summary>
    /// <param name="groupId">Group ID - the group where the plan is to be disabled</param>
    /// <param name="planId">Plan ID - identifies the plan as stored in the device controller's memory.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> DisablePlan(byte groupId, byte planId)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_DISABLE_PLAN.ToString("X2")
                    + groupId.ToString("X2")
                    + planId.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the ack reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Disable plan request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _ackReplyCompletion.Task;
    }

    /// <summary>
    /// Request which plans are enabled in the device controller
    /// </summary>
    /// <returns>ReportEnabledPlans containing the list of enabled plans</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<ReportEnabledPlans> RequestEnabledPlans()
    {
        _reportEnabledPlansTaskCompletion = new TaskCompletionSource<ReportEnabledPlans>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body - just MI code, no additional data
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_REQUEST_ENABLED_PLANS.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_reportEnabledPlansTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Request enabled plans timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _reportEnabledPlansTaskCompletion.Task;
    }

    /// <summary>
    /// Set the dimming level for specified groups
    /// </summary>
    /// <param name="entries">List of tuples containing (groupId, dimmingMode, luminanceLevel).
    /// DimmingMode: 0 = Automatic, 1 = Manual.
    /// LuminanceLevel: 1-16 (ignored when dimmingMode is 0/Automatic).
    /// GroupID 0 applies to all groups.</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> SignSetDimmingLevel(List<(byte groupId, byte dimmingMode, byte luminanceLevel)> entries)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SIGN_SET_DIMMING_LEVEL.ToString("X2")
                    + ((byte)entries.Count).ToString("X2"); // number of entries

        // Add each entry: groupId, dimmingMode, luminanceLevel
        foreach (var entry in entries)
        {
            message += entry.groupId.ToString("X2")
                    + entry.dimmingMode.ToString("X2")
                    + entry.luminanceLevel.ToString("X2");
        }

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the ack reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign set dimming level request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        return await _ackReplyCompletion.Task;
    }

    public async Task<AckReply> PowerOnOff(byte groupId, bool powerOn)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_POWER_ON_OFF.ToString("X2")
                    + 1.ToString("X2") + groupId.ToString("X2") + (powerOn ? 1 : 0).ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the fault log reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Fault log reply timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            RejectReply rejectReply = await _rejectReplyCompletion.Task;
            throw new SignRequestRejectedException(rejectReply);
        }

        if (completedTask == _ackReplyCompletion.Task)
        {
            return await _ackReplyCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
    }

    /// <summary>
    /// Send a command to disable or enable device groups
    /// </summary>
    /// <param name="entries">List of tuples containing (groupId, enabled)</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> DisableEnableDevice(List<(byte groupId, bool enabled)> entries)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_DISABLE_ENABLE_DEVICE.ToString("X2")
                    + entries.Count.ToString("X2"); // number of entries

        // Add each entry (groupId + enabled flag)
        foreach (var entry in entries)
        {
            message += entry.groupId.ToString("X2") + (entry.enabled ? 1 : 0).ToString("X2");
        }

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
            throw new TimeoutException("Disable/Enable Device request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        if (completedTask == _ackReplyCompletion.Task)
        {
            return await _ackReplyCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
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
        _signSetGraphicsFrameTaskCompletion = new TaskCompletionSource<SignSetGraphicsFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetHighResGraphicsFrameTaskCompletion = new TaskCompletionSource<SignSetHighResolutionGraphicsFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetPlanTaskCompletion = new TaskCompletionSource<SignSetPlan>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(
            _signSetTextFrameTaskCompletion.Task,
            _signSetGraphicsFrameTaskCompletion.Task,
            _signSetHighResGraphicsFrameTaskCompletion.Task,
            _signSetMessageTaskCompletion.Task,
            _signSetPlanTaskCompletion.Task,
            _rejectReplyCompletion.Task,
            delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign request stored frame/message/plan timed out.");
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

        if (completedTask == _signSetGraphicsFrameTaskCompletion.Task)
        {
            return await _signSetGraphicsFrameTaskCompletion.Task;
        }

        if (completedTask == _signSetHighResGraphicsFrameTaskCompletion.Task)
        {
            return await _signSetHighResGraphicsFrameTaskCompletion.Task;
        }

        if (completedTask == _signSetPlanTaskCompletion.Task)
        {
            return await _signSetPlanTaskCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
    }

    /// <summary>
    /// Send a request for extended status information from the sign controller
    /// </summary>
    /// <returns>SignExtendedStatusReply containing detailed status information</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<SignExtendedStatusReply> SignExtendedStatusRequest()
    {
        _signExtendedStatusReplyTaskCompletion = new TaskCompletionSource<SignExtendedStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body - MI Code 0x1B (Sign Extended Status Request)
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_SIGN_EXTENDED_STATUS_REQUEST.ToString("X2");

        // append crc and end of message
        message = message
                    + Functions.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + SignControllerServiceConfig.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_signExtendedStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign Extended Status Request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        if (completedTask == _signExtendedStatusReplyTaskCompletion.Task)
        {
            return await _signExtendedStatusReplyTaskCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
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

    /// <summary>
    /// Send a command to reset the fault log stored in the controller's memory
    /// </summary>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> ResetFaultLog()
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body - MI Code 0x1A (Reset Fault Log)
        string message = SignControllerServiceConfig.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + SignControllerServiceConfig.STX
                    + SignControllerServiceConfig.MI_RESET_FAULT_LOG.ToString("X2");

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
            throw new TimeoutException("Reset Fault Log request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        if (completedTask == _ackReplyCompletion.Task)
        {
            return await _ackReplyCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
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
            if (_signController is not null)
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
            if (_signController is not null)
                _signStatusReplyTaskCompletion?.TrySetResult(_signController);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process the report enabled plans response
    /// </summary>
    /// <param name="applicationData">The application data from the response</param>
    /// <returns></returns>
    public Task ProcessReportEnabledPlans(string applicationData)
    {
        try
        {
            ReportEnabledPlans reportEnabledPlans = new ReportEnabledPlans();

            // Parse the application data according to the protocol:
            // Position 1: MI Code (13) - already parsed by dispatcher
            // Position 2: Number of entries (1 byte)
            // Position 3+: Entries (2 bytes each: group ID, plan ID)

            byte numberOfEntries = Convert.ToByte(applicationData[2..4], 16);

            for (int i = 0; i < numberOfEntries; i++)
            {
                int baseIndex = 4 + (i * 4); // 4 hex chars per entry (2 bytes)

                if (baseIndex + 4 > applicationData.Length)
                    break;

                var entry = new EnabledPlanEntry
                {
                    GroupID = Convert.ToByte(applicationData[baseIndex..(baseIndex + 2)], 16),
                    PlanID = Convert.ToByte(applicationData[(baseIndex + 2)..(baseIndex + 4)], 16)
                };

                reportEnabledPlans.Entries.Add(entry);
            }

            _reportEnabledPlansTaskCompletion?.TrySetResult(reportEnabledPlans);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process report enabled plans: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process the Sign Extended Status Reply message (MI Code 0x1C)
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    /// <returns></returns>
    public Task ProcessSignExtendedStatusReply(string applicationData)
    {
        try
        {
            var reply = new SignExtendedStatusReply();

            // Position 2: Online status (offset 2-4 in hex string)
            reply.OnlineStatus = Convert.ToByte(applicationData[2..4], 16) == 1;

            // Position 3: Application error code (offset 4-6)
            reply.ApplicationErrorCode = Convert.ToByte(applicationData[4..6], 16);

            // Position 4-13: Manufacturer code details (10 bytes = 20 chars, offset 6-26)
            reply.ManufacturerCode = applicationData[6..26];

            // Position 14: Day (offset 26-28)
            reply.Day = Convert.ToByte(applicationData[26..28], 16);

            // Position 15: Month (offset 28-30)
            reply.Month = Convert.ToByte(applicationData[28..30], 16);

            // Position 16-17: Year (WORD, offset 30-34)
            byte yearLow = Convert.ToByte(applicationData[30..32], 16);
            byte yearHigh = Convert.ToByte(applicationData[32..34], 16);
            reply.Year = (ushort)(yearLow + (yearHigh << 8));

            // Position 18: Hours (offset 34-36)
            reply.Hour = Convert.ToByte(applicationData[34..36], 16);

            // Position 19: Minutes (offset 36-38)
            reply.Minute = Convert.ToByte(applicationData[36..38], 16);

            // Position 20: Seconds (offset 38-40)
            reply.Second = Convert.ToByte(applicationData[38..40], 16);

            // Position 21: Controller error code (offset 40-42)
            reply.ControllerErrorCode = Convert.ToByte(applicationData[40..42], 16);

            // Position 22: Number of signs (offset 42-44)
            reply.NumberOfSigns = Convert.ToByte(applicationData[42..44], 16);

            // Parse each sign's extended status
            int offset = 44; // Starting position for sign data
            for (int i = 0; i < reply.NumberOfSigns; i++)
            {
                var signStatus = new SignExtendedStatus();

                // Sign ID
                signStatus.SignID = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Sign type (0=text, 1=graphics, 2=advanced graphics)
                signStatus.SignType = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Number of rows
                signStatus.NumberOfRows = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Number of columns
                signStatus.NumberOfColumns = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Sign error code
                signStatus.SignErrorCode = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Dimming mode (0=automatic, 1=manual)
                signStatus.DimmingMode = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Luminance level (1-16)
                signStatus.LuminanceLevel = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Lamp/LED status length
                signStatus.LampLedStatusLength = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Lamp/LED status data (variable length)
                int lampLedDataLength = signStatus.LampLedStatusLength * 2; // Convert bytes to hex chars
                if (lampLedDataLength > 0 && offset + lampLedDataLength <= applicationData.Length)
                {
                    signStatus.LampLedStatus = applicationData[offset..(offset + lampLedDataLength)];
                    offset += lampLedDataLength;
                }

                reply.Signs[signStatus.SignID] = signStatus;
            }

            // Last 2 bytes: CRC (if present)
            if (offset + 4 <= applicationData.Length)
            {
                byte crcLow = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                byte crcHigh = Convert.ToByte(applicationData[(offset + 2)..(offset + 4)], 16);
                reply.CRC = (ushort)(crcLow + (crcHigh << 8));
            }

            _signExtendedStatusReplyTaskCompletion?.TrySetResult(reply);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process sign extended status reply: " + ex.Message);
        }

        return Task.CompletedTask;
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

    /// <summary>
    /// Process the sign set high resolution graphics frame response
    /// </summary>
    /// <param name="applicationData">The application data from the response</param>
    /// <returns></returns>
    public Task ProcessSignSetHighResolutionGraphicsFrame(string applicationData)
    {
        try
        {
            SignSetHighResolutionGraphicsFrame signSetHighResGraphicsFrameFeedback = new SignSetHighResolutionGraphicsFrame();

            // Parse the application data according to the protocol:
            // Position 1: MI Code (1D) - already parsed by dispatcher
            // Position 2: Frame ID (1 byte)
            // Position 3: Revision (1 byte)
            // Position 4-5: Number of rows (2 bytes, WORD)
            // Position 6-7: Number of columns (2 bytes, WORD)
            // Position 8: Colour (1 byte)
            // Position 9: Conspicuity (1 byte)
            // Position 10-13: Graphics length (4 bytes, DWORD)
            // Position 14+: Graphics data (variable)
            // Last 4 chars: CRC (2 bytes)

            signSetHighResGraphicsFrameFeedback.FrameID = Convert.ToByte(applicationData[2..4], 16);
            signSetHighResGraphicsFrameFeedback.Revision = Convert.ToByte(applicationData[4..6], 16);
            signSetHighResGraphicsFrameFeedback.NumberOfRows = Convert.ToUInt16(applicationData[6..10], 16);
            signSetHighResGraphicsFrameFeedback.NumberOfColumns = Convert.ToUInt16(applicationData[10..14], 16);
            signSetHighResGraphicsFrameFeedback.Colour = Convert.ToByte(applicationData[14..16], 16);
            signSetHighResGraphicsFrameFeedback.Conspicuity = Convert.ToByte(applicationData[16..18], 16);
            signSetHighResGraphicsFrameFeedback.GraphicsLength = Convert.ToUInt32(applicationData[18..26], 16);

            // Graphics data starts at position 26 and goes for GraphicsLength * 2 hex characters
            int graphicsDataLength = (int)(signSetHighResGraphicsFrameFeedback.GraphicsLength * 2);
            signSetHighResGraphicsFrameFeedback.GraphicsData = applicationData[26..(26 + graphicsDataLength)];

            // CRC is the last 4 hex characters after the graphics data
            signSetHighResGraphicsFrameFeedback.CRC = Convert.ToUInt16(applicationData[(26 + graphicsDataLength)..(26 + graphicsDataLength + 4)], 16);

            _signSetHighResGraphicsFrameTaskCompletion?.TrySetResult(signSetHighResGraphicsFrameFeedback);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process sign set high resolution graphics frame: " + ex.Message);
        }

        return Task.CompletedTask;
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

    /// <summary>
    /// Process the sign set plan response
    /// </summary>
    /// <param name="applicationData">The application data from the response</param>
    /// <returns></returns>
    public Task ProcessSignSetPlan(string applicationData)
    {
        try
        {
            SignSetPlan signSetPlanFeedback = new SignSetPlan();

            // Parse the application data according to the protocol:
            // Position 1: MI Code (0D) - already parsed by dispatcher
            // Position 2: Plan ID (1 byte)
            // Position 3: Revision (1 byte)
            // Position 4: Day of week (1 byte)
            // Position 5+: Entries (6 bytes each: type, id, start hour, start min, stop hour, stop min)

            signSetPlanFeedback.PlanID = Convert.ToByte(applicationData[2..4], 16);
            signSetPlanFeedback.Revision = Convert.ToByte(applicationData[4..6], 16);
            signSetPlanFeedback.DayOfWeek = Convert.ToByte(applicationData[6..8], 16);

            // Parse entries - each entry is 6 bytes (12 hex characters)
            int index = 8;
            while (index + 2 <= applicationData.Length)
            {
                byte frameMessageType = Convert.ToByte(applicationData[index..(index + 2)], 16);

                // Type 0 indicates end of plan
                if (frameMessageType == 0)
                    break;

                // Make sure we have enough data for a complete entry
                if (index + 12 > applicationData.Length)
                    break;

                var entry = new SignSetPlanEntry
                {
                    FrameMessageType = frameMessageType,
                    FrameMessageID = Convert.ToByte(applicationData[(index + 2)..(index + 4)], 16),
                    StartHour = Convert.ToByte(applicationData[(index + 4)..(index + 6)], 16),
                    StartMinute = Convert.ToByte(applicationData[(index + 6)..(index + 8)], 16),
                    StopHour = Convert.ToByte(applicationData[(index + 8)..(index + 10)], 16),
                    StopMinute = Convert.ToByte(applicationData[(index + 10)..(index + 12)], 16)
                };

                signSetPlanFeedback.Entries.Add(entry);
                index += 12;

                // Maximum 6 entries
                if (signSetPlanFeedback.Entries.Count >= 6)
                    break;
            }

            _signSetPlanTaskCompletion?.TrySetResult(signSetPlanFeedback);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to process sign set plan: " + ex.Message);
        }

        return Task.CompletedTask;
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
    public Task<SignController?> GetControllerConfigurationAsync()
    {
        try
        {
            // To reimplement
            return Task.FromResult<SignController?>(null);
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
    public Task<SignStatusReply?> GetStatus()
    {
        return Task.FromResult(_signController);
    }

    /// <summary>
    /// Extended request message
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ExtendedRequestMessage(ExtendedRequestMessageDto request)
    {
        bool result = false;
        byte currentId = 80;
        try
        {
            SignSetMessage signSetMessage = new SignSetMessage
            {
                MessageID = currentId
            };

            if (request.Frame1 != null)
            {
                signSetMessage.Frame1Time = (byte)(request.Frame1Time * 10);

                SignSetTextFrame signSetTextFrame1 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame1.Font,
                    Colour = request.Frame1.Colour,
                    Conspicuity = request.Frame1.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame1.Text.Length,
                    Text = request.Frame1.Text
                };

                signSetMessage.Frame1ID = signSetTextFrame1.FrameID;

                await SignSetTextFrame(signSetTextFrame1);
            }

            if (request.Frame2 != null)
            {
                signSetMessage.Frame2Time = (byte)(request.Frame2Time * 10);

                SignSetTextFrame signSetTextFrame2 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame2.Font,
                    Colour = request.Frame2.Colour,
                    Conspicuity = request.Frame2.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame2.Text.Length,
                    Text = request.Frame2.Text
                };

                signSetMessage.Frame2ID = signSetTextFrame2.FrameID;

                await SignSetTextFrame(signSetTextFrame2);
            }

            if (request.Frame3 != null)
            {
                signSetMessage.Frame3Time = (byte)(request.Frame3Time * 10);

                SignSetTextFrame signSetTextFrame3 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame3.Font,
                    Colour = request.Frame3.Colour,
                    Conspicuity = request.Frame3.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame3.Text.Length,
                    Text = request.Frame3.Text
                };

                signSetMessage.Frame3ID = signSetTextFrame3.FrameID;

                await SignSetTextFrame(signSetTextFrame3);
            }

            if (request.Frame4 != null)
            {
                signSetMessage.Frame4Time = (byte)(request.Frame4Time * 10);

                SignSetTextFrame signSetTextFrame4 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame4.Font,
                    Colour = request.Frame4.Colour,
                    Conspicuity = request.Frame4.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame4.Text.Length,
                    Text = request.Frame4.Text
                };

                signSetMessage.Frame4ID = signSetTextFrame4.FrameID;

                await SignSetTextFrame(signSetTextFrame4);
            }

            if (request.Frame5 != null)
            {
                signSetMessage.Frame5Time = (byte)(request.Frame5Time * 10);

                SignSetTextFrame signSetTextFrame5 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame5.Font,
                    Colour = request.Frame5.Colour,
                    Conspicuity = request.Frame5.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame5.Text.Length,
                    Text = request.Frame5.Text
                };

                signSetMessage.Frame5ID = signSetTextFrame5.FrameID;

                await SignSetTextFrame(signSetTextFrame5);
            }

            if (request.Frame6 != null)
            {
                signSetMessage.Frame6Time = (byte)(request.Frame6Time * 10);

                SignSetTextFrame signSetTextFrame6 = new SignSetTextFrame
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = request.Frame6.Font,
                    Colour = request.Frame6.Colour,
                    Conspicuity = request.Frame6.Conspicuity,
                    NumberOfCharsInText = (byte)request.Frame6.Text.Length,
                    Text = request.Frame6.Text
                };

                signSetMessage.Frame6ID = signSetTextFrame6.FrameID;

                await SignSetTextFrame(signSetTextFrame6);
            }

            await SignSetMessage(signSetMessage);

            await SignDisplayMessage(new SignDisplayMessage
            {
                // TODO: parametrize
                GroupID = 1,
                MessageID = signSetMessage.MessageID
            });

            result = true;
        }

        catch (System.Exception ex)
        {
            Console.WriteLine("Failed to process extended request message: " + ex.Message);
        }
        return result;
    }
}
