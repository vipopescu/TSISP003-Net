namespace TSISP003.Simulator.Storage;

/// <summary>
/// In-memory store for the simulator's frames, messages, plans, and display state.
/// All mutators and accessors are internally synchronized on <see cref="Gate"/>.
/// Callers may still take <see cref="Gate"/> themselves to read an atomic multi-field
/// snapshot (as <c>SimulatorReplyBuilder.StatusReply</c> does); C# Monitor is
/// reentrant, so the internal locks compose safely.
/// </summary>
public class SimulatorMemory
{
    public object Gate { get; } = new();

    private readonly Dictionary<byte, StoredTextFrame> _text = [];
    private readonly Dictionary<byte, StoredGraphicsFrame> _graphics = [];
    private readonly Dictionary<byte, StoredHiResFrame> _hiRes = [];
    private readonly Dictionary<byte, StoredMessage> _messages = [];
    private readonly Dictionary<byte, StoredPlan> _plans = [];

    public byte ActiveFrameId { get; private set; }
    public byte ActiveFrameRevision { get; private set; }
    public byte ActiveMessageId { get; private set; }
    public byte ActiveMessageRevision { get; private set; }
    public byte ActivePlanId { get; private set; }
    public byte ActivePlanRevision { get; private set; }

    private bool _signEnabled = true;
    public bool SignEnabled
    {
        get { lock (Gate) { return _signEnabled; } }
        set { lock (Gate) { _signEnabled = value; } }
    }

    public void PutTextFrame(StoredTextFrame f) { lock (Gate) { _text[f.FrameId] = f; } }
    public StoredTextFrame? GetTextFrame(byte id) { lock (Gate) { return _text.TryGetValue(id, out var f) ? f : null; } }

    public void PutGraphicsFrame(StoredGraphicsFrame f) { lock (Gate) { _graphics[f.FrameId] = f; } }
    public StoredGraphicsFrame? GetGraphicsFrame(byte id) { lock (Gate) { return _graphics.TryGetValue(id, out var f) ? f : null; } }

    public void PutHiResFrame(StoredHiResFrame f) { lock (Gate) { _hiRes[f.FrameId] = f; } }
    public StoredHiResFrame? GetHiResFrame(byte id) { lock (Gate) { return _hiRes.TryGetValue(id, out var f) ? f : null; } }

    public void PutMessage(StoredMessage m) { lock (Gate) { _messages[m.MessageId] = m; } }
    public StoredMessage? GetMessage(byte id) { lock (Gate) { return _messages.TryGetValue(id, out var m) ? m : null; } }

    public void PutPlan(StoredPlan p) { lock (Gate) { _plans[p.PlanId] = p; } }
    public StoredPlan? GetPlan(byte id) { lock (Gate) { return _plans.TryGetValue(id, out var p) ? p : null; } }

    public void SetActiveFrame(byte id, byte rev) { lock (Gate) { ActiveFrameId = id; ActiveFrameRevision = rev; } }
    public void SetActiveMessage(byte id, byte rev) { lock (Gate) { ActiveMessageId = id; ActiveMessageRevision = rev; } }
    public void SetActivePlan(byte id, byte rev) { lock (Gate) { ActivePlanId = id; ActivePlanRevision = rev; } }
}
