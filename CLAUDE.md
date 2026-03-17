# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TSISP003-Net is a .NET 10 web API application that implements the TSI-SP-003 communications protocol for managing roadside devices like Variable Message Signs (VMS) and Variable Speed Limit Signs (VSLS). The application serves as a bridge between HTTP requests and TCP-based sign controllers.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build TSISP003-Net.slnx

# Run the application (development)
dotnet run --project src/TSISP003.Api

# Run with specific profile
dotnet run --project src/TSISP003.Api --launch-profile https
```

### Testing
```bash
# Run all tests
dotnet test TSISP003-Net.slnx

# Run tests with verbose output
dotnet test TSISP003-Net.slnx --verbosity normal
```

### Other Commands
```bash
# Restore packages
dotnet restore TSISP003-Net.slnx

# Clean build artifacts
dotnet clean TSISP003-Net.slnx
```

## Architecture

The project follows **Clean Architecture** with four layers and strict dependency rules.

### Repository Structure

```
TSISP003-Net/
├── src/
│   ├── TSISP003.Domain/              # Core domain layer (no dependencies)
│   │   ├── Entities/                  # Domain entities (Sign, Frame, etc.)
│   │   ├── Enums/                     # SignType, ResetLevel, RequestType
│   │   ├── Constants/                 # ErrorCodes
│   │   ├── Exceptions/               # SignRequestRejectedException
│   │   └── Interfaces/               # ISignResponse marker interface
│   │
│   ├── TSISP003.Application/         # Application layer (depends on Domain)
│   │   ├── DTOs/                      # Data Transfer Objects by concern
│   │   ├── Mapping/                   # Entity ↔ DTO mapping extensions
│   │   └── Interfaces/               # ISignControllerService, ISignControllerServiceFactory, ITcpClient
│   │
│   ├── TSISP003.Infrastructure/      # Infrastructure layer (depends on Domain + Application)
│   │   ├── Tcp/                       # TcpClientAdapter implementation
│   │   ├── Protocol/                  # ProtocolConstants, ProtocolHelper (CRC, password, hex)
│   │   ├── Services/                  # SignControllerService, SignControllerServiceFactory
│   │   ├── Configuration/             # SignControllerConnectionOptions, SignControllerServiceOptions
│   │   ├── DataStore/                 # Data store interface and implementation
│   │   └── DependencyInjection.cs     # IServiceCollection extension for DI registration
│   │
│   └── TSISP003.Api/                 # Presentation layer (depends on Application + Infrastructure)
│       ├── Controllers/               # Split into focused controllers
│       │   ├── SystemController.cs    # SystemReset, UpdateTime, ExtendedRequest
│       │   ├── FrameController.cs     # SetTextFrame, SetGraphicsFrame, RequestStored
│       │   ├── MessageController.cs   # SetMessage, DisplayMessage
│       │   ├── DisplayController.cs   # DisplayFrame, DisplayAtomicFrames
│       │   ├── PlanController.cs      # SetPlan, EnablePlan, DisablePlan
│       │   ├── StatusController.cs    # ExtendedStatus, FaultLog, Configuration
│       │   ├── DeviceController.cs    # PowerOnOff, DisableEnable, DimmingLevel
│       │   └── HARController.cs       # HAR voice/strategy/plan operations
│       ├── Program.cs
│       └── Properties/launchSettings.json
│
├── tests/
│   └── TSISP003.Tests/               # Unit tests (xUnit + Moq)
├── Directory.Build.props              # Common build settings (net10.0)
└── TSISP003-Net.slnx                  # Solution file
```

### Dependency Flow

```
Domain ← Application ← Infrastructure
                    ↖       ↙
                      Api
```

- **Domain**: Pure C# classes, no external dependencies
- **Application**: Depends only on Domain. Defines service interfaces and DTOs
- **Infrastructure**: Implements Application interfaces. Contains protocol logic, TCP communication
- **Api**: Composes everything via DI. References Application (for DTOs/interfaces) and Infrastructure (for DI registration)

### Core Components

1. **SignControllerService** (`Infrastructure/Services/`) - Manages TCP connections, protocol state (N(S)/N(R) sequence numbers), heartbeat polling, and all TSI-SP-003 protocol operations
2. **SignControllerServiceFactory** (`Infrastructure/Services/`) - Creates and manages per-device service instances as a hosted service
3. **Domain Entities** (`Domain/Entities/`) - Data models for the TSI-SP-003 protocol
4. **TcpClientAdapter** (`Infrastructure/Tcp/`) - TCP communication with retry logic and semaphore-based thread safety
5. **ProtocolHelper** (`Infrastructure/Protocol/`) - CRC generation, password computation, hex/ASCII conversion, packet parsing
6. **Controllers** (`Api/Controllers/`) - 8 focused REST API controllers split by domain concern

### Key Patterns

- **Clean Architecture**: Strict dependency inversion with interfaces defined in Application layer
- **Dependency Injection**: Infrastructure registers services via `AddInfrastructure()` extension method
- **Factory Pattern**: `SignControllerServiceFactory` manages multiple sign controller instances
- **Hosted Service**: Sign controller services run as background services with automatic session management
- **DTO/Entity Mapping**: Extension methods in `EntityMappingExtensions` handle all conversions
- **Interface Segregation**: `ISignControllerService` exposes only business operations; protocol internals are encapsulated

### Configuration Structure

Device configuration is done via `appsettings.json` under `SignControllerServices.Devices`. Each device requires:
- `IpAddress` and `Port` for TCP connection
- `Address` for protocol addressing
- `PasswordOffset` and `SeedOffset` for TSI-SP-003 authentication

### Protocol Implementation

The codebase implements the TSI-SP-003 protocol with entities for:
- Frame types (Text, Graphic, HighResolution, Atomic)
- Sign operations (SetMessage, DisplayFrame, Status)
- HAR operations (Voice, Strategy, Plan)
- Error handling (AckReply, RejectReply, Exceptions)

### API Endpoints

All endpoints follow the pattern `/api/{device}/{operation}` where:
- `{device}` corresponds to configured device names
- Operations include frame setting, message display, and system control

The application runs on ports 5105 (HTTP) and 7012 (HTTPS) in development, with Swagger UI available at `/swagger`.
