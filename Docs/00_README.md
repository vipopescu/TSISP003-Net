# TSI-SP-003 Communications Protocol for Roadside Devices
## Split for Claude Code consumption

### Files

| File | Content | Size | Tokens (est) |
|------|---------|------|--------------|
| `01_intro_overview.txt` | Sections 1-2: Purpose, scope, definitions, physical layout, master/slave, protocol attributes | 17KB | ~4K |
| `02_datalink_layer.txt` | Section 3.1-3.5: Data link protocol, CRC, packets, session management, message exchange examples | 35KB | ~8K |
| `03_app_messages_1_to_20.txt` | Section 3.6.3.1-3.6.3.20: Reject, ACK, Session, Password, Heartbeat, Sign Status, Text Frame, Graphics Frame, Message, Plan, Display | 31KB | ~7K |
| `04_app_messages_21_to_35.txt` | Section 3.6.3.21-3.6.3.35: Dimming, Power, Enable/Disable, Request/Report frames, High-res Graphics, Sign Config, Atomic Frames | 26KB | ~6K |
| `05_app_messages_36_to_51.txt` | Section 3.6.3.36-3.6.3.51: Detectors, Weather Stations, AVI Equipment, CCTV, Manufacturer Messages | 35KB | ~8K |
| `06_message_summary_table.txt` | Section 3.6.5: Complete message ID reference table | 6KB | ~1.5K |
| `07_appendices.txt` | Appendices A-E: CRC calculation, Password generation, Error codes, Message examples | 13KB | ~3K |

### Usage

Load only the chunks you need for your task:

- **Implementing basic comms**: Load `01` + `02`
- **VMS/Sign control**: Load `03` + `04` 
- **Detectors/Weather/CCTV**: Load `05`
- **Quick message reference**: Load `06`
- **Error handling & examples**: Load `07`

### Key Message Types (from 06_message_summary_table)

- `0x01` Reject
- `0x02` ACK
- `0x03-0x08` Session management
- `0x10-0x2F` Sign control messages
- `0x30-0x4F` Detector messages  
- `0x50-0x5F` Weather station messages
- `0x60-0x6F` AVI messages
- `0x80-0x8F` CCTV messages
- `0xF0-0xFF` Manufacturer specific
