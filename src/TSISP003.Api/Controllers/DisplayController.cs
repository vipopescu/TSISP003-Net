using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles display operations: display frame and display atomic frames.
/// </summary>
[ApiController]
[Route("api")]
public class DisplayController(ILogger<DisplayController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<DisplayController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    [HttpPost]
    [Route("{device}/SignDisplayFrame")]
    public async Task<IActionResult> SignDisplayFrame(string device, [FromBody] SignDisplayFrameDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignDisplayFrame(request.AsEntity());

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Display Frame timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Display Frame timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning("Request rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Frame - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Frame - Error");
        }
    }

    [HttpPost]
    [Route("{device}/SignDisplayAtomicFrames")]
    public async Task<IActionResult> SignDisplayAtomicFrames(string device, [FromBody] SignDisplayAtomicFrameDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignDisplayAtomicFrames(request.AsEntity());

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Display Atomic Frame timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Display Atomic Frame timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning("Request rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Atomic Frame - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Atomic Frame - Error");
        }
    }
}
