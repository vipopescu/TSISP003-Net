using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TSISP003.Application.DTOs;
using TSISP003.Infrastructure.Configuration;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Lists the sign controller devices defined in configuration.
/// </summary>
[ApiController]
[Route("api")]
public class DevicesController(IOptions<SignControllerServiceOptions> options) : ControllerBase
{
    private readonly SignControllerServiceOptions _options = options.Value;

    /// <summary>Returns every configured device (name, protocol address, host, port).</summary>
    [HttpGet]
    [Route("devices")]
    public ActionResult<IEnumerable<DeviceInfoDto>> GetDevices()
    {
        var devices = _options.Devices
            .Select(kvp => new DeviceInfoDto
            {
                Name = kvp.Key,
                Address = kvp.Value.Address,
                IpAddress = kvp.Value.IpAddress,
                Port = kvp.Value.Port
            })
            .OrderBy(d => d.Name)
            .ToList();

        return Ok(devices);
    }
}
