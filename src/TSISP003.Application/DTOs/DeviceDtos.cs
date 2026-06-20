namespace TSISP003.Application.DTOs;

/// <summary>A configured sign controller device, as exposed by the device-listing endpoint.</summary>
public class DeviceInfoDto
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; }
}
