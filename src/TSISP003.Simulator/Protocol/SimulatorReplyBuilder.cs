using System.Text;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Storage;

namespace TSISP003.Simulator.Protocol;

public class SimulatorReplyBuilder(SimulatorOptions options)
{
    private const byte SignId = 0x01;
    private const byte SignTypeText = 0x00;
    private const ushort SignWidth = 48;
    private const ushort SignHeight = 18;

    public string Ack() => "01";

    public string PasswordSeed() => "03" + options.Seed.ToString("X2");

    public string Reject(int rejectedMi, byte appErrorCode)
        => "00" + rejectedMi.ToString("X2") + appErrorCode.ToString("X2");

    public string StatusReply(SimulatorMemory mem, DateTime now)
    {
        var sb = new StringBuilder();
        sb.Append("06");                              // MI
        sb.Append("01");                              // online
        sb.Append("00");                              // application error code
        sb.Append(((byte)now.Day).ToString("X2"));
        sb.Append(((byte)now.Month).ToString("X2"));
        sb.Append(((ushort)now.Year).ToString("X4")); // year as big-endian word
        sb.Append(((byte)now.Hour).ToString("X2"));
        sb.Append(((byte)now.Minute).ToString("X2"));
        sb.Append(((byte)now.Second).ToString("X2"));
        sb.Append("0000");                            // controller checksum (WORD, ignored by client)
        sb.Append(0.ToString("D2"));                  // controller error code — DECIMAL
        sb.Append(1.ToString("D2"));                  // number of signs — DECIMAL

        lock (mem.Gate)
        {
            sb.Append(SignId.ToString("X2"));
            sb.Append("00");                          // sign error code
            sb.Append(mem.SignEnabled ? "01" : "00");
            sb.Append(mem.ActiveFrameId.ToString("X2"));
            sb.Append(mem.ActiveFrameRevision.ToString("X2"));
            sb.Append(mem.ActiveMessageId.ToString("X2"));
            sb.Append(mem.ActiveMessageRevision.ToString("X2"));
            sb.Append(mem.ActivePlanId.ToString("X2"));
            sb.Append(mem.ActivePlanRevision.ToString("X2"));
        }
        return sb.ToString();
    }

    public string ConfigReply()
    {
        var sb = new StringBuilder();
        sb.Append("22");
        sb.Append(new string('0', 20));               // 10-byte manufacturer code
        sb.Append(1.ToString("X2"));                  // number of groups
        sb.Append(1.ToString("X2"));                  // group id
        sb.Append(1.ToString("X2"));                  // number of signs
        sb.Append(SignId.ToString("X2"));
        sb.Append(SignTypeText.ToString("X2"));
        sb.Append(WordLoHi(SignWidth));
        sb.Append(WordLoHi(SignHeight));
        sb.Append("00");                              // signature length
        return sb.ToString();
    }

    public string ReportEnabledPlans((byte group, byte plan)[] enabled)
    {
        var sb = new StringBuilder();
        sb.Append("13");
        sb.Append(((byte)enabled.Length).ToString("X2"));
        foreach (var (group, plan) in enabled)
        {
            sb.Append(group.ToString("X2"));
            sb.Append(plan.ToString("X2"));
        }
        return sb.ToString();
    }

    private static string WordLoHi(ushort value)
        => ((byte)(value & 0xFF)).ToString("X2") + ((byte)(value >> 8)).ToString("X2");
}
