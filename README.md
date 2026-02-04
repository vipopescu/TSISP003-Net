# TSISP003-Net

A .NET 10 Web API implementation of the **TSI-SP-003 Communications Protocol** for managing roadside Intelligent Transportation Systems (ITS) devices.

## Overview

TSISP003-Net serves as a bridge between HTTP-based control systems and TCP-connected roadside devices. It implements the TSI-SP-003 protocol mandated by Transport for NSW (Australia) for standardized communication with:

- **Variable Message Signs (VMS)**
- **Variable Speed Limit Signs (VSLS)**
- **Highway Advisory Radio (HAR)** systems

The application exposes REST API endpoints that translate to protocol commands sent over TCP to sign controllers.

## Features

### Sign Control
- **Text Frames** - Store and display text with configurable font, color, and conspicuity
- **Graphics Frames** - Standard resolution (up to 255x255 pixels)
- **High-Resolution Graphics** - Large format (up to 65535x65535 pixels)
- **Messages** - Compose up to 6 frames with transition timing
- **Plans** - Schedule messages with daily/weekly time-based activation

### Device Management
- **Dimming Control** - Automatic or manual brightness (levels 1-16)
- **Power Control** - Per-group power on/off
- **Enable/Disable** - Control device groups
- **System Reset** - Multiple reset levels (0-3, 255)
- **Time Synchronization** - Update device clock

### Status & Diagnostics
- **Configuration Request** - Retrieve device specs (groups, signs, dimensions)
- **Status Queries** - Standard and extended status reporting
- **Fault Log** - Retrieve and reset fault history
- **Stored Data Retrieval** - Query frames, messages, and plans

### Highway Advisory Radio (HAR)
- **Voice Strategies** - Store ordered sequences of voice IDs
- **Strategy Activation** - Play or stop voice announcements
- **HAR Plans** - Schedule up to 6 strategies with timing

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- Network access to TSI-SP-003 compatible sign controllers

### Build
```bash
dotnet build TSISP003-Net.slnx
```

### Run
```bash
dotnet run --project src/TSISP003-Net
```

The API will be available at:
- HTTP: `http://localhost:5105`
- HTTPS: `https://localhost:7012`
- Swagger UI: `http://localhost:5105/swagger`

### Test
```bash
dotnet test TSISP003-Net.slnx
```

## Configuration

Configure devices in `appsettings.json`:

```json
{
  "SignControllerServices": {
    "Devices": {
      "TMS01": {
        "IpAddress": "192.168.1.100",
        "Port": 12333,
        "Address": "01",
        "PasswordOffset": "1234",
        "SeedOffset": "56"
      },
      "VMS02": {
        "IpAddress": "192.168.1.101",
        "Port": 12333,
        "Address": "02",
        "PasswordOffset": "ABCD",
        "SeedOffset": "EF"
      }
    }
  }
}
```

| Parameter | Description |
|-----------|-------------|
| `IpAddress` | Device IP address |
| `Port` | TCP port (typically 12333) |
| `Address` | TSI-SP-003 device address (hex) |
| `PasswordOffset` | Authentication offset (hex) |
| `SeedOffset` | Seed for password generation (hex) |

## API Reference

All endpoints follow the pattern: `POST/GET /api/{device}/{operation}`

### System Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SystemReset` | POST | Reset device (levels 0-3, 255) |
| `/{device}/UpdateTime` | POST | Synchronize device clock |

### Frame Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SignSetTextFrame` | POST | Store a text frame |
| `/{device}/SignSetGraphicsFrame` | POST | Store a graphics frame |
| `/{device}/SignSetHighResolutionGraphicsFrame` | POST | Store high-res graphics |
| `/{device}/SignDisplayFrame` | POST | Display a single frame |

### Message Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SignSetMessage` | POST | Create message from frames |
| `/{device}/SignDisplayMessage` | POST | Display a stored message |
| `/{device}/ExtendedRequestMessage` | POST | Set and display multi-frame message |

### Plan Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SignSetPlan` | POST | Store a scheduled plan |
| `/{device}/EnablePlan` | POST | Activate a plan |
| `/{device}/DisablePlan` | POST | Deactivate a plan |
| `/{device}/RequestEnabledPlans` | GET | Query active plans |

### Device Control

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SignSetDimmingLevel` | POST | Set brightness (auto/manual) |
| `/{device}/PowerOnOff` | POST | Control device power |
| `/{device}/DisableEnableDevice` | POST | Enable/disable device groups |

### Status & Configuration

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/SignConfigurationRequest` | GET | Get device configuration |
| `/{device}/StatusRequestExtended` | GET | Get extended status |
| `/{device}/SignExtendedStatusRequest` | GET | Get detailed status |

### Diagnostics

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/RetrieveFaultLog` | GET | Download fault history |
| `/{device}/ResetFaultLog` | POST | Clear fault log |
| `/{device}/SignRequestStoredFrameMessagePlan` | GET | Retrieve stored data |

### HAR Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{device}/HARSetStrategy` | POST | Store voice strategy |
| `/{device}/HARActivateStrategy` | POST | Play/stop strategy |
| `/{device}/HARSetPlan` | POST | Store HAR plan |
| `/{device}/HARRequestStoredVoiceStrategyPlan` | GET | Retrieve HAR data |

## Architecture

```
TSISP003-Net/
├── src/TSISP003-Net/
│   ├── Controllers/          # REST API endpoints
│   ├── Services/             # Business logic & protocol handling
│   │   ├── SignControllerService.cs      # Core protocol implementation
│   │   └── SignControllerServiceFactory.cs # Device instance management
│   ├── Infrastructure/
│   │   └── Tcp/              # TCP client with retry logic
│   ├── Domain/Entities/      # Protocol data models
│   ├── DTOs/                 # API request/response contracts
│   ├── Configuration/        # Settings classes
│   └── Utilities/            # Helpers, extensions, error codes
├── tests/TSISP003.Tests/     # Unit tests (xUnit)
└── Docs/                     # Protocol specification
```

### Key Components

| Component | Responsibility |
|-----------|----------------|
| `SignApiController` | HTTP endpoint handling, request validation |
| `SignControllerService` | Protocol implementation, TCP communication |
| `SignControllerServiceFactory` | Device instance lifecycle management |
| `TCPClient` | Low-level TCP with retry and timeout handling |

### Communication Flow

```
HTTP Request → Controller → Service → TCPClient → Sign Controller
                                ↓
HTTP Response ← Controller ← Service ← TCPClient ← Sign Controller
```

## Protocol Overview

The TSI-SP-003 protocol uses:

- **TCP transport** on configurable port (default 12333)
- **ASCII encoding** for message content
- **Sequence numbers** (NS/NR) for packet ordering
- **CRC checksums** for data integrity
- **Session-based authentication** with password exchange
- **Heartbeat polling** for connection health

### Message Types

| Range | Category |
|-------|----------|
| `0x01-0x02` | Reject, ACK |
| `0x03-0x08` | Session management |
| `0x10-0x2F` | Sign control |
| `0x30-0x4F` | Detector messages |
| `0x50-0x5F` | Weather station |
| `0x60-0x6F` | AVI equipment |
| `0x80-0x8F` | CCTV |
| `0xF0-0xFF` | Manufacturer specific |

For complete protocol details, see the [Docs/](./Docs/) directory or the official [TSI-SP-003 specification](https://standards.transport.nsw.gov.au/search-standard-specific/?id=TBA%20-%200004122:2022).

## Error Handling

The API returns standard HTTP status codes:

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Invalid request or device rejection |
| 404 | Device not found |
| 408 | Request timeout |
| 500 | Server error |

Rejection responses include error details from the device controller.

## Development

### Project Structure

| File | Purpose |
|------|---------|
| `TSISP003-Net.slnx` | Solution file |
| `Directory.Build.props` | Common build settings |
| `appsettings.json` | Runtime configuration |
| `CLAUDE.md` | AI assistant instructions |

### Adding a New Device Type

1. Add entity class in `Domain/Entities/`
2. Create DTO in `DTOs/DTOs.cs`
3. Add conversion extension in `Utilities/Extensions.cs`
4. Implement service method in `SignControllerService.cs`
5. Add controller endpoint in `SignApiController.cs`

## License

This project implements a protocol specified by Transport for NSW.

## References

- [TSI-SP-003 Protocol Specification](https://standards.transport.nsw.gov.au/search-standard-specific/?id=TBA%20-%200004122:2022)
- [Transport for NSW Standards](https://standards.transport.nsw.gov.au/)
