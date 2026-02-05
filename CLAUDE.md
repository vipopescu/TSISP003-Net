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
dotnet run --project src/TSISP003-Net

# Run with specific profile
dotnet run --project src/TSISP003-Net --launch-profile https
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

### Repository Structure

```
TSISP003-Net/
├── src/
│   └── TSISP003-Net/             # Main web API project
│       ├── Controllers/          # REST API endpoints
│       ├── Domain/
│       │   └── Entities/         # Domain entities (Sign, Frame, etc.)
│       ├── Services/             # SignControllerService, Factory, Config
│       ├── Infrastructure/
│       │   ├── Tcp/              # TCP client for sign communication
│       │   └── DataStore/        # Data store implementation
│       ├── DTOs/                 # Data Transfer Objects
│       ├── Configuration/        # Settings classes
│       ├── Utilities/            # Helper classes, extensions, constants
│       └── Program.cs
├── tests/
│   └── TSISP003.Tests/           # Unit tests (xUnit)
├── Directory.Build.props         # Common build settings
└── TSISP003-Net.slnx             # Solution file
```

### Core Components

1. **SignControllerService** (`Services/`) - Manages TCP connections and protocol communication with sign controllers
2. **Domain Entities** (`Domain/Entities/`) - Data models for the TSI-SP-003 protocol
3. **TCP Client** (`Infrastructure/Tcp/`) - Low-level TCP communication handling
4. **Controllers** (`Controllers/`) - REST API endpoints for device management

### Key Patterns

- **Factory Pattern**: `SignControllerServiceFactory` manages multiple sign controller instances
- **Hosted Service**: Sign controller services run as background services
- **DTO/Entity Mapping**: Clear separation between API DTOs and internal entities using extension methods

### Configuration Structure

Device configuration is done via `appsettings.json` under `SignControllerServices.Devices`. Each device requires:
- `IpAddress` and `Port` for TCP connection
- `Address` for protocol addressing
- `PasswordOffset` and `SeedOffset` for TSI-SP-003 authentication

### Protocol Implementation

The codebase implements the TSI-SP-003 protocol with entities for:
- Frame types (Text, Graphic, HighResolution, Atomic)
- Sign operations (SetMessage, DisplayFrame, Status)
- Error handling (AckReply, RejectReply, Exceptions)

### API Endpoints

All endpoints follow the pattern `/api/{device}/{operation}` where:
- `{device}` corresponds to configured device names
- Operations include frame setting, message display, and system control

The application runs on ports 5105 (HTTP) and 7012 (HTTPS) in development, with Swagger UI available at `/swagger`.