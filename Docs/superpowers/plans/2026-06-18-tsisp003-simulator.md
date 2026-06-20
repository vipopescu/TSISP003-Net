# TSISP003 Simulator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a TCP server that impersonates a TSI-SP-003 roadside sign controller (the "slave"), accepting the protocol's "set" commands, storing frames/messages/plans in in-memory storage, tracking display state, and answering status / request-stored queries so the existing `TSISP003.Api` client can round-trip end-to-end with no hardware.

**Architecture:** Extract the existing `ProtocolHelper`/`ProtocolConstants` into a new shared `TSISP003.Protocol` library so the client and simulator frame/CRC/password with identical code. Add a `TSISP003.Simulator` console host running a `BackgroundService` TCP listener. Each connection gets a `SimulatorSession` (state machine: `Idle → SeedSent → Online`) that decodes packets with a new `PacketCodec`, routes MI codes to handlers, mutates a `SimulatorMemory`, and emits byte-compatible replies built by `SimulatorReplyBuilder`.

**Tech Stack:** .NET 10, C#, `System.Net.Sockets.TcpListener`, `Microsoft.Extensions.Hosting` (Generic Host / `BackgroundService`), xUnit + Moq for tests.

## Global Constraints

- Target framework: `net10.0` (inherited from `Directory.Build.props`; do not add a `TargetFramework` to new csproj files unless the repo pattern requires it — match sibling projects).
- Clean Architecture dependency rule: `TSISP003.Protocol` has **no** project dependencies. `TSISP003.Infrastructure` references `TSISP003.Protocol`. `TSISP003.Simulator` references `TSISP003.Protocol` only (it must NOT reference Infrastructure/Application/Domain).
- All protocol numeric fields are ASCII-hex (2 hex chars per byte) EXCEPT the two decimal-encoded fields in Sign Status Reply called out in Task 5.
- Packet CRC is CRC-CCITT (poly `0x1021`, init `0x0000`) over the **ASCII bytes of the packet string** from `SOH` through end of application data, rendered as 4 uppercase hex chars (`ProtocolHelper.PacketCRC`).
- Sequence numbers: start at 0 at link establishment, then cycle 1..255 (`0→1→…→255→1`). Use the same `IncrementSequenceNumber` rule as the client.
- Use centralized package versions (repo uses Central Package Management — `Directory.Packages.props`). Add `<PackageReference Include="..." />` WITHOUT a `Version=` attribute; add the version to `Directory.Packages.props` if not already present.
- Tests live in `tests/TSISP003.Tests` (existing xUnit project). Run a single test with `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~<Name>"`.
- Commit after each task with a `feat:`/`refactor:`/`test:` prefixed message.

---

## Reference: exact wire formats the simulator must speak

These are derived from the real client in `src/TSISP003.Infrastructure/Services/SignControllerService.cs`. The simulator must EMIT what the client PARSES and PARSE what the client EMITS.

**Data packet (both directions):**
```
SOH | N(S):2hex | N(R):2hex | ADDR:2hex | STX | MI:2hex | appdata… | CRC:4hex | ETX
```
- `SOH=0x01 STX=0x02 ETX=0x03 ACK=0x06 NAK=0x15`
- CRC = `ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(stringFromSohThroughAppdata))`.

**Link ACK / NAK packet (slave→master, no STX):**
```
ACK | N(R):2hex | ADDR:2hex | CRC:4hex | ETX        (NAK identical with 0x15)
```
Client reads `packet[1..3]` as the slave's N(R), `packet[3..5]` as ADDR. (Client does NOT verify CRC on inbound packets, but we compute it correctly anyway.)

**Handshake (slave replies):**
- On `Start Session` (MI 02): send link-ACK, then data packet `MI=03` + seed (1 byte / 2 hex). Client reads seed from `packet[10..12]`.
- On `Password` (MI 04): validate via `ProtocolHelper.GeneratePassword(seed, seedOffset, passwordOffset)` (compare last 4 hex of result to the 4-hex payload). Send link-ACK, then data packet `MI=01` (Acknowledge, no payload).

**Heartbeat (MI 05):** reply with Sign Status Reply data packet `MI=06`.

**Sign Status Reply (MI 06) application-data layout (hex offsets into appdata string, MI at [0..2]):**
| offset | field | encoding |
|--------|-------|----------|
| 0..2   | MI = `06` | hex |
| 2..4   | Online status (`01`=online) | hex |
| 4..6   | Application error code | hex |
| 6..8   | Day | hex |
| 8..10  | Month | hex |
| 10..14 | Year (e.g. 2025 → `07E9`) | **4-hex big-endian word** (`year.ToString("X4")`) |
| 14..16 | Hour | hex |
| 16..18 | Minute | hex |
| 18..20 | Second | hex |
| 20..24 | Controller checksum (WORD) | hex (any value; client ignores) |
| 24..26 | Controller error code | **2-digit DECIMAL** (`value.ToString("D2")`) |
| 26..28 | Number of signs | **2-digit DECIMAL** (`count.ToString("D2")`) |
| then per sign, 18 hex (9 bytes) | SignID, SignErrorCode, SignEnabled(`01`), FrameID, FrameRevision, MessageID, MessageRevision, PlanID, PlanRevision | hex |

**Sign Configuration Reply (MI 22) application-data layout:**
`22` + manufacturer code (10 bytes = 20 hex, any value) + numberOfGroups(1 byte hex) + per group: GroupID(1), NumSigns(1), per sign: SignID(1), Type(1), Width(WORD low-byte-first = `lo hi`), Height(WORD low-byte-first), then SignatureLength(1) + signature bytes. All hex.

**Report Enabled Plans (MI 13):** `13` + numEntries(1 byte hex) + per entry: GroupID(1) PlanID(1).

**Set Text Frame (MI 0A) — master→slave appdata the simulator must PARSE:**
`0A` + FrameID(1) + Revision(1) + Font(1) + Colour(1) + Conspicuity(1) + NumChars(1) + Text(NumChars bytes hex) + **embedded app-CRC (4 hex)**. The embedded CRC is `PacketCRC(GetBytes(HexToAscii(appMsgWithoutEmbeddedCrc)))`. The packet CRC follows as usual.

**Set Graphics Frame (MI 0B):** `0B` + FrameID + Revision + Rows + Cols + Colour + Conspicuity + GraphicsLength(WORD, 4 hex) + GraphicsData(Length bytes) + embedded app-CRC(4 hex).

**Set Hi-Res Graphics Frame (MI 1D):** `1D` + FrameID + Revision + Rows(WORD 4hex) + Cols(WORD 4hex) + Colour + Conspicuity + GraphicsLength(DWORD 8hex) + GraphicsData + embedded app-CRC(4 hex).

**Set Message (MI 0C):** `0C` + MessageID + Revision + TransitionTime + (Frame1ID,Frame1Time)…(Frame6ID,Frame6Time). No embedded app-CRC.

**Set Plan (MI 0D):** `0D` + PlanID + Revision + DayOfWeek + up to 6×(Type,ID,StartHour,StartMin,StopHour,StopMin); a trailing `00` terminator when fewer than 6 entries.

**Display Frame (MI 0E):** `0E` + GroupID + FrameID → reply `*ACK` (MI 01). **Display Message (MI 0F):** `0F` + GroupID + MessageID → `*ACK`. **Display Atomic Frames (MI 2B):** `2B` + GroupID + NumSigns + (SignID,FrameID)… → Sign Status Reply (MI 06).

**Enable Plan (MI 10):** `10` + GroupID + PlanID → `*ACK`. **Disable Plan (MI 11):** `11` + GroupID + PlanID → `*ACK`. **Request Enabled Plans (MI 12):** `12` → Report Enabled Plans (MI 13).

**Sign Request Stored (MI 17):** `17` + RequestType(1) + RequestID(1). RequestType: 1=Frame, 2=Message, 3=Plan (see `Domain/Enums/RequestType.cs` — confirm values when implementing). Reply by re-emitting the stored item's set-command packet (MI 0A/0B/1D/0C/0D). Reject (MI 00) if not found.

**Reject (MI 00):** `00` + rejectedMI(1) + applicationErrorCode(1). Used for unsupported MIs (HAR 0x40-0x48, weather 0x80-0x87) and not-found/ malformed requests.

---

## Task 1: Extract shared `TSISP003.Protocol` library

**Files:**
- Create: `src/TSISP003.Protocol/TSISP003.Protocol.csproj`
- Create: `src/TSISP003.Protocol/ProtocolConstants.cs` (moved)
- Create: `src/TSISP003.Protocol/ProtocolHelper.cs` (moved)
- Delete: `src/TSISP003.Infrastructure/Protocol/ProtocolConstants.cs`
- Delete: `src/TSISP003.Infrastructure/Protocol/ProtocolHelper.cs`
- Modify: `src/TSISP003.Infrastructure/TSISP003.Infrastructure.csproj` (add ProjectReference)
- Modify: `TSISP003-Net.slnx` (register new project)
- Modify: every file with `using TSISP003.Infrastructure.Protocol;` (replace with `using TSISP003.Protocol;`)

**Interfaces:**
- Produces: namespace `TSISP003.Protocol` exposing `ProtocolConstants` (all `MI_*`, `SOH/STX/ETX/EOT/ACK/NAK`) and `ProtocolHelper` (`PacketCRC`, `PacketCRCushort`, `CRCGenerator`, `GetChunks`, `GeneratePassword`, `HexToAscii`, `AsciiToHex`, `PrintMessagePacket`) — identical signatures to today.

- [ ] **Step 1: Find all references to the old namespace**

Run: `grep -rl "TSISP003.Infrastructure.Protocol" src tests`
Expected: a list including `SignControllerService.cs` and possibly test files. Record it.

- [ ] **Step 2: Create the new project file**

Create `src/TSISP003.Protocol/TSISP003.Protocol.csproj`. Match a sibling leaf library (`src/TSISP003.Domain/TSISP003.Domain.csproj`) exactly in structure:

```bash
cat src/TSISP003.Domain/TSISP003.Domain.csproj
```
Mirror it (same `<Project Sdk=...>` and `<PropertyGroup>` content), changing only any assembly/name specifics if present. Most likely the whole file is just:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

- [ ] **Step 3: Move the two files and change their namespace**

```bash
git mv src/TSISP003.Infrastructure/Protocol/ProtocolConstants.cs src/TSISP003.Protocol/ProtocolConstants.cs
git mv src/TSISP003.Infrastructure/Protocol/ProtocolHelper.cs src/TSISP003.Protocol/ProtocolHelper.cs
```
Then edit both moved files: change `namespace TSISP003.Infrastructure.Protocol;` → `namespace TSISP003.Protocol;`. `ProtocolHelper.cs` keeps `using System.Text;` and `using Microsoft.Extensions.Logging;` (it depends on `ILogger`). Add `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />` to the new csproj (version via CPM — see Step 4).

- [ ] **Step 4: Wire dependencies**

Add the logging-abstractions package. Check `Directory.Packages.props` for an existing `Microsoft.Extensions.Logging.Abstractions` entry; if absent, add `<PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="<same major as other MS.Extensions packages>" />`. Add `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />` to `TSISP003.Protocol.csproj`.

In `src/TSISP003.Infrastructure/TSISP003.Infrastructure.csproj`, add under the `<ItemGroup>` that holds ProjectReferences:
```xml
<ProjectReference Include="..\TSISP003.Protocol\TSISP003.Protocol.csproj" />
```

In `TSISP003-Net.slnx`, add inside the `/src/` folder block:
```xml
<Project Path="src/TSISP003.Protocol/TSISP003.Protocol.csproj" />
```

- [ ] **Step 5: Update all `using` statements**

For each file found in Step 1, replace `using TSISP003.Infrastructure.Protocol;` with `using TSISP003.Protocol;`.

Run: `grep -rl "TSISP003.Infrastructure.Protocol" src tests`
Expected: no output (empty).

- [ ] **Step 6: Build and run existing tests to verify the refactor is behavior-preserving**

Run: `dotnet build TSISP003-Net.slnx`
Expected: Build succeeded, 0 errors.

Run: `dotnet test TSISP003-Net.slnx`
Expected: all existing tests pass (same count as before the change).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: extract shared TSISP003.Protocol library"
```

---

## Task 2: `PacketCodec` — build & parse frames

**Files:**
- Create: `src/TSISP003.Protocol/PacketCodec.cs`
- Test: `tests/TSISP003.Tests/Protocol/PacketCodecTests.cs`

**Interfaces:**
- Consumes: `ProtocolConstants`, `ProtocolHelper` from `TSISP003.Protocol`.
- Produces:
  - `record DataPacket(int Ns, int Nr, string Addr, int Mi, string AppData)` where `AppData` is the full hex application string INCLUDING the 2-hex MI prefix (matches the client's `applicationData` convention, e.g. `DispatchDataPacket` passes `packet[8..^5]`).
  - `static class PacketCodec` with:
    - `string BuildData(int ns, int nr, string addr, string appDataWithMi)` → full `SOH…ETX` string with packet CRC.
    - `string BuildAck(int nr, string addr)` and `string BuildNak(int nr, string addr)` → link packets.
    - `bool TryParse(string packet, out DataPacket data, out char kind)` where `kind` is `'D'` (data/SOH), `'A'` (ACK), `'N'` (NAK); returns false on malformed.
    - `bool VerifyCrc(string packet)` → recomputes packet CRC over `SOH..end-of-appdata` and compares the 4 hex before `ETX`.

- [ ] **Step 1: Write failing tests**

Create `tests/TSISP003.Tests/Protocol/PacketCodecTests.cs`:

```csharp
using TSISP003.Protocol;
using Xunit;

namespace TSISP003.Tests.Protocol;

public class PacketCodecTests
{
    [Fact]
    public void BuildData_ThenTryParse_RoundTrips()
    {
        // appdata = MI 06 + one byte payload "AB"
        string packet = PacketCodec.BuildData(ns: 1, nr: 2, addr: "01", appDataWithMi: "06AB");

        Assert.Equal(ProtocolConstants.SOH, packet[0]);
        Assert.Equal(ProtocolConstants.ETX, packet[^1]);

        Assert.True(PacketCodec.TryParse(packet, out var data, out char kind));
        Assert.Equal('D', kind);
        Assert.Equal(1, data.Ns);
        Assert.Equal(2, data.Nr);
        Assert.Equal("01", data.Addr);
        Assert.Equal(0x06, data.Mi);
        Assert.Equal("06AB", data.AppData);
    }

    [Fact]
    public void BuildData_CrcVerifies()
    {
        string packet = PacketCodec.BuildData(0, 0, "01", "05");
        Assert.True(PacketCodec.VerifyCrc(packet));
    }

    [Fact]
    public void BuildData_TamperedCrc_FailsVerify()
    {
        string packet = PacketCodec.BuildData(0, 0, "01", "05");
        // flip a char in the appdata region (index 8 is MI high nibble)
        char[] chars = packet.ToCharArray();
        chars[8] = chars[8] == '0' ? '1' : '0';
        Assert.False(PacketCodec.VerifyCrc(new string(chars)));
    }

    [Fact]
    public void BuildAck_ParsesAsAck_WithNrAndAddr()
    {
        string ack = PacketCodec.BuildAck(nr: 3, addr: "01");
        Assert.Equal(ProtocolConstants.ACK, ack[0]);
        Assert.True(PacketCodec.TryParse(ack, out var data, out char kind));
        Assert.Equal('A', kind);
        Assert.Equal(3, data.Nr);
        Assert.Equal("01", data.Addr);
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse()
    {
        Assert.False(PacketCodec.TryParse("not a packet", out _, out _));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~PacketCodecTests"`
Expected: FAIL — `PacketCodec` does not exist (compile error).

- [ ] **Step 3: Implement `PacketCodec`**

Create `src/TSISP003.Protocol/PacketCodec.cs`:

```csharp
using System.Text;

namespace TSISP003.Protocol;

public record DataPacket(int Ns, int Nr, string Addr, int Mi, string AppData);

public static class PacketCodec
{
    public static string BuildData(int ns, int nr, string addr, string appDataWithMi)
    {
        string body = ProtocolConstants.SOH
            + ns.ToString("X2") + nr.ToString("X2")
            + addr
            + ProtocolConstants.STX
            + appDataWithMi;
        return body
            + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body))
            + ProtocolConstants.ETX;
    }

    public static string BuildAck(int nr, string addr) => BuildLink(ProtocolConstants.ACK, nr, addr);
    public static string BuildNak(int nr, string addr) => BuildLink(ProtocolConstants.NAK, nr, addr);

    private static string BuildLink(char control, int nr, string addr)
    {
        string body = control + nr.ToString("X2") + addr;
        return body
            + ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body))
            + ProtocolConstants.ETX;
    }

    public static bool TryParse(string packet, out DataPacket data, out char kind)
    {
        data = new DataPacket(0, 0, string.Empty, 0, string.Empty);
        kind = '?';
        if (string.IsNullOrEmpty(packet) || packet[^1] != ProtocolConstants.ETX)
            return false;

        char start = packet[0];
        if (start == ProtocolConstants.ACK || start == ProtocolConstants.NAK)
        {
            // control(1) + NR(2) + ADDR(2) + CRC(4) + ETX(1) = 10 chars
            if (packet.Length < 10) return false;
            kind = start == ProtocolConstants.ACK ? 'A' : 'N';
            int lnr = Convert.ToInt32(packet[1..3], 16);
            string laddr = packet[3..5];
            data = new DataPacket(0, lnr, laddr, 0, string.Empty);
            return true;
        }

        if (start == ProtocolConstants.SOH)
        {
            // SOH + NS(2)+NR(2)+ADDR(2) + STX + appdata + CRC(4) + ETX
            if (packet.Length < 13 || packet[7] != ProtocolConstants.STX) return false;
            int ns = Convert.ToInt32(packet[1..3], 16);
            int nr = Convert.ToInt32(packet[3..5], 16);
            string addr = packet[5..7];
            string appData = packet[8..^5];
            int mi = Convert.ToInt32(appData[0..2], 16);
            kind = 'D';
            data = new DataPacket(ns, nr, addr, mi, appData);
            return true;
        }

        return false;
    }

    public static bool VerifyCrc(string packet)
    {
        if (string.IsNullOrEmpty(packet) || packet[^1] != ProtocolConstants.ETX) return false;
        string body = packet[0..^5];          // everything except CRC(4) + ETX(1)
        string crc = packet[^5..^1];
        return ProtocolHelper.PacketCRC(Encoding.ASCII.GetBytes(body)) == crc;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~PacketCodecTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: add PacketCodec for TSISP003 frame build/parse"
```

---

## Task 3: Simulator project skeleton + `SimulatorMemory`

**Files:**
- Create: `src/TSISP003.Simulator/TSISP003.Simulator.csproj`
- Create: `src/TSISP003.Simulator/Storage/StoredItems.cs` (record types)
- Create: `src/TSISP003.Simulator/Storage/SimulatorMemory.cs`
- Create: `src/TSISP003.Simulator/Configuration/SimulatorOptions.cs`
- Modify: `TSISP003-Net.slnx`
- Test: `tests/TSISP003.Tests/Simulator/SimulatorMemoryTests.cs`

**Interfaces:**
- Produces:
  - `record StoredTextFrame(byte FrameId, byte Revision, byte Font, byte Colour, byte Conspicuity, byte NumChars, string TextHex)`
  - `record StoredGraphicsFrame(byte FrameId, byte Revision, byte Rows, byte Cols, byte Colour, byte Conspicuity, ushort Length, string DataHex)`
  - `record StoredHiResFrame(byte FrameId, byte Revision, ushort Rows, ushort Cols, byte Colour, byte Conspicuity, uint Length, string DataHex)`
  - `record StoredMessage(byte MessageId, byte Revision, byte TransitionTime, (byte Id, byte Time)[] Frames)`
  - `record StoredPlanEntry(byte Type, byte Id, byte StartHour, byte StartMin, byte StopHour, byte StopMin)`
  - `record StoredPlan(byte PlanId, byte Revision, byte DayOfWeek, StoredPlanEntry[] Entries)`
  - `class SimulatorMemory` with `IDictionary` stores keyed by `byte` id for each of the above, plus mutable display state: `byte ActiveFrameId/ActiveFrameRevision/ActiveMessageId/ActiveMessageRevision/ActivePlanId/ActivePlanRevision`, `bool SignEnabled` (default true), and a thread-safe `object Gate`. Methods: `PutTextFrame`, `GetTextFrame(byte)→StoredTextFrame?`, etc.; `SetActiveFrame(byte id, byte rev)`, `SetActiveMessage(byte id, byte rev)`, `SetActivePlan(byte id, byte rev)`.
  - `class SimulatorOptions { string Address {get;set;} = "01"; int Port {get;set;} = 5000; string SeedOffset {get;set;} = "00"; string PasswordOffset {get;set;} = "00"; byte Seed {get;set;} = 0x12; }`

- [ ] **Step 1: Create the project**

Create `src/TSISP003.Simulator/TSISP003.Simulator.csproj` mirroring `src/TSISP003.Api/TSISP003.Api.csproj`'s SDK style but as a worker/console. Reference the Generic Host packages (via CPM) and the Protocol project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TSISP003.Protocol\TSISP003.Protocol.csproj" />
  </ItemGroup>

</Project>
```

Ensure `Microsoft.Extensions.Hosting` has a `<PackageVersion>` in `Directory.Packages.props` (add it at the version used by `TSISP003.ServiceDefaults`/`AppHost` if not present). Register the project in `TSISP003-Net.slnx` under `/src/`.

Also add a ProjectReference so the test project can see Simulator types (needed from this task onward). In `tests/TSISP003.Tests/TSISP003.Tests.csproj`, add to the ProjectReferences `<ItemGroup>`:
```xml
<ProjectReference Include="..\..\src\TSISP003.Simulator\TSISP003.Simulator.csproj" />
```
(`TSISP003.Protocol` is then available transitively; the test files in later tasks `using TSISP003.Protocol;` directly, which works through the transitive reference.)

- [ ] **Step 2: Write failing tests**

Create `tests/TSISP003.Tests/Simulator/SimulatorMemoryTests.cs`:

```csharp
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorMemoryTests
{
    [Fact]
    public void PutThenGetTextFrame_ReturnsSameFrame()
    {
        var mem = new SimulatorMemory();
        var frame = new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"); // "HELLO"
        mem.PutTextFrame(frame);

        var got = mem.GetTextFrame(7);
        Assert.Equal(frame, got);
    }

    [Fact]
    public void GetTextFrame_Unknown_ReturnsNull()
    {
        var mem = new SimulatorMemory();
        Assert.Null(mem.GetTextFrame(99));
    }

    [Fact]
    public void SetActiveFrame_UpdatesDisplayState()
    {
        var mem = new SimulatorMemory();
        mem.SetActiveFrame(7, 1);
        Assert.Equal(7, mem.ActiveFrameId);
        Assert.Equal(1, mem.ActiveFrameRevision);
    }

    [Fact]
    public void SignEnabled_DefaultsTrue()
    {
        Assert.True(new SimulatorMemory().SignEnabled);
    }
}
```

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorMemoryTests"`
Expected: FAIL — types don't exist.

- [ ] **Step 4: Implement the records and memory**

Create `src/TSISP003.Simulator/Storage/StoredItems.cs`:

```csharp
namespace TSISP003.Simulator.Storage;

public record StoredTextFrame(byte FrameId, byte Revision, byte Font, byte Colour, byte Conspicuity, byte NumChars, string TextHex);
public record StoredGraphicsFrame(byte FrameId, byte Revision, byte Rows, byte Cols, byte Colour, byte Conspicuity, ushort Length, string DataHex);
public record StoredHiResFrame(byte FrameId, byte Revision, ushort Rows, ushort Cols, byte Colour, byte Conspicuity, uint Length, string DataHex);
public record StoredMessage(byte MessageId, byte Revision, byte TransitionTime, (byte Id, byte Time)[] Frames);
public record StoredPlanEntry(byte Type, byte Id, byte StartHour, byte StartMin, byte StopHour, byte StopMin);
public record StoredPlan(byte PlanId, byte Revision, byte DayOfWeek, StoredPlanEntry[] Entries);
```

Create `src/TSISP003.Simulator/Storage/SimulatorMemory.cs`:

```csharp
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
```

Create `src/TSISP003.Simulator/Configuration/SimulatorOptions.cs`:

```csharp
namespace TSISP003.Simulator.Configuration;

public class SimulatorOptions
{
    public string Address { get; set; } = "01";
    public int Port { get; set; } = 5000;
    public string SeedOffset { get; set; } = "00";
    public string PasswordOffset { get; set; } = "00";
    public byte Seed { get; set; } = 0x12;
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorMemoryTests"`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add Simulator project skeleton and in-memory store"
```

---

## Task 4: `SimulatorReplyBuilder` — status & config replies

**Files:**
- Create: `src/TSISP003.Simulator/Protocol/SimulatorReplyBuilder.cs`
- Test: `tests/TSISP003.Tests/Simulator/SimulatorReplyBuilderTests.cs`

**Interfaces:**
- Consumes: `SimulatorMemory`, `SimulatorOptions`, `ProtocolConstants`.
- Produces: `class SimulatorReplyBuilder(SimulatorOptions options)` with pure methods returning **application-data hex strings (MI-prefixed, no framing)**:
  - `string StatusReply(SimulatorMemory mem, DateTime now)` (MI 06)
  - `string ConfigReply()` (MI 22)
  - `string ReportEnabledPlans((byte group, byte plan)[] enabled)` (MI 13)
  - `string Reject(int rejectedMi, byte appErrorCode)` (MI 00)
  - `string Ack()` → `"01"`
  - `string PasswordSeed()` → `"03" + options.Seed:X2`

The single fixed sign uses `SignId = 0x01`, `SignType = 0` (text), width/height `48 x 18` (configurable later if needed).

- [ ] **Step 1: Write failing tests** — assert the exact tricky encodings.

Create `tests/TSISP003.Tests/Simulator/SimulatorReplyBuilderTests.cs`:

```csharp
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorReplyBuilderTests
{
    private static SimulatorReplyBuilder NewBuilder() => new(new SimulatorOptions { Address = "01", Seed = 0x12 });

    [Fact]
    public void StatusReply_EncodesYearAsBigEndianWord_AndDecimalCountFields()
    {
        var mem = new SimulatorMemory();
        mem.SetActiveFrame(7, 1);
        var b = NewBuilder();

        string app = b.StatusReply(mem, new DateTime(2025, 3, 4, 5, 6, 7));

        Assert.Equal("06", app[0..2]);          // MI
        Assert.Equal("01", app[2..4]);          // online
        Assert.Equal("07E9", app[10..14]);      // year 2025 big-endian word
        Assert.Equal("00", app[24..26]);        // controller error, decimal D2
        Assert.Equal("01", app[26..28]);        // number of signs, decimal D2
        // sign block (18 hex): SignID=01, err=00, enabled=01, frame=07, frameRev=01, msg=00, msgRev=00, plan=00, planRev=00
        Assert.Equal("010001070100000000", app[28..46]);
    }

    [Fact]
    public void Ack_IsMi01() => Assert.Equal("01", NewBuilder().Ack());

    [Fact]
    public void PasswordSeed_IsMi03PlusSeed() => Assert.Equal("0312", NewBuilder().PasswordSeed());

    [Fact]
    public void Reject_CarriesRejectedMiAndError()
        => Assert.Equal("004005", NewBuilder().Reject(0x40, 5));

    [Fact]
    public void ConfigReply_HasOneGroupOneSign()
    {
        string app = NewBuilder().ConfigReply();
        Assert.Equal("22", app[0..2]);
        Assert.Equal("01", app[22..24]); // number of groups
    }

    [Fact]
    public void ReportEnabledPlans_EncodesEntries()
    {
        string app = NewBuilder().ReportEnabledPlans(new[] { ((byte)1, (byte)5) });
        Assert.Equal("13", app[0..2]);
        Assert.Equal("01", app[2..4]);   // count
        Assert.Equal("0105", app[4..8]); // group 1 plan 5
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorReplyBuilderTests"`
Expected: FAIL — `SimulatorReplyBuilder` missing.

- [ ] **Step 3: Implement the builder**

Create `src/TSISP003.Simulator/Protocol/SimulatorReplyBuilder.cs`:

```csharp
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
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorReplyBuilderTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: add SimulatorReplyBuilder for status/config/report replies"
```

---

## Task 5: Set-command parsers → store in memory

**Files:**
- Create: `src/TSISP003.Simulator/Protocol/SetCommandParser.cs`
- Test: `tests/TSISP003.Tests/Simulator/SetCommandParserTests.cs`

**Interfaces:**
- Consumes: `SimulatorMemory`, the `Stored*` records, `ProtocolHelper`.
- Produces: `static class SetCommandParser` with, for each MI, a parse method taking the MI-prefixed appdata hex and returning the `Stored*` record:
  - `StoredTextFrame ParseTextFrame(string appData)` — fields per the layout; `TextHex = appData[14..(14+NumChars*2)]`.
  - `StoredGraphicsFrame ParseGraphicsFrame(string appData)` — length at `[14..18]` (WORD hex), data `[18..18+Length*2]`.
  - `StoredHiResFrame ParseHiResFrame(string appData)` — rows `[6..10]`, cols `[10..14]`, colour `[14..16]`, consp `[16..18]`, length `[18..26]` (DWORD), data `[26..26+Length*2]`.
  - `StoredMessage ParseMessage(string appData)` — 6 (id,time) pairs starting at `[6..]`.
  - `StoredPlan ParsePlan(string appData)` — entries of 12 hex each from `[6..]`, stop at a `00` type terminator or end of data.
- Also produce the inverse re-encoders used by Request-Stored in Task 6 (`BuildTextFrameAppData(StoredTextFrame)` etc.) — defined HERE so parser/encoder stay together:
  - `string BuildTextFrameAppData(StoredTextFrame f)` reproducing the client's `SignSetTextFrame` body **including the embedded application CRC** (`PacketCRC(GetBytes(HexToAscii(appMsg)))`).
  - `string BuildGraphicsFrameAppData(StoredGraphicsFrame f)` (embedded CRC).
  - `string BuildHiResFrameAppData(StoredHiResFrame f)` (embedded CRC).
  - `string BuildMessageAppData(StoredMessage m)` (no embedded CRC; pad to 6 frames with `0000`).
  - `string BuildPlanAppData(StoredPlan p)` (no embedded CRC; trailing `00` if <6 entries).

- [ ] **Step 1: Write failing tests** (round-trip parse↔build, the strongest guarantee).

Create `tests/TSISP003.Tests/Simulator/SetCommandParserTests.cs`:

```csharp
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SetCommandParserTests
{
    [Fact]
    public void TextFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"); // HELLO
        string appData = SetCommandParser.BuildTextFrameAppData(f);

        Assert.Equal("0A", appData[0..2]);
        var parsed = SetCommandParser.ParseTextFrame(appData);
        Assert.Equal(f, parsed);
    }

    [Fact]
    public void GraphicsFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredGraphicsFrame(3, 2, 18, 48, 1, 0, 2, "ABCD");
        string appData = SetCommandParser.BuildGraphicsFrameAppData(f);
        Assert.Equal("0B", appData[0..2]);
        Assert.Equal(f, SetCommandParser.ParseGraphicsFrame(appData));
    }

    [Fact]
    public void HiResFrame_BuildThenParse_RoundTrips()
    {
        var f = new StoredHiResFrame(4, 1, 256, 512, 2, 0, 3, "AABBCC");
        string appData = SetCommandParser.BuildHiResFrameAppData(f);
        Assert.Equal("1D", appData[0..2]);
        Assert.Equal(f, SetCommandParser.ParseHiResFrame(appData));
    }

    [Fact]
    public void Message_BuildThenParse_RoundTrips()
    {
        var m = new StoredMessage(2, 1, 5, new (byte, byte)[] { (7, 10), (8, 10) });
        string appData = SetCommandParser.BuildMessageAppData(m);
        Assert.Equal("0C", appData[0..2]);
        var parsed = SetCommandParser.ParseMessage(appData);
        Assert.Equal((byte)2, parsed.MessageId);
        Assert.Equal((7, 10), ((int)parsed.Frames[0].Id, (int)parsed.Frames[0].Time));
        Assert.Equal((8, 10), ((int)parsed.Frames[1].Id, (int)parsed.Frames[1].Time));
    }

    [Fact]
    public void Plan_BuildThenParse_RoundTrips()
    {
        var p = new StoredPlan(1, 1, 0x7F, new[] { new StoredPlanEntry(1, 7, 8, 0, 17, 30) });
        string appData = SetCommandParser.BuildPlanAppData(p);
        Assert.Equal("0D", appData[0..2]);
        var parsed = SetCommandParser.ParsePlan(appData);
        Assert.Equal((byte)1, parsed.PlanId);
        Assert.Single(parsed.Entries);
        Assert.Equal(p.Entries[0], parsed.Entries[0]);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SetCommandParserTests"`
Expected: FAIL — `SetCommandParser` missing.

- [ ] **Step 3: Implement parser + encoder**

Create `src/TSISP003.Simulator/Protocol/SetCommandParser.cs`:

```csharp
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
```

Note: `ParseTextFrame`/etc. ignore the trailing embedded CRC because field lengths are known. The round-trip test passes because `Build` appends and `Parse` slices by length.

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SetCommandParserTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: add set-command parsers and re-encoders"
```

---

## Task 6: `SimulatorSession` — state machine & dispatch

**Files:**
- Create: `src/TSISP003.Simulator/Session/SimulatorSession.cs`
- Test: `tests/TSISP003.Tests/Simulator/SimulatorSessionTests.cs`

**Interfaces:**
- Consumes: `PacketCodec`, `SimulatorReplyBuilder`, `SetCommandParser`, `SimulatorMemory`, `SimulatorOptions`, `ProtocolConstants`, `ProtocolHelper.GetChunks`.
- Produces: `class SimulatorSession(SimulatorMemory mem, SimulatorReplyBuilder replies, SimulatorOptions options, Func<DateTime> clock)` with:
  - `IReadOnlyList<string> Handle(string incoming)` — feeds bytes through the buffered chunker and returns the list of packet strings to write back (link-ACKs and data replies), in order. Pure/synchronous so it is trivially testable; the TCP layer (Task 7) just writes them.
  - Internal `enum State { Idle, SeedSent, Online }`, plus `int Ns/Nr` sequence tracking and `_incompleteBuffer`.

**Behavior rules (per MI):**
- Every well-formed master DATA packet → first emit a link-ACK (`BuildAck(nextNr, addr)`), then emit the MI-specific data reply (if any). Increment the simulator's `Ns` after sending a data reply (mirror `IncrementSequenceNumber`).
- MI 02 (Start Session): emit link-ACK + data `PasswordSeed()`; state → SeedSent.
- MI 04 (Password): emit link-ACK + data `Ack()`; state → Online.
- MI 05 (Heartbeat): emit data `StatusReply(mem, clock())` (no separate link-ACK needed but emit one for consistency).
- MI 21 (Config Request): emit data `ConfigReply()`.
- MI 0A/0B/1D (set frame): parse, store, emit data `StatusReply`.
- MI 0C (set message): parse, store, emit data `StatusReply`.
- MI 0D (set plan): parse, store, emit data `StatusReply`.
- MI 0E (display frame): `mem.SetActiveFrame(frameId, rev=storedRevOr0)`; emit data `Ack()`. (Frame revision: look up stored frame; if present use its revision, else 0.)
- MI 0F (display message): `mem.SetActiveMessage(id, rev)`; emit `Ack()`.
- MI 2B (display atomic): set active frame to the last (sign,frame) pair's frame; emit `StatusReply`.
- MI 10 (enable plan): record enabled (group,plan) in a session set; `mem.SetActivePlan(planId, rev)`; emit `Ack()`.
- MI 11 (disable plan): remove from enabled set; emit `Ack()`.
- MI 12 (request enabled plans): emit `ReportEnabledPlans(currentEnabled)`.
- MI 17 (request stored): look up by RequestType+id; emit the rebuilt set-command appdata, or `Reject(0x17, errorCode)` if not found.
- MI 07 (end session): emit `Ack()`; state → Idle.
- MI 08/09/14/15/16/1A/1B (supported-but-simple): emit `Ack()` (or for 1B Extended Status, emit a minimal status — to keep scope tight, emit `Reject(mi, code)` ONLY for truly out-of-scope MIs). Per the spec's response table, MI 1B expects MI 1C; since Extended Status is out of the Core-without-extended decision, reply `Reject(0x1B, errorCode)`.
- Unsupported MIs (0x40–0x48 HAR, 0x80–0x87 weather, anything unknown): emit `Reject(mi, errorCode)`.
- Use `ErrorCodes` from `Domain`? No — Simulator must not reference Domain. Define a local `const byte UnsupportedErrorCode = 0x03;` in the session (value not critical; client surfaces it in `SignRequestRejectedException`).

- [ ] **Step 1: Write failing tests** (drive the handshake and a set→status round-trip).

Create `tests/TSISP003.Tests/Simulator/SimulatorSessionTests.cs`:

```csharp
using TSISP003.Protocol;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Session;
using TSISP003.Simulator.Storage;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorSessionTests
{
    private static SimulatorSession NewSession(SimulatorMemory mem)
    {
        var options = new SimulatorOptions { Address = "01", Seed = 0x12 };
        return new SimulatorSession(mem, new SimulatorReplyBuilder(options), options,
            () => new DateTime(2025, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void StartSession_RepliesAckAndPasswordSeed()
    {
        var session = NewSession(new SimulatorMemory());
        string start = PacketCodec.BuildData(0, 0, "01", "02");

        var outPackets = session.Handle(start);

        Assert.Contains(outPackets, p => p[0] == ProtocolConstants.ACK);
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x03 && d.AppData == "0312");
    }

    [Fact]
    public void Heartbeat_RepliesStatusReply()
    {
        var session = NewSession(new SimulatorMemory());
        var heartbeat = PacketCodec.BuildData(0, 0, "01", "05");

        var outPackets = session.Handle(heartbeat);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void SetTextFrame_StoresFrame_AndRepliesStatus()
    {
        var mem = new SimulatorMemory();
        var session = NewSession(mem);
        string appData = SetCommandParser.BuildTextFrameAppData(
            new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"));
        var packet = PacketCodec.BuildData(0, 0, "01", appData);

        var outPackets = session.Handle(packet);

        Assert.NotNull(mem.GetTextFrame(7));
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x06);
    }

    [Fact]
    public void DisplayFrame_MarksFrameActive_AndAcks()
    {
        var mem = new SimulatorMemory();
        mem.PutTextFrame(new StoredTextFrame(7, 3, 0, 0, 0, 5, "48454C4C4F"));
        var session = NewSession(mem);
        var packet = PacketCodec.BuildData(0, 0, "01", "0E" + "01" + "07"); // group 1, frame 7

        var outPackets = session.Handle(packet);

        Assert.Equal(7, mem.ActiveFrameId);
        Assert.Equal(3, mem.ActiveFrameRevision); // picked up from stored frame
        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x01);
    }

    [Fact]
    public void RequestStoredFrame_EchoesStoredFrame()
    {
        var mem = new SimulatorMemory();
        mem.PutTextFrame(new StoredTextFrame(7, 1, 0, 0, 0, 5, "48454C4C4F"));
        var session = NewSession(mem);
        // RequestType 1 = Frame, id 7
        var packet = PacketCodec.BuildData(0, 0, "01", "17" + "01" + "07");

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x0A);
    }

    [Fact]
    public void UnsupportedMi_Rejects()
    {
        var session = NewSession(new SimulatorMemory());
        var packet = PacketCodec.BuildData(0, 0, "01", "40"); // HAR status — out of scope

        var outPackets = session.Handle(packet);

        Assert.Contains(outPackets, p =>
            PacketCodec.TryParse(p, out var d, out var k) && k == 'D' && d.Mi == 0x00);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorSessionTests"`
Expected: FAIL — `SimulatorSession` missing.

- [ ] **Step 3: Confirm `RequestType` enum values**

Run: `cat src/TSISP003.Domain/Enums/RequestType.cs`
Use the integer values it defines for Frame/Message/Plan when mapping MI 17's RequestType byte. If Frame=1, Message=2, Plan=3 differ, adjust the `switch` in the implementation accordingly. (The simulator does NOT reference Domain — hard-code the confirmed integer values with a comment citing the enum.)

- [ ] **Step 4: Implement the session**

Create `src/TSISP003.Simulator/Session/SimulatorSession.cs`:

```csharp
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
    private enum State { Idle, SeedSent, Online }

    private const byte UnsupportedErrorCode = 0x03;
    private const byte NotFoundErrorCode = 0x04;

    // RequestType values per Domain/Enums/RequestType.cs (confirm in Step 3).
    private const int RequestFrame = 1;
    private const int RequestMessage = 2;
    private const int RequestPlan = 3;

    private State _state = State.Idle;
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
                    byte rev = mem.GetTextFrame(frameId)?.Revision ?? (byte)0;
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
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorSessionTests"`
Expected: PASS (6 tests). If `RequestStoredFrame_EchoesStoredFrame` fails on MI mapping, re-check Step 3 enum values.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add SimulatorSession protocol state machine and dispatch"
```

---

## Task 7: TCP listener + host + config + manual end-to-end verification

**Files:**
- Create: `src/TSISP003.Simulator/Tcp/SimulatorListener.cs`
- Create: `src/TSISP003.Simulator/Program.cs`
- Create: `src/TSISP003.Simulator/appsettings.json`
- Test: `tests/TSISP003.Tests/Simulator/SimulatorListenerTests.cs`

**Interfaces:**
- Consumes: `SimulatorSession`, `SimulatorMemory`, `SimulatorReplyBuilder`, `SimulatorOptions`, `BackgroundService`, `TcpListener`.
- Produces: `class SimulatorListener : BackgroundService` that accepts connections and, per connection, reads ASCII, feeds `SimulatorSession.Handle`, writes each returned packet back. One `SimulatorMemory` shared across the process (so stored items persist across reconnects within a run); one `SimulatorSession` per connection.

- [ ] **Step 1: Write a failing integration test** (real loopback socket round-trip of the handshake).

Create `tests/TSISP003.Tests/Simulator/SimulatorListenerTests.cs`:

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TSISP003.Protocol;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Tcp;
using Xunit;

namespace TSISP003.Tests.Simulator;

public class SimulatorListenerTests
{
    [Fact]
    public async Task StartSession_OverRealSocket_ReturnsSeed()
    {
        var options = new SimulatorOptions { Address = "01", Port = 0, Seed = 0x12 };
        using var listener = new SimulatorListener(options, NullLogger<SimulatorListener>.Instance);
        listener.Start();
        int port = listener.BoundPort;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        string start = PacketCodec.BuildData(0, 0, "01", "02");
        byte[] outBytes = Encoding.ASCII.GetBytes(start);
        await stream.WriteAsync(outBytes);

        var buffer = new byte[1024];
        int read = await stream.ReadAsync(buffer);
        string response = Encoding.ASCII.GetString(buffer, 0, read);

        // Response should contain the password-seed data packet "0312".
        Assert.Contains("0312", response);

        await listener.StopAsync(default);
    }
}
```

The test uses two members not on a plain `BackgroundService`: a synchronous `Start()` and a `BoundPort`. Provide them in the implementation (Step 3): `Start()` calls `StartAsync(default)`; `BoundPort` returns the `TcpListener`'s actual bound port (works with `Port = 0`).

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorListenerTests"`
Expected: FAIL — `SimulatorListener` missing.

- [ ] **Step 3: Implement the listener**

Create `src/TSISP003.Simulator/Tcp/SimulatorListener.cs`:

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Protocol;
using TSISP003.Simulator.Session;
using TSISP003.Simulator.Storage;

namespace TSISP003.Simulator.Tcp;

public class SimulatorListener(SimulatorOptions options, ILogger<SimulatorListener> logger)
    : BackgroundService
{
    private readonly TcpListener _listener = new(IPAddress.Any, options.Port);
    private readonly SimulatorMemory _memory = new();

    public int BoundPort => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public void Start() => _listener.Start();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_listener.Server.IsBound) _listener.Start();
        logger.LogInformation("TSISP003 simulator listening on port {Port}", BoundPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }

            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var replies = new SimulatorReplyBuilder(options);
        var session = new SimulatorSession(_memory, replies, options, () => DateTime.Now);
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];
            try
            {
                int read;
                while ((read = await stream.ReadAsync(buffer, token)) > 0)
                {
                    string incoming = Encoding.ASCII.GetString(buffer, 0, read);
                    foreach (var packet in session.Handle(incoming))
                    {
                        byte[] outBytes = Encoding.ASCII.GetBytes(packet);
                        await stream.WriteAsync(outBytes, token);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                logger.LogDebug("Client disconnected: {Message}", ex.Message);
            }
        }
    }

    public override void Dispose()
    {
        _listener.Dispose();
        base.Dispose();
    }
}
```

- [ ] **Step 4: Create the host entrypoint and config**

Create `src/TSISP003.Simulator/Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Tcp;

var builder = Host.CreateApplicationBuilder(args);

var options = new SimulatorOptions();
builder.Configuration.GetSection("Simulator").Bind(options);
builder.Services.AddSingleton(options);
builder.Services.AddHostedService<SimulatorListener>();

await builder.Build().RunAsync();
```

Create `src/TSISP003.Simulator/appsettings.json`:

```json
{
  "Simulator": {
    "Address": "01",
    "Port": 5000,
    "SeedOffset": "00",
    "PasswordOffset": "00",
    "Seed": 18
  }
}
```

Ensure the csproj copies appsettings to output. Add to `src/TSISP003.Simulator/TSISP003.Simulator.csproj`:
```xml
<ItemGroup>
  <None Update="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```
Also add `<PackageReference Include="Microsoft.Extensions.Configuration.Json" />` and `<PackageReference Include="Microsoft.Extensions.Configuration.Binder" />` if not transitively available (build will tell you). Add their `<PackageVersion>` to `Directory.Packages.props` if missing.

`SimulatorListener` is constructed by DI which provides `ILogger<SimulatorListener>` automatically; no extra registration needed.

- [ ] **Step 5: Run to verify the integration test passes**

Run: `dotnet test tests/TSISP003.Tests/TSISP003.Tests.csproj --filter "FullyQualifiedName~SimulatorListenerTests"`
Expected: PASS (1 test).

- [ ] **Step 6: Full solution build & test**

Run: `dotnet build TSISP003-Net.slnx`
Expected: Build succeeded, 0 errors.

Run: `dotnet test TSISP003-Net.slnx`
Expected: ALL tests pass (existing + new).

- [ ] **Step 7: Manual end-to-end against the real client**

In terminal A:
```bash
dotnet run --project src/TSISP003.Simulator
```
Expected log: `TSISP003 simulator listening on port 5000`.

Point the API's device config at the simulator (edit `src/TSISP003.Api/appsettings.Development.json` `SignControllerServices.Devices` so one device has `IpAddress: 127.0.0.1`, `Port: 5000`, `Address: 01`, `SeedOffset: 00`, `PasswordOffset: 00`). In terminal B:
```bash
dotnet run --project src/TSISP003.Api
```
Expected: API logs `Session started successfully` then `Sign configuration received: 1 groups`, and heartbeats proceed without errors. Use Swagger (`/swagger`) to POST a SetTextFrame to that device; expect HTTP 200 and a status reply. Then call RequestStored for that frame id; expect the same text back.

Record the observed outcome (success/log excerpts) in the commit message.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add TCP listener and host for TSISP003 simulator"
```

---

## Self-Review Notes (for the implementer)

- **Spec coverage:** session handshake (Task 6), set text/graphics/hires/message/plan (Tasks 5–6), display frame/message/atomic (Task 6), enable/disable/request plans (Task 6), status & config replies (Task 4), request-stored echo (Tasks 5–6), reject for HAR/weather/unknown (Task 6), in-memory store (Task 3), shared protocol code (Task 1), TCP host (Task 7). All spec sections map to a task.
- **Decimal-vs-hex trap:** Status Reply controller-error-code and sign-count are decimal (`D2`); year is a 4-hex big-endian word. Covered by `SimulatorReplyBuilderTests`.
- **Embedded application CRC:** text/graphics/hires set commands carry an inner CRC over `HexToAscii(appdata)`. The simulator parses by fixed lengths (ignoring it) and reproduces it on re-encode. Covered by `SetCommandParserTests` round-trips.
- **No silent caps:** message parser reads 6 slots, plan parser stops at a `00`-type terminator — both match the client encoder.
- **If a reply fails to parse on the real client during Task 7 manual test:** capture the raw bytes the client received (`ProtocolHelper.PrintMessagePacket` debug logs) and diff against the layout table at the top of this plan before changing code.
