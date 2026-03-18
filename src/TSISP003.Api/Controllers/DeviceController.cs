using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles device operations: power on/off, disable/enable device, and dimming level.
/// </summary>
[ApiController]
[Route("api")]
public class DeviceController(ILogger<DeviceController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<DeviceController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    [HttpPost]
    [Route("{device}/PowerOnOff")]
    public async Task<IActionResult> PowerOnOff(string device, [FromBody] PowerOnOffCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .PowerOnOff(request.GroupID, request.PoweredOn);

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Configuration Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Configuration Request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning("Request rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting configuration for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting configuration.");
        }
    }

    [HttpPost]
    [Route("{device}/DisableEnableDevice")]
    public async Task<IActionResult> DisableEnableDevice(string device, [FromBody] DisableEnableDeviceCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var entries = request.Entries.Select(e => (e.GroupID, e.Enabled)).ToList();
            var controllerResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .DisableEnableDevice(entries);

            return Ok(controllerResponse.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Disable/Enable Device request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Disable/Enable Device request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "Disable/Enable Device request rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling/enabling device for {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error disabling/enabling device.");
        }
    }

    /// <summary>
    /// Sets the dimming level for specified groups.
    /// DimmingMode: 0 = Automatic, 1 = Manual.
    /// LuminanceLevel: 1-16 (1 = minimum, 16 = maximum intensity). Ignored when DimmingMode is Automatic.
    /// GroupID 0 applies to all groups.
    /// </summary>
    [HttpPost]
    [Route("{device}/SignSetDimmingLevel")]
    public async Task<IActionResult> SignSetDimmingLevel(string device, [FromBody] SignSetDimmingLevelCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var entries = request.Entries
                .Select(e => (e.GroupID, e.DimmingMode, e.LuminanceLevel))
                .ToList();

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetDimmingLevel(entries);

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Set Dimming Level timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Set Dimming Level timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Sign Set Dimming Level rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dimming level for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting dimming level.");
        }
    }
}
