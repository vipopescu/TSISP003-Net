using TSISP003.Protocol;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;

namespace TSISP003.Simulator.Session;

public class SimulatorSession(
    SimulatorMemory mem,
    SimulatorReplyBuilder replies,
    SimulatorOptions options,
    Func<DateTime> clock)
{
    public enum State { Idle, SeedSent, Online }

    private const byte UnsupportedErrorCode = 0x03;
    private const byte NotFoundErrorCode = 0x04;

    // RequestType values per Domain/Enums/RequestType.cs (Frame=1, Message=2, Plan=3).
    private const int RequestFrame = 1;
    private const int RequestMessage = 2;
    private const int RequestPlan = 3;

    private State _state = State.Idle;
    public State CurrentState => _state;
    private int _ns;
    private int _nr;
    private string _buffer = string.Empty;
    private readonly HashSet<(byte group, byte plan)> _enabledPlans = new();

    public IReadOnlyList<string> Handle(string incoming)
    {
        var output = new List<string>();
        _buffer += incoming;
        var chunks = ProtocolHelper.GetChunks(_buffer, out string remaining);
        _buffer = remaining;

        foreach (var packet in chunks)
        {
            if (!PacketCodec.TryParse(packet, out var data, out char kind) || kind != 'D')
                continue; // ignore link ACK/NAK and garbage from master

            // Verify CRC before processing. Bad CRC → NAK with current N(R), no dispatch.
            if (!PacketCodec.VerifyCrc(packet))
            {
                output.Add(PacketCodec.BuildNak(_nr, options.Address));
                continue;
            }

            // Link-acknowledge the received data packet.
            _nr = Increment(data.Ns);
            output.Add(PacketCodec.BuildAck(_nr, options.Address));

            string? reply = Dispatch(data);
            if (reply is not null)
            {
                output.Add(PacketCodec.BuildData(_ns, _nr, options.Address, reply));
                _ns = Increment(_ns);
            }
        }
        return output;
    }

    private string? Dispatch(DataPacket data)
    {
        string a = data.AppData;
        switch (data.Mi)
        {
            case ProtocolConstants.MI_START_SESSION:
                _state = State.SeedSent;
                return replies.PasswordSeed();

            case ProtocolConstants.MI_PASSWORD:
                _state = State.Online;
                return replies.Ack();

            case ProtocolConstants.MI_HEARTBEAT_POLL:
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_CONFIGURATION_REQUEST:
                return replies.ConfigReply();

            case ProtocolConstants.MI_SIGN_SET_TEXT_FRAME:
                mem.PutTextFrame(SetCommandParser.ParseTextFrame(a));
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_SET_GRAPHIC_FRAME:
                mem.PutGraphicsFrame(SetCommandParser.ParseGraphicsFrame(a));
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME:
                mem.PutHiResFrame(SetCommandParser.ParseHiResFrame(a));
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_SET_MESSAGE:
                mem.PutMessage(SetCommandParser.ParseMessage(a));
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_SET_PLAN:
                mem.PutPlan(SetCommandParser.ParsePlan(a));
                return replies.StatusReply(mem, clock());

            case ProtocolConstants.MI_SIGN_DISPLAY_FRAME:
            {
                byte frameId = Convert.ToByte(a[4..6], 16);
                byte rev = mem.GetTextFrame(frameId)?.Revision
                           ?? mem.GetGraphicsFrame(frameId)?.Revision
                           ?? mem.GetHiResFrame(frameId)?.Revision ?? (byte)0;
                mem.SetActiveFrame(frameId, rev);
                return replies.Ack();
            }

            case ProtocolConstants.MI_SIGN_DISPLAY_MESSAGE:
            {
                byte msgId = Convert.ToByte(a[4..6], 16);
                byte rev = mem.GetMessage(msgId)?.Revision ?? (byte)0;
                mem.SetActiveMessage(msgId, rev);
                return replies.Ack();
            }

            case ProtocolConstants.MI_SIGN_DISPLAY_ATOMIC_FRAMES:
            {
                byte numSigns = Convert.ToByte(a[4..6], 16);
                if (numSigns > 0)
                {
                    int lastFrameOff = 6 + (numSigns - 1) * 4 + 2;
                    byte frameId = Convert.ToByte(a[lastFrameOff..(lastFrameOff + 2)], 16);
                    byte rev = mem.GetTextFrame(frameId)?.Revision
                               ?? mem.GetGraphicsFrame(frameId)?.Revision
                               ?? mem.GetHiResFrame(frameId)?.Revision ?? (byte)0;
                    mem.SetActiveFrame(frameId, rev);
                }
                return replies.StatusReply(mem, clock());
            }

            case ProtocolConstants.MI_ENABLE_PLAN:
            {
                byte group = Convert.ToByte(a[2..4], 16);
                byte planId = Convert.ToByte(a[4..6], 16);
                _enabledPlans.Add((group, planId));
                byte rev = mem.GetPlan(planId)?.Revision ?? (byte)0;
                mem.SetActivePlan(planId, rev);
                return replies.Ack();
            }

            case ProtocolConstants.MI_DISABLE_PLAN:
            {
                byte group = Convert.ToByte(a[2..4], 16);
                byte planId = Convert.ToByte(a[4..6], 16);
                _enabledPlans.Remove((group, planId));
                return replies.Ack();
            }

            case ProtocolConstants.MI_REQUEST_ENABLED_PLANS:
                return replies.ReportEnabledPlans(_enabledPlans.ToArray());

            case ProtocolConstants.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN:
                return RequestStored(a);

            case ProtocolConstants.MI_END_SESSION:
                _state = State.Idle;
                return replies.Ack();

            default:
                return replies.Reject(data.Mi, UnsupportedErrorCode);
        }
    }

    private string RequestStored(string a)
    {
        int requestType = Convert.ToInt32(a[2..4], 16);
        byte id = Convert.ToByte(a[4..6], 16);

        switch (requestType)
        {
            case RequestFrame:
                if (mem.GetTextFrame(id) is { } tf) return SetCommandParser.BuildTextFrameAppData(tf);
                if (mem.GetGraphicsFrame(id) is { } gf) return SetCommandParser.BuildGraphicsFrameAppData(gf);
                if (mem.GetHiResFrame(id) is { } hf) return SetCommandParser.BuildHiResFrameAppData(hf);
                break;
            case RequestMessage:
                if (mem.GetMessage(id) is { } m) return SetCommandParser.BuildMessageAppData(m);
                break;
            case RequestPlan:
                if (mem.GetPlan(id) is { } p) return SetCommandParser.BuildPlanAppData(p);
                break;
        }
        return replies.Reject(ProtocolConstants.MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN, NotFoundErrorCode);
    }

    private static int Increment(int current) => current is 0 or >= 255 ? 1 : current + 1;
}
