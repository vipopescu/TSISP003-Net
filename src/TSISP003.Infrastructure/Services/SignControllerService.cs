using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using System.Net.Sockets;
using System.Text;
using TSISP003.Infrastructure.Configuration;
using TSISP003.Infrastructure.Protocol;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Enums;
using TSISP003.Domain.Exceptions;
using TSISP003.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TSISP003.Infrastructure.Services;

public class SignControllerService(
    ITcpClient tcpClient,
    SignControllerConnectionOptions deviceSettings,
    ILogger<SignControllerService> logger) : ISignControllerService, IHostedService, IDisposable
{
    private readonly ILogger<SignControllerService> _logger = logger;
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
    private TaskCompletionSource<HARStatusReply>? _harStatusReplyTaskCompletion;
    private TaskCompletionSource<HARSetStrategy>? _harSetStrategyTaskCompletion;
    private TaskCompletionSource<HARSetPlan>? _harSetPlanTaskCompletion;
    private TaskCompletionSource<SignController>? _signConfigurationReplyTaskCompletion;

    private Task? heartBeatPollTask;
    private Task? startSessionTask;
    private readonly ITcpClient _tcpClient = tcpClient;
    private readonly SignControllerConnectionOptions _deviceSettings = deviceSettings;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private SignStatusReply? _signController;

    /// <summary>
    /// Stores the sign controller configuration received from the device.
    /// Contains group and sign information.
    /// </summary>
    private SignController? _signConfiguration;

    // Rolling ID management for ExtendedRequestMessage (cycles MinId to MaxId)
    private int _currentExtendedRequestId = MinExtendedRequestId;
    private const int MinExtendedRequestId = 80;
    private const int MaxExtendedRequestId = 254;
    private readonly object _idLock = new();

    private byte GetNextExtendedRequestId()
    {
        lock (_idLock)
        {
            var id = _currentExtendedRequestId;
            _currentExtendedRequestId = _currentExtendedRequestId >= MaxExtendedRequestId
                ? MinExtendedRequestId
                : _currentExtendedRequestId + 1;
            return (byte)id;
        }
    }

    /// <summary>
    /// Flag to indicate whether the session was manually stopped via EndSession().
    /// When true, prevents automatic session restart after heartbeat failures.
    /// </summary>
    private volatile bool _sessionManuallyStopped = false;

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

                foreach (var packet in ProtocolHelper.GetChunks(response, out _))
                {
                    // Iterate over both messages, we need to receive ACK and password seed response
                    if (packet[0] == ProtocolConstants.ACK || packet[0] == ProtocolConstants.NAK)
                        isAcknowledged = packet[0] == ProtocolConstants.ACK;
                    else if (packet[0] == ProtocolConstants.SOH
                                && Convert.ToInt32(packet[8..10], 16) == ProtocolConstants.MI_PASSWORD_SEED)
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

                foreach (var packet in ProtocolHelper.GetChunks(response, out _))
                {
                    // Iterate over both messages, we need to receive ACK and ACK from the protocol
                    if (packet[0] == ProtocolConstants.ACK || packet[0] == ProtocolConstants.NAK)
                        isAcknowledged = packet[0] == ProtocolConstants.ACK;
                    else if (packet[8..10] == ProtocolConstants.MI_ACK_MESSAGE.ToString("X2"))
                        isAckProtocolReceived = true;
                }

                // 5 - If successful, get out
                sessionStarted = isAcknowledged && isAckProtocolReceived;

                if (sessionStarted)
                {
                    _logger.LogInformation("Session started successfully");
                    break;
                }
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Starting session failed: {Message}", ex.Message);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Starting session failed: {Message}", ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Starting session failed: {Message}", ex.Message);
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
            try
            {
                _signConfigurationReplyTaskCompletion = new TaskCompletionSource<SignController>(TaskCreationOptions.RunContinuationsAsynchronously);

                await SignConfigurationRequest();
                await Task.Delay(1000, cancellationToken);
                await ReadStream();

                // Wait for the configuration reply with timeout
                var delayTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                var completedTask = await Task.WhenAny(_signConfigurationReplyTaskCompletion.Task, delayTask);

                if (completedTask == _signConfigurationReplyTaskCompletion.Task)
                {
                    _signConfiguration = await _signConfigurationReplyTaskCompletion.Task;
                    SignConfigurationReceived = true;
                    _logger.LogInformation("Sign configuration received: {GroupCount} groups", _signConfiguration.NumberOfGroups);
                }
                else
                {
                    _logger.LogWarning("Sign configuration request timed out, retrying...");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sign configuration: {Message}", ex.Message);
                await Task.Delay(3000, cancellationToken);
            }
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
                _logger.LogWarning(ex, "Failed to read from the socket: {Message}", ex.Message);
                failedAttempts++;
            }
            finally
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        // If we reach the max attempts, restart the session only if not manually stopped
        if (!cancellationToken.IsCancellationRequested && failedAttempts >= maxAttempts)
        {
            if (_sessionManuallyStopped)
            {
                _logger.LogInformation("Session was manually stopped. Not restarting automatically.");
            }
            else
            {
                _logger.LogWarning("Heartbeat failed. Restarting the session...");
                startSessionTask = Task.Run(() => StartSessionTask(_cancellationTokenSource.Token));
            }
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
        var chunks = ProtocolHelper.GetChunks(data, out remaining);

        // Process each complete chunk.
        foreach (var packet in chunks)
        {
            // Determine the packet type by its starting character.
            if (packet[0] == ProtocolConstants.ACK || packet[0] == ProtocolConstants.NAK)
                ProcessNonDataPacket(packet);
            else if (packet[0] == ProtocolConstants.SOH)
                DispatchDataPacket(packet);
            else
                _logger.LogWarning("Unable to determine type of the packet.");
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
        if (packet[0] == ProtocolConstants.ACK)
        {
            // Slave's ACK contains N(R) which indicates how many valid data packets
            // the slave has received from us. This confirms our last packet was received.
            int slaveNR = int.Parse(packet[1..3], System.Globalization.NumberStyles.HexNumber);

            // Increment our send sequence number for the next packet we send
            NS = IncrementSequenceNumber(NS);
        }
        else if (packet[0] == ProtocolConstants.NAK)
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

        if (miCode == ProtocolConstants.MI_SIGN_STATUS_REPLY)
            await ProcessSignStatusReply(applicationData);
        else if (miCode == ProtocolConstants.MI_PASSWORD_SEED)
            await ProcessPasswordSeed(applicationData);
        else if (miCode == ProtocolConstants.MI_HAR_STATUS_REPLY)
            await ProcessHARStatusReply(applicationData);
        else if (miCode == ProtocolConstants.MI_HAR_SET_STRATEGY)
            await ProcessHARSetStrategy(applicationData);
        else if (miCode == ProtocolConstants.MI_HAR_SET_PLAN)
            await ProcessHARSetPlan(applicationData);
        else if (miCode == ProtocolConstants.MI_HAR_SET_VOICE_DATA_ACK)
            await ProcessHARVoiceDataAck(applicationData);
        else if (miCode == ProtocolConstants.MI_HAR_SET_VOICE_DATA_NAK)
            await ProcessHARVoiceDataNak(applicationData);
        else if (miCode == ProtocolConstants.MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY)
            await ProcessEnvironmentalWeatherStatusReply(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_CONFIGURATION_REPLY)
            await ProcessSignConfigurationReply(applicationData);
        else if (miCode == ProtocolConstants.MI_REPORT_ENABLED_PLANS)
            await ProcessReportEnabledPlans(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_EXTENDED_STATUS_REPLY)
            await ProcessSignExtendedStatusReply(applicationData);
        else if (miCode == ProtocolConstants.MI_FAULT_LOG_REPLY)
            await ProcessFaultLogReply(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_SET_TEXT_FRAME)
            await ProcessSignSetTextFrame(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_SET_GRAPHIC_FRAME)
            await ProcessSignSetGraphicsFrame(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME)
            await ProcessSignSetHighResolutionGraphicsFrame(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_SET_MESSAGE)
            await ProcessSignSetMessage(applicationData);
        else if (miCode == ProtocolConstants.MI_SIGN_SET_PLAN)
            await ProcessSignSetPlan(applicationData);
        else if (miCode == ProtocolConstants.MI_REJECT_MESSAGE)
            await ProcessRejectMessage(applicationData);
        else if (miCode == ProtocolConstants.MI_ACK_MESSAGE)
            await ProcessAckMessage(applicationData);
        else
            _logger.LogWarning("Unexpected MI code: {MiCode}", miCode);

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_HEARTBEAT_POLL.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// Start a session. This resets the manually stopped flag,
    /// allowing automatic restart after heartbeat failures.
    /// </summary>
    /// <returns></returns>
    public async Task StartSession()
    {
        _logger.LogDebug("Starting session for device at address {Address}", _deviceSettings.Address);

        // Reset the manually stopped flag since we're starting a new session
        _sessionManuallyStopped = false;

        // build body
        string message = ProtocolConstants.SOH // Start of header
                    + "00" + "00" // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_START_SESSION.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + "00" + "00" // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_PASSWORD.ToString("X2")
                    + ProtocolHelper.GeneratePassword(passwordSeed, _deviceSettings.SeedOffset, _deviceSettings.PasswordOffset)[^4..];

        // append crc and end of message
        string crc = ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message));
        message = message + crc
                    + ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// End the current session. This marks the session as manually stopped,
    /// preventing automatic restart after heartbeat failures.
    /// </summary>
    /// <returns></returns>
    public async Task EndSession()
    {
        _logger.LogInformation("Ending session for device at address {Address}", _deviceSettings.Address);

        // Mark session as manually stopped to prevent automatic restart
        _sessionManuallyStopped = true;

        // build body
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_END_SESSION.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        _logger.LogInformation("Sending SystemReset command - GroupId: {GroupId}, ResetLevel: {ResetLevel}", groupId, resetLevel);

        // Validate reset level
        if (!IsValidResetLevel(resetLevel))
        {
            throw new ArgumentException($"Invalid reset level: {resetLevel}. Valid values are 0, 1, 2, 3, or 255.", nameof(resetLevel));
        }

        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SYSTEM_RESET.ToString("X2")
                    + groupId.ToString("X2")
                    + resetLevel.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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

    /// <summary>
    /// Send the UPDATE TIME command to update the real time clock in the device controller.
    /// </summary>
    /// <param name="dateTime">The date/time to set. If null, uses the current system time.</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> UpdateTime(DateTime? dateTime = null)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Use provided time or current system time
        DateTime time = dateTime ?? DateTime.Now;

        // Build the time data:
        // Day (1 byte), Month (1 byte), Year (2 bytes WORD), Hours (1 byte), Minutes (1 byte), Seconds (1 byte)
        byte day = (byte)time.Day;
        byte month = (byte)time.Month;
        ushort year = (ushort)time.Year;
        byte hours = (byte)time.Hour;
        byte minutes = (byte)time.Minute;
        byte seconds = (byte)time.Second;

        // Year is transmitted as WORD (2 bytes, low byte first based on protocol convention)
        byte yearLow = (byte)(year & 0xFF);
        byte yearHigh = (byte)((year >> 8) & 0xFF);

        // build body - MI Code 0x09 (Update Time)
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_UPDATE_TIME.ToString("X2")
                    + day.ToString("X2")
                    + month.ToString("X2")
                    + yearLow.ToString("X2") + yearHigh.ToString("X2")
                    + hours.ToString("X2")
                    + minutes.ToString("X2")
                    + seconds.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Update Time request timed out.");
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

    public async Task<SignStatusReply> SignSetTextFrame(SignSetTextFrame request)
    {
        _signStatusReplyTaskCompletion = new TaskCompletionSource<SignStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        string applicationMessage = ProtocolConstants.MI_SIGN_SET_TEXT_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2") + request.Revision.ToString("X2")
                   + request.Font.ToString("X2") + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2") + request.NumberOfCharsInText.ToString("X2")
                   + request.Text;

        applicationMessage = applicationMessage + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(ProtocolHelper.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        // Build application message:
        // MI Code (0B) + FrameID + Revision + NumberOfRows + NumberOfColumns + Colour + Conspicuity + GraphicsLength (2 bytes) + GraphicsData
        string applicationMessage = ProtocolConstants.MI_SIGN_SET_GRAPHIC_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2")
                   + request.Revision.ToString("X2")
                   + request.NumberOfRows.ToString("X2")
                   + request.NumberOfColumns.ToString("X2")
                   + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2")
                   + request.GraphicsLength.ToString("X4") // 2 bytes (WORD) for length
                   + request.GraphicsData;

        // Calculate application message CRC
        applicationMessage = applicationMessage + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(ProtocolHelper.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append packet crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
            _logger.LogError(ex, "Failed to process sign set graphics frame: {Message}", ex.Message);
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
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        // Build application message:
        // MI Code (1D) + FrameID + Revision + NumberOfRows (2 bytes) + NumberOfColumns (2 bytes) + Colour + Conspicuity + GraphicsLength (4 bytes) + GraphicsData
        string applicationMessage = ProtocolConstants.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME.ToString("X2")
                   + request.FrameID.ToString("X2")
                   + request.Revision.ToString("X2")
                   + request.NumberOfRows.ToString("X4") // 2 bytes (WORD) for rows
                   + request.NumberOfColumns.ToString("X4") // 2 bytes (WORD) for columns
                   + request.Colour.ToString("X2")
                   + request.Conspicuity.ToString("X2")
                   + request.GraphicsLength.ToString("X8") // 4 bytes (DWORD) for length
                   + request.GraphicsData;

        // Calculate application message CRC
        applicationMessage = applicationMessage + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(ProtocolHelper.HexToAscii(applicationMessage)));

        var message = header + applicationMessage;

        // append packet crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SIGN_CONFIGURATION_REQUEST.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        string message = header + ProtocolConstants.MI_SIGN_DISPLAY_ATOMIC_FRAMES.ToString("X2")
            + request.GroupID.ToString("X2") + request.NumbeOfSigns.ToString("X2");

        foreach (var frame in request.Frames)
        {
            message += frame.SignID.ToString("X2") + frame.FrameID.ToString("X2");
        }

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        string message = header + ProtocolConstants.MI_SIGN_SET_MESSAGE.ToString("X2")
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
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        // Build application message:
        // MI Code (0D) + PlanID + Revision + DayOfWeek + [FrameMessageType + FrameMessageID + StartHour + StartMinute + StopHour + StopMinute]...
        string applicationMessage = ProtocolConstants.MI_SIGN_SET_PLAN.ToString("X2")
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
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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

    /// <summary>
    /// Send the SIGN DISPLAY FRAME command to display a pre-stored frame on all signs in a group.
    /// Note: The SignID field in the request is used as the GroupID per the protocol specification.
    /// </summary>
    /// <param name="request">The request containing GroupID (as SignID) and FrameID</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> SignDisplayFrame(SignDisplayFrame request)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body - MI Code 0x0E (Sign Display Frame)
        // Note: SignID is used as GroupID per protocol spec (displays same frame on all signs in group)
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SIGN_DISPLAY_FRAME.ToString("X2")
                    + request.SignID.ToString("X2")  // GroupID in protocol
                    + request.FrameID.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for either the reply to be received or a timeout after 3 seconds.
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("Sign Display Frame request timed out.");
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

    public async Task<AckReply> SignDisplayMessage(SignDisplayMessage request)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string header = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX;

        string message = header + ProtocolConstants.MI_SIGN_DISPLAY_MESSAGE.ToString("X2")
                   + request.GroupID.ToString("X2") + request.MessageID.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_ENABLE_PLAN.ToString("X2")
                    + groupId.ToString("X2")
                    + planId.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_DISABLE_PLAN.ToString("X2")
                    + groupId.ToString("X2")
                    + planId.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_REQUEST_ENABLED_PLANS.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SIGN_SET_DIMMING_LEVEL.ToString("X2")
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
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_POWER_ON_OFF.ToString("X2")
                    + 1.ToString("X2") + groupId.ToString("X2") + (powerOn ? 1 : 0).ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_DISABLE_ENABLE_DEVICE.ToString("X2")
                    + entries.Count.ToString("X2"); // number of entries

        // Add each entry (groupId + enabled flag)
        foreach (var entry in entries)
        {
            message += entry.groupId.ToString("X2") + (entry.enabled ? 1 : 0).ToString("X2");
        }

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
    public async Task<ISignResponse> SignRequestStoredFrameMessagePlan(RequestType requestType, byte requestID)
    {
        _signSetTextFrameTaskCompletion = new TaskCompletionSource<SignSetTextFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetGraphicsFrameTaskCompletion = new TaskCompletionSource<SignSetGraphicsFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetHighResGraphicsFrameTaskCompletion = new TaskCompletionSource<SignSetHighResolutionGraphicsFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetPlanTaskCompletion = new TaskCompletionSource<SignSetPlan>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _signSetMessageTaskCompletion = new TaskCompletionSource<SignSetMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        // build body
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN.ToString("X2")
                    + ((byte)requestType).ToString("X2") + requestID.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_SIGN_EXTENDED_STATUS_REQUEST.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_RETRIEVE_FAULT_LOG.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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
        string message = ProtocolConstants.SOH // Start of header
                    + NS.ToString("X2") + NR.ToString("X2") // NS and NR
                    + _deviceSettings.Address // ADDR
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_RESET_FAULT_LOG.ToString("X2");

        // append crc and end of message
        message = message
                    + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message))
                    + ProtocolConstants.ETX;

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

    /// <summary>
    /// Send HAR Set Strategy command to store a voice strategy in the HAR controller's memory
    /// </summary>
    /// <param name="request">The strategy to store</param>
    /// <returns>HARStatusReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<HARStatusReply> HARSetStrategy(HARSetStrategy request)
    {
        _harStatusReplyTaskCompletion = new TaskCompletionSource<HARStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Build message:
        // MI Code (43h) + StrategyID (WORD) + Revision + Number of VoiceIDs + VoiceIDs
        string message = ProtocolConstants.SOH
                    + NS.ToString("X2") + NR.ToString("X2")
                    + _deviceSettings.Address
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_HAR_SET_STRATEGY.ToString("X2");

        // Strategy ID (WORD - low byte first)
        message += (request.StrategyID & 0xFF).ToString("X2");
        message += ((request.StrategyID >> 8) & 0xFF).ToString("X2");

        // Revision
        message += request.Revision.ToString("X2");

        // Number of Voice IDs
        message += ((byte)request.VoiceIDs.Count).ToString("X2");

        // Voice IDs (WORD each - low byte first)
        foreach (var voiceId in request.VoiceIDs)
        {
            message += (voiceId & 0xFF).ToString("X2");
            message += ((voiceId >> 8) & 0xFF).ToString("X2");
        }

        // Append CRC and ETX
        message += ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message));
        message += ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for HAR Status Reply or timeout
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_harStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("HAR Set Strategy request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        return await _harStatusReplyTaskCompletion.Task;
    }

    /// <summary>
    /// Send HAR Activate Strategy command to activate a voice strategy
    /// </summary>
    /// <param name="strategyId">Strategy ID to activate (0 stops the current strategy)</param>
    /// <returns>AckReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<AckReply> HARActivateStrategy(ushort strategyId)
    {
        _ackReplyCompletion = new TaskCompletionSource<AckReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Build message:
        // MI Code (44h) + StrategyID (WORD)
        string message = ProtocolConstants.SOH
                    + NS.ToString("X2") + NR.ToString("X2")
                    + _deviceSettings.Address
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_HAR_ACTIVATE_STRATEGY.ToString("X2");

        // Strategy ID (WORD - low byte first)
        message += (strategyId & 0xFF).ToString("X2");
        message += ((strategyId >> 8) & 0xFF).ToString("X2");

        // Append CRC and ETX
        message += ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message));
        message += ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for ACK or timeout
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_ackReplyCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("HAR Activate Strategy request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        return await _ackReplyCompletion.Task;
    }

    /// <summary>
    /// Send HAR Set Plan command to store a plan in the HAR controller's memory
    /// </summary>
    /// <param name="request">The plan to store</param>
    /// <returns>HARStatusReply on success</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<HARStatusReply> HARSetPlan(HARSetPlan request)
    {
        _harStatusReplyTaskCompletion = new TaskCompletionSource<HARStatusReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Build message:
        // MI Code (45h) + PlanID + Revision + DayOfWeek + StrategyEntries
        string message = ProtocolConstants.SOH
                    + NS.ToString("X2") + NR.ToString("X2")
                    + _deviceSettings.Address
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_HAR_SET_PLAN.ToString("X2");

        // Plan ID and Revision
        message += request.PlanID.ToString("X2");
        message += request.Revision.ToString("X2");
        message += request.DayOfWeek.ToString("X2");

        // Strategy entries (up to 6)
        foreach (var entry in request.Entries)
        {
            // Strategy ID (WORD - low byte first)
            message += (entry.StrategyID & 0xFF).ToString("X2");
            message += ((entry.StrategyID >> 8) & 0xFF).ToString("X2");
            message += entry.StartHour.ToString("X2");
            message += entry.StartMinute.ToString("X2");
            message += entry.StopHour.ToString("X2");
            message += entry.StopMinute.ToString("X2");
        }

        // Append CRC and ETX
        message += ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message));
        message += ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for HAR Status Reply or timeout
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(_harStatusReplyTaskCompletion.Task, _rejectReplyCompletion.Task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("HAR Set Plan request timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        return await _harStatusReplyTaskCompletion.Task;
    }

    /// <summary>
    /// Send HAR Request Stored Voice/Strategy/Plan command
    /// </summary>
    /// <param name="requestType">Type: 0 = Voice, 1 = Strategy, 2 = Plan</param>
    /// <param name="requestId">Voice/Strategy/Plan ID</param>
    /// <param name="sequenceNumber">Sequence number (only used for Voice requests)</param>
    /// <returns>The requested Voice Data, Strategy, or Plan</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    /// <exception cref="SignRequestRejectedException">Thrown when the request is rejected</exception>
    public async Task<ISignResponse> HARRequestStoredVoiceStrategyPlan(byte requestType, ushort requestId, byte sequenceNumber)
    {
        _harSetStrategyTaskCompletion = new TaskCompletionSource<HARSetStrategy>(TaskCreationOptions.RunContinuationsAsynchronously);
        _harSetPlanTaskCompletion = new TaskCompletionSource<HARSetPlan>(TaskCreationOptions.RunContinuationsAsynchronously);
        _rejectReplyCompletion = new TaskCompletionSource<RejectReply>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Build message:
        // MI Code (46h) + Type + ID (WORD) + SequenceNumber
        string message = ProtocolConstants.SOH
                    + NS.ToString("X2") + NR.ToString("X2")
                    + _deviceSettings.Address
                    + ProtocolConstants.STX
                    + ProtocolConstants.MI_HAR_REQUEST_STORED_VOICE_STRATEGY_PLAN.ToString("X2");

        // Request type
        message += requestType.ToString("X2");

        // ID (WORD - low byte first)
        message += (requestId & 0xFF).ToString("X2");
        message += ((requestId >> 8) & 0xFF).ToString("X2");

        // Sequence number (0 for Strategy/Plan requests)
        message += sequenceNumber.ToString("X2");

        // Append CRC and ETX
        message += ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(message));
        message += ProtocolConstants.ETX;

        await _tcpClient.SendAsync(message);

        // Wait for the appropriate reply or timeout
        var delayTask = Task.Delay(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(
            _harSetStrategyTaskCompletion.Task,
            _harSetPlanTaskCompletion.Task,
            _rejectReplyCompletion.Task,
            delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException("HAR Request Stored Voice/Strategy/Plan timed out.");
        }

        if (completedTask == _rejectReplyCompletion.Task)
        {
            throw new SignRequestRejectedException(await _rejectReplyCompletion.Task);
        }

        if (completedTask == _harSetStrategyTaskCompletion.Task)
        {
            return await _harSetStrategyTaskCompletion.Task;
        }

        if (completedTask == _harSetPlanTaskCompletion.Task)
        {
            return await _harSetPlanTaskCompletion.Task;
        }

        throw new InvalidOperationException("Unexpected execution path.");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process sign status reply");
        }
        finally
        {
            if (_signController is not null)
                _signStatusReplyTaskCompletion?.TrySetResult(_signController);
        }
    }

    /// <summary>
    /// Process HAR Status Reply (MI Code 0x40)
    /// Contains HAR controller status: online status, date/time, error codes,
    /// HAR enabled/disabled, voice ID playing, strategy ID active, etc.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessHARStatusReply(string applicationData)
    {
        try
        {
            // HAR Status Reply format (21 bytes):
            // Position 1: MI Code (40h)
            // Position 2: Online status (0/1)
            // Position 3: Application error code
            // Position 4: Day (1-31)
            // Position 5: Month (1-12)
            // Position 6-7: Year (WORD)
            // Position 8: Hours (0-23)
            // Position 9: Minutes (0-59)
            // Position 10: Seconds (0-59)
            // Position 11-12: Controller hardware checksum (WORD)
            // Position 13: Controller error code
            // Position 14: HAR disabled/enabled (0/1)
            // Position 15-16: Voice ID playing (WORD, 0 if none)
            // Position 17: Voice revision
            // Position 18-19: Strategy ID active (WORD, 0 if none)
            // Position 20: Strategy revision
            // Position 21: Strategy status

            var harStatusReply = new HARStatusReply
            {
                OnlineStatus = Convert.ToByte(applicationData[2..4], 16) == 1,
                ApplicationErrorCode = Convert.ToByte(applicationData[4..6], 16),
                Day = Convert.ToByte(applicationData[6..8], 16),
                Month = Convert.ToByte(applicationData[8..10], 16)
            };

            // Year is WORD (2 bytes, low byte first)
            byte yearLow = Convert.ToByte(applicationData[10..12], 16);
            byte yearHigh = Convert.ToByte(applicationData[12..14], 16);
            harStatusReply.Year = (ushort)(yearLow + (yearHigh << 8));

            harStatusReply.Hour = Convert.ToByte(applicationData[14..16], 16);
            harStatusReply.Minute = Convert.ToByte(applicationData[16..18], 16);
            harStatusReply.Second = Convert.ToByte(applicationData[18..20], 16);

            // Controller hardware checksum (WORD)
            byte checksumLow = Convert.ToByte(applicationData[20..22], 16);
            byte checksumHigh = Convert.ToByte(applicationData[22..24], 16);
            harStatusReply.ControllerChecksum = (ushort)(checksumLow + (checksumHigh << 8));

            harStatusReply.ControllerErrorCode = Convert.ToByte(applicationData[24..26], 16);
            harStatusReply.HAREnabled = Convert.ToByte(applicationData[26..28], 16) == 1;

            // Voice ID playing (WORD)
            byte voiceIdLow = Convert.ToByte(applicationData[28..30], 16);
            byte voiceIdHigh = Convert.ToByte(applicationData[30..32], 16);
            harStatusReply.VoiceIDPlaying = (ushort)(voiceIdLow + (voiceIdHigh << 8));

            harStatusReply.VoiceRevision = Convert.ToByte(applicationData[32..34], 16);

            // Strategy ID active (WORD)
            byte strategyIdLow = Convert.ToByte(applicationData[34..36], 16);
            byte strategyIdHigh = Convert.ToByte(applicationData[36..38], 16);
            harStatusReply.StrategyIDActive = (ushort)(strategyIdLow + (strategyIdHigh << 8));

            harStatusReply.StrategyRevision = Convert.ToByte(applicationData[38..40], 16);
            harStatusReply.StrategyStatus = Convert.ToByte(applicationData[40..42], 16);

            _logger.LogDebug("HAR Status Reply received - Online: {Online}, Enabled: {Enabled}, AppError: {AppError}, CtrlError: {CtrlError}",
                harStatusReply.OnlineStatus, harStatusReply.HAREnabled, harStatusReply.ApplicationErrorCode, harStatusReply.ControllerErrorCode);

            _harStatusReplyTaskCompletion?.TrySetResult(harStatusReply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HAR Status Reply: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process Environmental/Weather Status Reply (MI Code 0x80)
    /// Contains environmental/weather station status: online status, date/time,
    /// error codes, sensor presence flags, etc.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessEnvironmentalWeatherStatusReply(string applicationData)
    {
        try
        {
            // Environmental/Weather Status Reply format (30 bytes):
            // Position 1: MI Code (80h)
            // Position 2: Online status (0/1)
            // Position 3: Application error code
            // Position 4: Day (1-31)
            // Position 5: Month (1-12)
            // Position 6-7: Year (WORD)
            // Position 8: Hours (0-23)
            // Position 9: Minutes (0-59)
            // Position 10: Seconds (0-59)
            // Position 11-12: Controller hardware checksum (WORD)
            // Position 13: Controller error code
            // Position 14: Supports thresholds flag (0/1)
            // Position 15: Current event log sequence number
            // Position 16-30: Sensor presence flags (pressure, temp, visibility, etc.)

            bool onlineStatus = Convert.ToByte(applicationData[2..4], 16) == 1;
            byte appErrorCode = Convert.ToByte(applicationData[4..6], 16);
            byte controllerErrorCode = Convert.ToByte(applicationData[24..26], 16);
            bool supportsThresholds = Convert.ToByte(applicationData[26..28], 16) == 1;

            _logger.LogDebug("Environmental/Weather Status Reply received - Online: {Online}, SupportsThresholds: {SupportsThresholds}, AppError: {AppError}, CtrlError: {CtrlError}",
                onlineStatus, supportsThresholds, appErrorCode, controllerErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Environmental/Weather Status Reply: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process the sign configuration reply (MI Code 0x22)
    /// Format:
    /// - Position 1: MI Code (22h)
    /// - Position 2-11: Manufacturer code details (10 bytes)
    /// - Position 12: Number of groups
    /// - For each group:
    ///   - Group ID (1 byte)
    ///   - Number of signs (1 byte)
    ///   - For each sign: Sign ID (1), Type (1), Width (2 WORD), Height (2 WORD)
    ///   - Number of signature data bytes (1 byte)
    ///   - Signature bytes (variable)
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    /// <returns></returns>
    public Task ProcessSignConfigurationReply(string applicationData)
    {
        try
        {
            var signConfiguration = new SignController();

            // Position 2-11: Manufacturer code details (10 bytes = 20 hex chars)
            // Offset 2-22 in the hex string (position 1 is MI code at offset 0-2)
            // We skip this for now as SignController doesn't have a field for it

            // Position 12: Number of groups (offset 22-24)
            byte numberOfGroups = Convert.ToByte(applicationData[22..24], 16);
            signConfiguration.NumberOfGroups = numberOfGroups;

            // Parse each group starting at position 13 (offset 24)
            int offset = 24;
            for (byte groupIndex = 0; groupIndex < numberOfGroups; groupIndex++)
            {
                // Group ID (1 byte)
                byte groupId = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Number of signs (1 byte)
                byte numberOfSigns = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                var signGroup = new SignGroup
                {
                    GroupID = groupId
                };

                // Parse each sign
                for (int signIndex = 0; signIndex < numberOfSigns; signIndex++)
                {
                    // Sign ID (1 byte)
                    byte signId = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                    offset += 2;

                    // Sign type (1 byte)
                    byte signType = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                    offset += 2;

                    // Sign width (WORD - 2 bytes, low byte first)
                    byte widthLow = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                    byte widthHigh = Convert.ToByte(applicationData[(offset + 2)..(offset + 4)], 16);
                    short signWidth = (short)(widthLow + (widthHigh << 8));
                    offset += 4;

                    // Sign height (WORD - 2 bytes, low byte first)
                    byte heightLow = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                    byte heightHigh = Convert.ToByte(applicationData[(offset + 2)..(offset + 4)], 16);
                    short signHeight = (short)(heightLow + (heightHigh << 8));
                    offset += 4;

                    var sign = new Sign
                    {
                        SignID = signId,
                        SignType = (SignType)signType,
                        SignWidth = signWidth,
                        SignHeight = signHeight
                    };

                    signGroup.Signs[signId] = sign;
                }

                // Number of signature data bytes (1 byte)
                byte signatureLength = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                offset += 2;

                // Signature bytes (variable)
                if (signatureLength > 0 && offset + (signatureLength * 2) <= applicationData.Length)
                {
                    signGroup.Signature = applicationData[offset..(offset + signatureLength * 2)];
                    offset += signatureLength * 2;
                }

                signConfiguration.Groups[groupId] = signGroup;
            }

            _logger.LogDebug("Sign Configuration Reply: {GroupCount} groups parsed successfully", numberOfGroups);

            // Store the configuration and complete the TaskCompletionSource
            _signConfiguration = signConfiguration;
            _signConfigurationReplyTaskCompletion?.TrySetResult(signConfiguration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process sign configuration reply: {Message}", ex.Message);
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
            _logger.LogError(ex, "Failed to process report enabled plans: {Message}", ex.Message);
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
            _logger.LogError(ex, "Failed to process sign extended status reply: {Message}", ex.Message);
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

    /// <summary>
    /// Process HAR Voice Data ACK (MI Code 0x47)
    /// Acknowledges receipt of HAR voice data.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessHARVoiceDataAck(string applicationData)
    {
        try
        {
            // HAR Voice Data ACK format (4 bytes):
            // Position 1: MI Code (47h)
            // Position 2-3: Voice ID (WORD)
            // Position 4: Sequence Number

            byte voiceIdLow = Convert.ToByte(applicationData[2..4], 16);
            byte voiceIdHigh = Convert.ToByte(applicationData[4..6], 16);
            ushort voiceId = (ushort)(voiceIdLow + (voiceIdHigh << 8));
            byte sequenceNumber = Convert.ToByte(applicationData[6..8], 16);

            _logger.LogDebug("HAR Voice Data ACK received - VoiceID: {VoiceId}, Sequence: {SequenceNumber}", voiceId, sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HAR Voice Data ACK: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process HAR Voice Data NAK (MI Code 0x48)
    /// Indicates HAR voice data was not received correctly.
    /// Contains the sequence number of the last correctly received data.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessHARVoiceDataNak(string applicationData)
    {
        try
        {
            // HAR Voice Data NAK format (4 bytes):
            // Position 1: MI Code (48h)
            // Position 2-3: Voice ID (WORD)
            // Position 4: Sequence Number (last correctly received)

            byte voiceIdLow = Convert.ToByte(applicationData[2..4], 16);
            byte voiceIdHigh = Convert.ToByte(applicationData[4..6], 16);
            ushort voiceId = (ushort)(voiceIdLow + (voiceIdHigh << 8));
            byte sequenceNumber = Convert.ToByte(applicationData[6..8], 16);

            _logger.LogWarning("HAR Voice Data NAK received - VoiceID: {VoiceId}, LastGoodSequence: {SequenceNumber}", voiceId, sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HAR Voice Data NAK: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process HAR Set Strategy Reply (MI Code 0x43)
    /// Used when controller reports stored strategy in response to HAR Request Stored Voice/Strategy/Plan.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessHARSetStrategy(string applicationData)
    {
        try
        {
            // HAR Set Strategy format:
            // Position 1: MI Code (43h)
            // Position 2-3: Strategy ID (WORD)
            // Position 4: Revision Number
            // Position 5: Number of Voice IDs to follow
            // Position 6+: Voice IDs (WORD each)

            var harSetStrategy = new HARSetStrategy();

            // Strategy ID (WORD)
            byte strategyIdLow = Convert.ToByte(applicationData[2..4], 16);
            byte strategyIdHigh = Convert.ToByte(applicationData[4..6], 16);
            harSetStrategy.StrategyID = (ushort)(strategyIdLow + (strategyIdHigh << 8));

            harSetStrategy.Revision = Convert.ToByte(applicationData[6..8], 16);

            byte numberOfVoiceIDs = Convert.ToByte(applicationData[8..10], 16);

            // Parse Voice IDs
            int offset = 10;
            for (int i = 0; i < numberOfVoiceIDs; i++)
            {
                byte voiceIdLow = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                byte voiceIdHigh = Convert.ToByte(applicationData[(offset + 2)..(offset + 4)], 16);
                ushort voiceId = (ushort)(voiceIdLow + (voiceIdHigh << 8));
                harSetStrategy.VoiceIDs.Add(voiceId);
                offset += 4;
            }

            _logger.LogDebug("HAR Set Strategy received - StrategyID: {StrategyId}, Revision: {Revision}, VoiceIDs: {VoiceIdCount}",
                harSetStrategy.StrategyID, harSetStrategy.Revision, numberOfVoiceIDs);

            _harSetStrategyTaskCompletion?.TrySetResult(harSetStrategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HAR Set Strategy: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process HAR Set Plan Reply (MI Code 0x45)
    /// Used when controller reports stored plan in response to HAR Request Stored Voice/Strategy/Plan.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessHARSetPlan(string applicationData)
    {
        try
        {
            // HAR Set Plan format (up to 40 bytes):
            // Position 1: MI Code (45h)
            // Position 2: Plan ID
            // Position 3: Revision
            // Position 4: Day of week (bitwise)
            // Position 5-6: Strategy 1 ID (WORD)
            // Position 7: Strategy 1 start hour
            // Position 8: Strategy 1 start minute
            // Position 9: Strategy 1 stop hour
            // Position 10: Strategy 1 stop minute
            // ... (repeat for up to 6 strategies)

            var harSetPlan = new HARSetPlan
            {
                PlanID = Convert.ToByte(applicationData[2..4], 16),
                Revision = Convert.ToByte(applicationData[4..6], 16),
                DayOfWeek = Convert.ToByte(applicationData[6..8], 16)
            };

            // Parse strategy entries
            int offset = 8;
            while (offset + 12 <= applicationData.Length && harSetPlan.Entries.Count < 6)
            {
                // Strategy ID (WORD)
                byte strategyIdLow = Convert.ToByte(applicationData[offset..(offset + 2)], 16);
                byte strategyIdHigh = Convert.ToByte(applicationData[(offset + 2)..(offset + 4)], 16);
                ushort strategyId = (ushort)(strategyIdLow + (strategyIdHigh << 8));

                // Strategy ID 0 indicates end of plan
                if (strategyId == 0)
                    break;

                var entry = new HARSetPlanEntry
                {
                    StrategyID = strategyId,
                    StartHour = Convert.ToByte(applicationData[(offset + 4)..(offset + 6)], 16),
                    StartMinute = Convert.ToByte(applicationData[(offset + 6)..(offset + 8)], 16),
                    StopHour = Convert.ToByte(applicationData[(offset + 8)..(offset + 10)], 16),
                    StopMinute = Convert.ToByte(applicationData[(offset + 10)..(offset + 12)], 16)
                };

                harSetPlan.Entries.Add(entry);
                offset += 12;
            }

            _logger.LogDebug("HAR Set Plan received - PlanID: {PlanId}, Revision: {Revision}, Entries: {EntryCount}",
                harSetPlan.PlanID, harSetPlan.Revision, harSetPlan.Entries.Count);

            _harSetPlanTaskCompletion?.TrySetResult(harSetPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HAR Set Plan: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process Environmental/Weather Values Reply (MI Code 0x82)
    /// Contains environmental/weather sensor values.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessEnvironmentalWeatherValuesReply(string applicationData)
    {
        try
        {
            // Environmental/Weather Values Reply contains sensor readings
            // Format varies based on which sensors are present
            _logger.LogDebug("Environmental/Weather Values Reply received - Data length: {DataLength} bytes", applicationData.Length / 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Environmental/Weather Values Reply: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process Environmental/Weather Threshold Definition Reply (MI Code 0x83)
    /// Contains threshold definitions for environmental/weather parameters.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessEnvironmentalWeatherThresholdDefinitionReply(string applicationData)
    {
        try
        {
            // Environmental/Weather Threshold Definition format:
            // Position 1: MI Code (83h)
            // Position 2: Environmental/Weather Parameter Type
            // Position 3: Number of thresholds to follow
            // For each threshold:
            //   - Threshold Value (WORD)
            //   - Rising/Falling threshold (0/1)

            byte parameterType = Convert.ToByte(applicationData[2..4], 16);
            byte numberOfThresholds = Convert.ToByte(applicationData[4..6], 16);

            _logger.LogDebug("Environmental/Weather Threshold Definition Reply received - ParameterType: {ParameterType}, NumberOfThresholds: {ThresholdCount}",
                parameterType, numberOfThresholds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Environmental/Weather Threshold Definition Reply: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Process Environmental/Weather Event Log Reply (MI Code 0x86)
    /// Contains environmental/weather event log entries.
    /// </summary>
    /// <param name="applicationData">The hex-encoded application data</param>
    public Task ProcessEnvironmentalWeatherEventLogReply(string applicationData)
    {
        try
        {
            // Environmental/Weather Event Log Reply format:
            // Position 1: MI Code (86h)
            // Position 2: Number of entries (0-20)
            // For each entry (13 bytes):
            //   - Entry number (cycles 0-255)
            //   - Day, Month, Year (WORD), Hours, Minutes, Seconds
            //   - Environmental/Weather Parameter Type
            //   - Rising/Falling threshold (0/1)
            //   - Threshold Value (WORD)

            byte numberOfEntries = Convert.ToByte(applicationData[2..4], 16);

            _logger.LogDebug("Environmental/Weather Event Log Reply received - NumberOfEntries: {EntryCount}", numberOfEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Environmental/Weather Event Log Reply: {Message}", ex.Message);
        }

        return Task.CompletedTask;
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
            _logger.LogError(ex, "Failed to process sign set high resolution graphics frame: {Message}", ex.Message);
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
            _logger.LogError(ex, "Failed to process sign set message: {Message}", ex.Message);
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
            _logger.LogError(ex, "Failed to process sign set plan: {Message}", ex.Message);
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
        var rejectedMiCode = Convert.ToByte(applicationData[2..4], 16);
        var errorCode = Convert.ToByte(applicationData[4..6], 16);

        _logger.LogWarning("Request rejected - MI Code: 0x{MiCode:X2}, Error Code: {ErrorCode}",
            rejectedMiCode, errorCode);

        _rejectReplyCompletion?.TrySetResult(new RejectReply
        {
            RejectedMiCode = rejectedMiCode,
            ApplicationErrorCode = errorCode
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

    /// <summary>
    /// Check if a group ID exists in the stored sign controller configuration.
    /// Group ID 0 is a special case that applies to all groups.
    /// </summary>
    /// <param name="groupId">The group ID to check</param>
    /// <returns>True if the group exists or groupId is 0 (all groups), false otherwise</returns>
    private bool GroupIDExists(byte groupId)
    {
        // Group ID 0 applies to all groups, so it's always valid
        if (groupId == 0)
            return true;

        // If we haven't received the configuration yet, we can't validate
        if (_signConfiguration == null)
        {
            _logger.LogWarning("GroupIDExists called but sign configuration not yet received");
            return true; // Allow the operation to proceed; the controller will reject if invalid
        }

        return _signConfiguration.Groups.ContainsKey(groupId);
    }

    /// <summary>
    /// Validate reset level
    /// </summary>
    /// <param name="resetLevel"></param>
    /// <returns></returns>
    private bool IsValidResetLevel(byte resetLevel)
    {
        return resetLevel == (byte)ResetLevel.Zero
                || resetLevel == (byte)ResetLevel.One
                || resetLevel == (byte)ResetLevel.Two
                || resetLevel == (byte)ResetLevel.Three
                || resetLevel == (byte)ResetLevel.Factory;
    }

    /// <summary>
    /// Get the controller configuration that was received from the device.
    /// Returns null if configuration hasn't been received yet.
    /// </summary>
    /// <returns>The stored SignController configuration or null</returns>
    public Task<SignController?> GetControllerConfigurationAsync()
    {
        return Task.FromResult(_signConfiguration);
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
        try
        {
            SignSetMessage signSetMessage = new SignSetMessage
            {
                MessageID = GetNextExtendedRequestId()
            };

            if (request.Frame1 != null)
            {
                signSetMessage.Frame1Time = (byte)(request.Frame1Time * 10);

                SignSetTextFrame signSetTextFrame1 = new SignSetTextFrame
                {
                    FrameID = GetNextExtendedRequestId(),
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
                    FrameID = GetNextExtendedRequestId(),
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
                    FrameID = GetNextExtendedRequestId(),
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
                    FrameID = GetNextExtendedRequestId(),
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
                    FrameID = GetNextExtendedRequestId(),
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
                    FrameID = GetNextExtendedRequestId(),
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

        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process extended request message: {Message}", ex.Message);
        }
        return result;
    }
}
