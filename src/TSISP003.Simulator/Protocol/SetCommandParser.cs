using System.Text;
using TSISP003.Protocol;
using TSISP003.Simulator.Storage;

namespace TSISP003.Simulator.Protocol;

public static class SetCommandParser
{
    private static byte B(string s, int start) => Convert.ToByte(s[start..(start + 2)], 16);
    private static ushort W(string s, int start) => Convert.ToUInt16(s[start..(start + 4)], 16);
    private static uint D(string s, int start) => Convert.ToUInt32(s[start..(start + 8)], 16);

    // ---- Text Frame (MI 0A) ----
    public static StoredTextFrame ParseTextFrame(string a)
    {
        byte numChars = B(a, 12);
        string textHex = a[14..(14 + numChars * 2)];
        return new StoredTextFrame(B(a, 2), B(a, 4), B(a, 6), B(a, 8), B(a, 10), numChars, textHex);
    }

    public static string BuildTextFrameAppData(StoredTextFrame f)
    {
        string app = "0A"
            + f.FrameId.ToString("X2") + f.Revision.ToString("X2")
            + f.Font.ToString("X2") + f.Colour.ToString("X2")
            + f.Conspicuity.ToString("X2") + f.NumChars.ToString("X2")
            + f.TextHex;
        return app + EmbeddedCrc(app);
    }

    // ---- Graphics Frame (MI 0B) ----
    public static StoredGraphicsFrame ParseGraphicsFrame(string a)
    {
        ushort len = W(a, 14);
        string data = a[18..(18 + len * 2)];
        return new StoredGraphicsFrame(B(a, 2), B(a, 4), B(a, 6), B(a, 8), B(a, 10), B(a, 12), len, data);
    }

    public static string BuildGraphicsFrameAppData(StoredGraphicsFrame f)
    {
        string app = "0B"
            + f.FrameId.ToString("X2") + f.Revision.ToString("X2")
            + f.Rows.ToString("X2") + f.Cols.ToString("X2")
            + f.Colour.ToString("X2") + f.Conspicuity.ToString("X2")
            + f.Length.ToString("X4") + f.DataHex;
        return app + EmbeddedCrc(app);
    }

    // ---- Hi-Res Frame (MI 1D) ----
    public static StoredHiResFrame ParseHiResFrame(string a)
    {
        uint len = D(a, 18);
        string data = a[26..(26 + (int)len * 2)];
        return new StoredHiResFrame(B(a, 2), B(a, 4), W(a, 6), W(a, 10), B(a, 14), B(a, 16), len, data);
    }

    public static string BuildHiResFrameAppData(StoredHiResFrame f)
    {
        string app = "1D"
            + f.FrameId.ToString("X2") + f.Revision.ToString("X2")
            + f.Rows.ToString("X4") + f.Cols.ToString("X4")
            + f.Colour.ToString("X2") + f.Conspicuity.ToString("X2")
            + f.Length.ToString("X8") + f.DataHex;
        return app + EmbeddedCrc(app);
    }

    // ---- Message (MI 0C) ----
    public static StoredMessage ParseMessage(string a)
    {
        byte id = B(a, 2), rev = B(a, 4), transition = B(a, 6);
        var frames = new List<(byte, byte)>();
        for (int i = 0; i < 6; i++)
        {
            int off = 8 + i * 4;
            if (off + 4 > a.Length) break;
            byte fid = B(a, off), ftime = B(a, off + 2);
            if (fid == 0) continue;            // 0 id = unused slot
            frames.Add((fid, ftime));
        }
        return new StoredMessage(id, rev, transition, frames.ToArray());
    }

    public static string BuildMessageAppData(StoredMessage m)
    {
        var sb = new StringBuilder();
        sb.Append("0C");
        sb.Append(m.MessageId.ToString("X2"));
        sb.Append(m.Revision.ToString("X2"));
        sb.Append(m.TransitionTime.ToString("X2"));
        for (int i = 0; i < 6; i++)
        {
            if (i < m.Frames.Length)
            {
                sb.Append(m.Frames[i].Id.ToString("X2"));
                sb.Append(m.Frames[i].Time.ToString("X2"));
            }
            else sb.Append("0000");
        }
        return sb.ToString();
    }

    // ---- Plan (MI 0D) ----
    public static StoredPlan ParsePlan(string a)
    {
        byte id = B(a, 2), rev = B(a, 4), dow = B(a, 6);
        var entries = new List<StoredPlanEntry>();
        for (int i = 0; i < 6; i++)
        {
            int off = 8 + i * 12;
            if (off + 12 > a.Length) break;
            byte type = B(a, off);
            if (type == 0) break;              // terminator
            entries.Add(new StoredPlanEntry(type, B(a, off + 2), B(a, off + 4),
                B(a, off + 6), B(a, off + 8), B(a, off + 10)));
        }
        return new StoredPlan(id, rev, dow, entries.ToArray());
    }

    public static string BuildPlanAppData(StoredPlan p)
    {
        var sb = new StringBuilder();
        sb.Append("0D");
        sb.Append(p.PlanId.ToString("X2"));
        sb.Append(p.Revision.ToString("X2"));
        sb.Append(p.DayOfWeek.ToString("X2"));
        foreach (var e in p.Entries)
        {
            sb.Append(e.Type.ToString("X2"));
            sb.Append(e.Id.ToString("X2"));
            sb.Append(e.StartHour.ToString("X2"));
            sb.Append(e.StartMin.ToString("X2"));
            sb.Append(e.StopHour.ToString("X2"));
            sb.Append(e.StopMin.ToString("X2"));
        }
        if (p.Entries.Length < 6) sb.Append("00");
        return sb.ToString();
    }

    private static string EmbeddedCrc(string appHex)
        => ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(ProtocolHelper.HexToAscii(appHex)));
}
