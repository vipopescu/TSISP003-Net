using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Extended (non-protocol) management operations layered on top of TSI-SP-003.
/// These are conveniences built from protocol primitives, not part of the protocol itself.
/// </summary>
[ApiController]
[Route("api")]
public class ExtendedController(
    ILogger<ExtendedController> logger,
    ISignControllerServiceFactory signControllerServiceFactory,
    IExtendedSignService extendedSignService) : ControllerBase
{
    private readonly ILogger<ExtendedController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;
    private readonly IExtendedSignService _extendedSignService = extendedSignService;

    /// <summary>
    /// Builds text frames from the request, assembles them into a message, and displays it.
    /// </summary>
    [HttpPost]
    [Route("{device}/extended/request")]
    public async Task<IActionResult> ExtendedRequestMessage(string device, [FromBody] ExtendedRequestMessageDto extendedRequestMessage)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            await _extendedSignService.BuildAndDisplayMessageAsync(device, extendedRequestMessage);

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Extended request message timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Extended request message timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Extended request message rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing extended request message for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error processing extended request message.");
        }
    }

    /// <summary>
    /// Returns the device's sign status, enriched with the resolved content (frames and decoded
    /// text) of each sign's active message/frame.
    /// </summary>
    [HttpGet]
    [Route("{device}/extended/status")]
    public async Task<IActionResult> StatusRequestExtended(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var status = await _extendedSignService.GetExtendedStatusAsync(device);

            if (status is null)
                return NotFound("Status not available");

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting extended status for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting status.");
        }
    }
}
