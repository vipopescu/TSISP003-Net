using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Enums;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles frame operations: set text frames, graphics frames, high resolution graphics frames,
/// and request stored frame/message/plan.
/// </summary>
[ApiController]
[Route("api")]
public class FrameController(ILogger<FrameController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<FrameController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    /// <summary>
    /// Sends a text frame to the specified device.
    /// </summary>
    [HttpPost]
    [Route("{device}/SignSetTextFrame")]
    public async Task<IActionResult> SignSetTextFrame(string device, [FromBody] SignSetTextFrameDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetTextFrame(request.AsEntity());

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Configuration Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Configuration Request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError($"Request rejected: {ex.RejectReply.ApplicationErrorCode}");
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting configuration for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting configuration.");
        }
    }

    /// <summary>
    /// Sends a graphics frame to the specified device.
    /// Used for signs with dimensions up to 255 x 255 pixels.
    /// </summary>
    [HttpPost]
    [Route("{device}/SignSetGraphicsFrame")]
    public async Task<IActionResult> SignSetGraphicsFrame(string device, [FromBody] SignSetGraphicsFrameDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetGraphicsFrame(request.AsEntity());

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Set Graphics Frame timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Set Graphics Frame timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Sign Set Graphics Frame rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting graphics frame for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting graphics frame.");
        }
    }

    /// <summary>
    /// Sends a high resolution graphics frame to the specified device.
    /// Used for signs with dimensions up to 65535 x 65535 pixels.
    /// </summary>
    [HttpPost]
    [Route("{device}/SignSetHighResolutionGraphicsFrame")]
    public async Task<IActionResult> SignSetHighResolutionGraphicsFrame(string device, [FromBody] SignSetHighResolutionGraphicsFrameDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetHighResolutionGraphicsFrame(request.AsEntity());

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Set High Resolution Graphics Frame timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Set High Resolution Graphics Frame timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Sign Set High Resolution Graphics Frame rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting high resolution graphics frame for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting high resolution graphics frame.");
        }
    }

    [HttpPost]
    [Route("{device}/SignRequestStoredFrameMessagePlan")]
    public async Task<IActionResult> SignRequestStoredFrameMessagePlan(string device, [FromBody] SignRequestStoredFrameMessagePlanDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controllerResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignRequestStoredFrameMessagePlan((RequestType)request.TypeRequest, request.RequestID);

            if (controllerResponse is SignSetMessage signSetMessage)
            {
                return Ok(signSetMessage.AsDto());
            }
            else if (controllerResponse is SignSetTextFrame signSetTextFrame)
            {
                return Ok(signSetTextFrame.AsDto());
            }
            else if (controllerResponse is SignSetGraphicsFrame signSetGraphicsFrame)
            {
                return Ok(signSetGraphicsFrame.AsDto());
            }
            else if (controllerResponse is SignSetHighResolutionGraphicsFrame signSetHighResGraphicsFrame)
            {
                return Ok(signSetHighResGraphicsFrame.AsDto());
            }
            else if (controllerResponse is SignSetPlan signSetPlan)
            {
                return Ok(signSetPlan.AsDto());
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected response type.");
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
}
