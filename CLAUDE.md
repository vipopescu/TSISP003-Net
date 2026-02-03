# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TSISP003-Net is a .NET 8 web API application that implements the TSI-SP-003 communications protocol for managing roadside devices like Variable Message Signs (VMS) and Variable Speed Limit Signs (VSLS). The application serves as a bridge between HTTP requests and TCP-based sign controllers.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build TSISP003-Net/TSISP003-Net.sln

# Run the application (development)
cd TSISP003-Net && dotnet run

# Run with specific profile
cd TSISP003-Net && dotnet run --launch-profile https
```

### Testing and Validation
```bash
# Restore packages
dotnet restore TSISP003-Net/TSISP003-Net.sln

# Clean build artifacts
dotnet clean TSISP003-Net/TSISP003-Net.sln
```

## Architecture

### Core Components

1. **SignControllerService** - Manages TCP connections and protocol communication with sign controllers
2. **SignControllerDataStore** - Data models and entities for the TSI-SP-003 protocol
3. **TCP Client** - Low-level TCP communication handling
4. **Controllers** - REST API endpoints for device management

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