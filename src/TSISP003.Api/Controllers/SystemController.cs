using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles system-level operations: reset and time update.
/// </summary>
[ApiController]
[Route("api")]
public class SystemController(ILogger<SystemController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<SystemController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    /// <summary>
    /// Resets the system for the specified device.
    /// Reset levels:
    /// - 0: Blank display, turn off conspicuity devices, set to automatic dimming, deactivate active frame/message, enable sign group
    /// - 1: Level 0 + disable all plans
    /// - 2: Level 1 + reset all faults and fault log
    /// - 3: Level 2 + clear all frames, messages, and plans
    /// - 255: Level 3 + restore factory settings (except device address)
    /// </summary>
    [HttpPost]
    [Route("{device}/SystemReset")]
    public async Task<IActionResult> SystemReset(string device, [FromBody] SystemResetCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .SystemReset(request.GroupID, request.ResetLevel);

            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reset level for device {Device}", device);
            return BadRequest(ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "System Reset timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "System Reset timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("System Reset rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing System Reset for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error performing System Reset.");
        }
    }

    /// <summary>
    /// Updates the real time clock in the device controller.
    /// If no DateTime is provided in the request body, the current server time is used.
    /// </summary>
    [HttpPost]
    [Route("{device}/UpdateTime")]
    public async Task<IActionResult> UpdateTime(string device, [FromBody] UpdateTimeCommandDto? request = null)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .UpdateTime(request?.DateTime);

            return Ok(ackReply.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Update Time timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Update Time timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "Update Time rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error updating time.");
        }
    }
}
