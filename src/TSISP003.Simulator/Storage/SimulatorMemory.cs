namespace TSISP003.Simulator.Storage;

public class SimulatorMemory
{
    public object Gate { get; } = new();

    private readonly Dictionary<byte, StoredTextFrame> _text = new();
    private readonly Dictionary<byte, StoredGraphicsFrame> _graphics = new();
    private readonly Dictionary<byte, StoredHiResFrame> _hiRes = new();
    private readonly Dictionary<byte, StoredMessage> _messages = new();
    private readonly Dictionary<byte, StoredPlan> _plans = new();

    public byte ActiveFrameId { get; private set; }
    public byte ActiveFrameRevision { get; private set; }
    public byte ActiveMessageId { get; private set; }
    public byte ActiveMessageRevision { get; private set; }
    public byte ActivePlanId { get; private set; }
    public byte ActivePlanRevision { get; private set; }
    public bool SignEnabled { get; set; } = true;

    public void PutTextFrame(StoredTextFrame f) => _text[f.FrameId] = f;
    public StoredTextFrame? GetTextFrame(byte id) => _text.TryGetValue(id, out var f) ? f : null;

    public void PutGraphicsFrame(StoredGraphicsFrame f) => _graphics[f.FrameId] = f;
    public StoredGraphicsFrame? GetGraphicsFrame(byte id) => _graphics.TryGetValue(id, out var f) ? f : null;

    public void PutHiResFrame(StoredHiResFrame f) => _hiRes[f.FrameId] = f;
    public StoredHiResFrame? GetHiResFrame(byte id) => _hiRes.TryGetValue(id, out var f) ? f : null;

    public void PutMessage(StoredMessage m) => _messages[m.MessageId] = m;
    public StoredMessage? GetMessage(byte id) => _messages.TryGetValue(id, out var m) ? m : null;

    public void PutPlan(StoredPlan p) => _plans[p.PlanId] = p;
    public StoredPlan? GetPlan(byte id) => _plans.TryGetValue(id, out var p) ? p : null;

    public void SetActiveFrame(byte id, byte rev) { ActiveFrameId = id; ActiveFrameRevision = rev; }
    public void SetActiveMessage(byte id, byte rev) { ActiveMessageId = id; ActiveMessageRevision = rev; }
    public void SetActivePlan(byte id, byte rev) { ActivePlanId = id; ActivePlanRevision = rev; }
}
