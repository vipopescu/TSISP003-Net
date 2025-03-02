namespace TSISP003_Net.Utils;

using System.Collections.Generic;

public class ErrorCodes
{
    public static readonly Dictionary<int, string> ControllerDeviceErrorCodes = new Dictionary<int, string>
    {
        { 0x00, "No error" },
        { 0x01, "Power failure" },
        { 0x02, "Communications time-out error (communications failure with host)" },
        { 0x03, "Memory error" },
        { 0x04, "Battery failure" },
        { 0x05, "Internal communications failure" },
        { 0x06, "Sign lamp failure" },
        { 0x07, "Sign single-LED failure" },
        { 0x08, "Sign multi-LED failure" },
        { 0x09, "Over-temperature alarm (fan failure)" },
        { 0x0A, "Under-temperature alarm (heater failure)" },
        { 0x0B, "Conspicuity device failure" },
        { 0x0C, "Sign luminance controller failure" },
        { 0x0D, "Controller reset (via watchdog)" },
        { 0x0E, "Battery low" },
        { 0x0F, "Powered off by command" },
        { 0x10, "Facility Switch override" },
        { 0x11, "Sign display driver failure" },
        { 0x12, "Sign firmware mismatch" },
        { 0x13, "Sign lamp pair failure" },
        { 0x14, "Equipment over-mismatch" },
        { 0x15, "No response from sensor" },
        { 0x16, "Cut sensor cable" },
        { 0x17, "Over and under sensor errors" },
        { 0x18, "Sensor dirty lens" },
        { 0x19, "HAR hardware failure" },
        { 0x1A, "HAR radio fault" },
        { 0x1B, "HAR voice data error" },
        { 0x1C, "Display time-out error" },
        { 0x1D, "Backup controller unavailable" },
        { 0x1E, "Not allocated (used incorrectly by SA TS 5719:2017)" },
        { 0x1F, "Not allocated (used incorrectly by SA TS 5719:2017)" },
        { 0x20, "Under local control" },
        { 0x21, "Main processor communications error" },
        { 0x22, "Mimic state error" },
        { 0x23, "Sign moved from set location" },
        { 0x24, "Cabinet door open" },
        { 0x25, "Sign wind tilt" },
        { 0x26, "Sign orientation changed" },
        { 0x27, "Battery charger/regulator fault" },
        { 0x28, "Internal power supply fault" },
        { 0x29, "Vibration alarm" },
        { 0x2A, "Operating on secondary power" },
        { 0x99, "Pre-existing or reoccurring fault exists" }
    };

    public static readonly Dictionary<int, string> ApplicationErrorCodes = new Dictionary<int, string>
    {
        { 0x00, "No error" }
    };
}
