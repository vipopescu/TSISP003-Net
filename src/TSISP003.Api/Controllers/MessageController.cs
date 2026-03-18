using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles message operations: set message and display message.
/// </summary>
[ApiController]
[Route("api")]
public class MessageController(ILogger<MessageController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<MessageController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    [HttpPost]
    [Route("{device}/SignSetMessage")]
    public async Task<IActionResult> SignSetMessage(string device, [FromBody] SignSetMessageDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetMessage(request.AsEntity());

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Display Message timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Display Message timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning("Request rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Message - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Message - Error");
        }
    }

    [HttpPost]
    [Route("{device}/SignDisplayMessage")]
    public async Task<IActionResult> SignDisplayMessage(string device, [FromBody] SignDisplayMessageDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignDisplayMessage(request.AsEntity());

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Display Message timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Display Message timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning("Request rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Message - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Message - Error");
        }
    }
}
