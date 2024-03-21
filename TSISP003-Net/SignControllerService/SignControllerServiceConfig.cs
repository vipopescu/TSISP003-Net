
namespace TSISP003.SignControllerService
{
    public class SignControllerServiceConfig
    {
        // Data Presentation Tags
        public const char SOH = '\u0001';
        public const char STX = '\u0002';
        public const char ETX = '\u0003';
        public const char EOT = '\u0004';
        public const char ACK = '\u0006';
        public const char NAK = '\u0015';

        // Mi Codes
        public const int MI_REJECT_MESSAGE = 0x15;
        public const int MI_ACK_MESSAGE = 0x01;
        public const int MI_START_SESSION = 0x02;
        public const int MI_PASSWORD_SEED = 0x03;
        public const int MI_PASSWORD = 0x04;
        public const int MI_HEARTBEAT_POLL = 0x05;
        public const int MI_SIGN_STATUS_REPLY = 0x06;
        public const int MI_END_SESSION = 0x07;
        public const int MI_SYSTEM_RESET = 0x08;
        public const int MI_UPDATE_TIME = 0x09;
        public const int MI_SIGN_SET_TEXT_FRAME = 0x0A;
        public const int MI_SIGN_SET_GRAPHIC_FRAME = 0x0B;
        public const int MI_SIGN_SET_MESSAGE = 0x0C;
        public const int MI_SIGN_SET_PLAN = 0x0D;
        public const int MI_SIGN_DISPLAY_FRAME = 0x0E;
        public const int MI_SIGN_DISPLAY_MESSAGE = 0x0F;
        public const int MI_ENABLE_PLAN = 0x10;
        public const int MI_DISABLE_PLAN = 0x11;
        public const int MI_REQUEST_ENABLED_PLANS = 0x12;
        public const int MI_REPORT_ENABLED_PLANS = 0x13;
        public const int MI_SIGN_SET_DIMMING_LEVEL = 0x14;
        public const int MI_POWER_ON_OFF = 0x15;
        public const int MI_DISABLE_ENABLE_DEVICE = 0x16;
        public const int MI_SIGN_REQUEST_STORED_FRAME_MESSAGE_PLAN = 0x17;
        public const int MI_RETRIEVE_FAULT_LOG = 0x18;
        public const int MI_FAULT_LOG_REPLY = 0x19;
        public const int MI_RESET_FAULT_LOG = 0x1A;
        public const int MI_SIGN_EXTENDED_STATUS_REQUEST = 0x1B;
        public const int MI_SIGN_EXTENDED_STATUS_REPLY = 0x1C;
        public const int MI_SIGN_SET_HIGH_RESOLUTION_GRAPHICS_FRAME = 0x1D;
        public const int MI_SIGN_CONFIGURATION_REQUEST = 0x21;
        public const int MI_SIGN_CONFIGURATION_REPLY = 0x22;
        public const int MI_SIGN_DISPLAY_ATOMIC_FRAMES = 0x2B;
        public const int MI_HAR_STATUS_REPLY = 0x40;
        public const int MI_HAR_SET_VOICE_DATA_INCOMPLETE = 0x41;
        public const int MI_HAR_SET_VOICE_DATA_COMPLETE = 0x42;
        public const int MI_HAR_SET_STRATEGY = 0x43;
        public const int MI_HAR_ACTIVATE_STRATEGY = 0x44;
        public const int MI_HAR_SET_PLAN = 0x45;
        public const int MI_HAR_REQUEST_STORED_VOICE_STRATEGY_PLAN = 0x46;
        public const int MI_HAR_SET_VOICE_DATA_ACK = 0x47;
        public const int MI_HAR_SET_VOICE_DATA_NAK = 0x48;
        public const int MI_ENVIRONMENTAL_WEATHER_STATUS_REPLY = 0x80;
        public const int MI_REQUEST_ENVIRONMENTAL_WEATHER_VALUES = 0x81;
        public const int MI_ENVIRONMENTAL_WEATHER_VALUES = 0x82;
        public const int MI_ENVIRONMENTAL_WEATHER_THRESHOLD_DEFINITION = 0x83;
        public const int MI_REQUEST_THRESHOLD_DEFINITION = 0x84;
        public const int MI_REQUEST_ENVIRONMENTAL_WEATHER_EVENT_LOG = 0x85;
        public const int MI_ENVIRONMENTAL_WEATHER_EVENT_LOG_REPLY = 0x86;
        public const int MI_RESET_ENVIRONMENTAL_WEATHER_EVENT_LOG = 0x87;
    }
}