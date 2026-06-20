# TSISP003 Simulator — Design

**Date:** 2026-06-18
**Status:** Approved (initial)

## Goal

Build a simulator for the TSI-SP-003 protocol: a TCP **server** that impersonates a
roadside sign controller (the "slave"). It accepts the protocol's "set" commands from
the existing client, stores frames/messages/plans **in local (in-memory) memory**, tracks
display state, and answers status/request-stored queries — so the real `TSISP003.Api`
client can connect to it and round-trip data end-to-end without physical hardware.

## Scope

**In scope (Core):** session handshake (start / password / heartbeat / end), the "set"
commands (text frame, graphics frame, hi-res graphics frame, message, plan), display
commands (frame, message, atomic), enable/disable plan + request-enabled-plans,
status / extended-status / configuration replies, and request-stored-frame/message/plan.

**Out of scope:** HAR audio (voice/strategy/plan) and environmental/weather messages —
these MI codes are answered with a `RejectReply`. No fault injection. No disk persistence
(in-memory only; cleared on restart).

## Authoritative reference

Byte-level layouts validated against the spec in `docs/`:
`docs/06_message_summary_table.txt`, `docs/03_app_messages_1_to_20.txt`,
`docs/04_app_messages_21_to_35.txt`, `docs/05_app_messages_36_to_51.txt`,
`docs/02_datalink_layer.txt`, and the source PDF `TS 03644`.

## Architecture & project layout

Two new projects plus one refactor:

```
src/
  TSISP003.Protocol/        NEW — shared protocol primitives
      ProtocolConstants.cs    (MOVED from Infrastructure/Protocol)
      ProtocolHelper.cs       (MOVED from Infrastructure/Protocol: CRC, password, hex, GetChunks)
      PacketCodec.cs          NEW — build/parse SOH & ACK/NAK frames; extract N(S)/N(R)/ADDR/MI/data
  TSISP003.Infrastructure/  references TSISP003.Protocol (drops its own copy)
  TSISP003.Simulator/       NEW — simulated sign controller (console / BackgroundService host)
  TSISP003.Api/
```

Moving `ProtocolHelper`/`ProtocolConstants` into a shared `TSISP003.Protocol` library means
the real client and the simulator frame packets, compute CRCs, and derive passwords with the
*same code* — guaranteeing wire-format agreement. Infrastructure behaviour is unchanged; only
the `using` namespace updates.

## Components (in TSISP003.Simulator)

- **`SimulatorTcpListener`** — `TcpListener` accepting connections, one `SimulatorSession`
  per client. Hosted as a `BackgroundService` so `dotnet run` starts it.
- **`SimulatorSession`** — per-connection state machine + read buffer. Reuses
  `ProtocolHelper.GetChunks` for partial-packet buffering. States: `Idle → SeedSent → Online`.
- **`MessageDispatcher`** — decodes MI via `PacketCodec`, routes to a handler, manages
  slave-side N(S)/N(R) and ACK emission (mirrors the client's increment rules).
- **`SimulatorMemory`** — the "local memory": dictionaries keyed by id for text frames,
  graphics frames, hi-res frames, messages, and plans; plus current display state (active
  frame/message/plan id + revision) for the single sign. In-memory only.
- **Handlers** — one method per supported MI (Core set). HAR & weather MIs → `RejectReply`.

## Device model (fixed)

Hard-coded: **1 group, 1 sign, text type, online.** Configuration / extended-status / status
replies are assembled from this fixed layout combined with live memory state.

## Data flow (worked example)

```
client → StartSession (MI 02)     sim → ACK, then PasswordSeed (MI 03, deterministic seed)
client → Password    (MI 04)      sim → validate via GeneratePassword; ACK + AckMsg (MI 01)
client → Heartbeat   (MI 05)      sim → SignStatusReply (MI 06) built from memory
client → SetTextFrame(MI 0A)      sim → store frame[id]; ACK + StatusReply
client → DisplayFrame(MI 0E)      sim → mark frame active on sign; ACK + StatusReply
client → RequestStored(MI 17)     sim → echo stored frame back (MI 0A payload)
```

The seed sent in MI 03 is deterministic (nondeterministic RNG is unnecessary); the client
computes the password from whatever seed is sent, and the simulator validates against the
same `GeneratePassword` computation.

## Error handling

- Bad CRC / unparseable frame → `NAK` with current N(R).
- Unknown / unsupported MI → `RejectReply` (MI 00) carrying the rejected MI + an `ErrorCodes` value.
- Malformed "set" payload → `RejectReply` with the appropriate application error code.

## Testing

In `tests/TSISP003.Tests`:
- `PacketCodec` round-trip tests (build → parse).
- Password / CRC parity tests against the shared `ProtocolHelper`.
- Handler tests: SetTextFrame then RequestStored returns identical bytes; DisplayFrame is
  reflected in the next StatusReply.
- Byte layouts cross-checked against `docs/06_message_summary_table.txt` and app-message specs.

## Non-goals / YAGNI

- No HAR or weather implementation (reject only).
- No fault injection, no offline simulation toggles.
- No persistence across restarts.
- No multi-device / configurable layout (single fixed group+sign).
