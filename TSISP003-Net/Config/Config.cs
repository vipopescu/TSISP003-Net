using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TSISP003_Net.Config
{
    public static class Config
    {
        static Dictionary<string, string> ErrorCodes = new Dictionary<string, string>()
        {
            { "00", "No error" },
            { "01", "Power failure" },
            { "02", "Communications time-out error (communications failure with host)" },
            { "03", "Memory error" },
            { "04", "Battery failure" },
            { "05", "Internal communications failure" },
            { "06", "Sign lamp failure" },
            { "07", "Sign single-LED failure" },
            { "08", "Sign multi-LED failure" },
            { "09", "Over-temperature alarm (fan failure)" },
            { "0A", "Under-temperature alarm (heater failure)" },
            { "0B", "Conspicuity device failure" },
            { "0C", "Sign luminance controller failure" },
            { "0D", "Controller reset (via watchdog)" },
            { "0E", "Battery low" },
            { "0F", "Powered off by command" },
            { "10", "Facility Switch override" },
            { "11", "Sign display driver failure" },
            { "12", "Sign firmware mismatch" },
            { "13", "Sign lamp pair failure" },
            { "14", "Equipment over-temperature" },
            { "15", "No response from sensor" },
            { "16", "Cut sensor cable" },
            { "17", "Sensor short circuit" },
            { "18", "Sensor dirty lens" },
            { "19", "HAR hardware error" },
            { "1A", "HAR radio fault" },
            { "1B", "HAR voice data error" },
            { "1C", "Display time-out error" },
            { "1D", "Backup controller unavailable" },
            { "20", "Under local control" },
            { "21", "Main processor communications error" },
            { "22", "Mimic state error" },
            { "23", "Sign moved from set location" },
            { "24", "Cabinet door open" },
            { "25", "Sign tilted" },
            { "26", "Sign orientation changed" },
            { "27", "Battery charger/regulator fault" },
            { "28", "Internal power supply fault" },
            { "29", "Vibration alarm" },
            { "2A", "Operating on secondary power" }
        };

        static Dictionary<string, string> ApplicationErrorCodes = new Dictionary<string, string>()
        {
            { "00", "No error" },
            { "01", "Device controller off-line" },
            { "02", "Syntax error in command" },
            { "03", "Length error in command - Application message is too long or too short" },
            { "04", "Data checksum error" },
            { "05", "Text with non ASCII characters" },
            { "06", "Frame too large for sign - The number of characters sent exceeds the number of characters capable of being displayed by the sign in that font; or the length of the graphics frame exceeds the graphics frame capabilities of the sign." },
            { "07", "Unknown MI Code" },
            { "08", "MI Code not supported by device controller" },
            { "09", "Power is OFF" },
            { "0A", "Undefined device number" },
            { "0B", "Font not supported" },
            { "0C", "Colour not supported" },
            { "0D", "Overlaps/overlays not supported" },
            { "0E", "Dimming level not supported" },
            { "0F", "Frame, message, plan, voice or strategy currently active" },
            { "10", "Facility Switch override" },
            { "11", "Conspicuity device definition not supported by device controller" },
            { "12", "Transition time not supported" },
            { "13", "Frame, message or plan undefined" },
            { "14", "Plan not enabled" },
            { "15", "Plan enabled" },
            { "16", "Size mismatch - The number of pixel rows or the number of pixel columns required by the message do not match the respective pixel row/column display capabilities of the sign." },
            { "17", "Frame too small - The number of characters sent equals 0; or the length of a graphics frame equals 0; or the length of the graphics frame is less than the graphics frame display capabilities of the sign." },
            { "18", "HAR strategy stopped by master" },
            { "19", "HAR voice or strategy undefined" },
            { "1A", "HAR error in strategy definition" },
            { "1B", "HAR voice data error" },
            { "1C", "HAR voice format not supported by device controller" },
            { "1D", "HAR hardware error" },
            { "1E", "Time expired" },
            { "1F", "Colour depth not supported" },
            { "20", "Incomplete colour frame definition" },
            { "21", "Incorrect password" },
            { "22", "Interlocking reject (invalid settings)" },
            { "23", "Interlocking reject (missing signs)" },
            { "24", "Interlocking not active" },
            { "25", "Interlocking active" }
        };
    }
}