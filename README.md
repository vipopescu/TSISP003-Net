# TSISP003-Net Overview

TSISP003-Net is a .NET application designed to manage and control roadside sign displays and other ITS devices. The project adheres to the TSI-SP-003 communications protocol mandated by Roads and Maritime Services (now part of Transport for NSW), ensuring standardized, reliable communication between master control systems and roadside devices such as Variable Message Signs (VMS) and Variable Speed Limit Signs (VSLS).

The application exposes its functionality via HTTP requests and integrates directly with sign controllers to manage sign displays.

## Configuration

The application is configured via an `appsettings.json` file. Below is an example configuration snippet for setting up sign controller services:

```json
{
  "SignControllerServices": {
    "Devices": {
      "TMS01": {
        "IpAddress": "XX.XX.XXX.XXX",
        "Port": 12333,
        "Address": "XX",
        "PasswordOffset": "XXXX",
        "SeedOffset": "XX"
      }
    }
  }
}
```

This example configures a device (TMS01) with its network settings and protocol parameters, including password and seed offsets required for the TSI-SP-003 communications protocol.

For the complete protocol specification, please refer to the [TSI-SP-003 Protocol Specification](https://standards.transport.nsw.gov.au/search-standard-specific/?id=TBA%20-%200004122:2022).

> **Note:** This project is a work in progress. Additional features and improvements are under active development.
