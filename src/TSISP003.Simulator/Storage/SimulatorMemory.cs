namespace TSISP003.Simulator.Storage;

/// <summary>Immutable snapshot of one sign's display state, returned by <see cref="SimulatorMemory.SnapshotSigns"/>.</summary>
public readonly record struct SignSnapshot(
    byte SignId,
    byte FrameId, byte FrameRevision,
    byte MessageId, byte MessageRevision,
    byte PlanId, byte PlanRevision,
    bool Enabled);

/// <summary>
/// In-memory store for one controller's frames, messages, plans, and per-sign display state.
/// Stored frames/messages/plans are device-wide; display state is tracked per sign so that
/// atomic display can differ between signs while group display affects all signs.
/// All mutators and accessors are internally synchronized on <see cref="Gate"/>; C# Monitor
/// is reentrant, so the internal locks compose with an outer caller-held <see cref="Gate"/>.
/// </summary>
public class SimulatorMemory
{
    private sealed class SignDisplayState
    {
        public byte FrameId, FrameRevision;
        public byte MessageId, MessageRevision;
        public byte PlanId, PlanRevision;
        public bool Enabled = true;
    }

    public object Gate { get; } = new();

    private readonly Dictionary<byte, StoredTextFrame> _text = [];
    private readonly Dictionary<byte, StoredGraphicsFrame> _graphics = [];
    private readonly Dictionary<byte, StoredHiResFrame> _hiRes = [];
    private readonly Dictionary<byte, StoredMessage> _messages = [];
    private readonly Dictionary<byte, StoredPlan> _plans = [];

    private readonly Dictionary<byte, SignDisplayState> _signs = [];

    /// <summary>Sign IDs in this controller's single group (1..signCount).</summary>
    public IReadOnlyList<byte> SignIds { get; }

    public SimulatorMemory(int signCount = 1)
    {
        if (signCount < 1) signCount = 1;
        var ids = new List<byte>(signCount);
        for (int i = 1; i <= signCount; i++)
        {
            var id = (byte)i;
            ids.Add(id);
            _signs[id] = new SignDisplayState();
        }
        SignIds = ids;
    }

    // ---- Device-wide stores ----
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

    // ---- Group-level display (affects every sign in the group) ----
    public void SetActiveFrameAll(byte id, byte rev)
    {
        lock (Gate) { foreach (var s in _signs.Values) { s.FrameId = id; s.FrameRevision = rev; } }
    }

    public void SetActiveMessageAll(byte id, byte rev)
    {
        lock (Gate) { foreach (var s in _signs.Values) { s.MessageId = id; s.MessageRevision = rev; } }
    }

    public void SetActivePlanAll(byte id, byte rev)
    {
        lock (Gate) { foreach (var s in _signs.Values) { s.PlanId = id; s.PlanRevision = rev; } }
    }

    public void SetSignEnabledAll(bool enabled)
    {
        lock (Gate) { foreach (var s in _signs.Values) s.Enabled = enabled; }
    }

    // ---- Per-sign display (atomic frame display) ----
    public void SetActiveFrameForSign(byte signId, byte id, byte rev)
    {
        lock (Gate) { if (_signs.TryGetValue(signId, out var s)) { s.FrameId = id; s.FrameRevision = rev; } }
    }

    /// <summary>Atomic, ordered snapshot of every sign's display state.</summary>
    public List<SignSnapshot> SnapshotSigns()
    {
        lock (Gate)
        {
            var result = new List<SignSnapshot>(SignIds.Count);
            foreach (var id in SignIds)
            {
                var s = _signs[id];
                result.Add(new SignSnapshot(id, s.FrameId, s.FrameRevision, s.MessageId, s.MessageRevision, s.PlanId, s.PlanRevision, s.Enabled));
            }
            return result;
        }
    }
}
