namespace TSISP003.Simulator.Storage;

public record StoredTextFrame(byte FrameId, byte Revision, byte Font, byte Colour, byte Conspicuity, byte NumChars, string TextHex);
public record StoredGraphicsFrame(byte FrameId, byte Revision, byte Rows, byte Cols, byte Colour, byte Conspicuity, ushort Length, string DataHex);
public record StoredHiResFrame(byte FrameId, byte Revision, ushort Rows, ushort Cols, byte Colour, byte Conspicuity, uint Length, string DataHex);
public record StoredMessage(byte MessageId, byte Revision, byte TransitionTime, (byte Id, byte Time)[] Frames);
public record StoredPlanEntry(byte Type, byte Id, byte StartHour, byte StartMin, byte StopHour, byte StopMin);
public record StoredPlan(byte PlanId, byte Revision, byte DayOfWeek, StoredPlanEntry[] Entries);
