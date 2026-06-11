using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles HAR (Highway Advisory Radio) operations: set strategy, activate strategy,
/// set plan, and request stored voice/strategy/plan.
/// </summary>
[ApiController]
[Route("api")]
public class HARController(ILogger<HARController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<HARController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    /// <summary>
    /// Store a voice strategy in the HAR controller's memory.
    /// A strategy is an ordered sequence of voice IDs that make up a message.
    /// </summary>
    [HttpPost]
    [Route("{device}/HARSetStrategy")]
    public async Task<IActionResult> HARSetStrategy(string device, [FromBody] HARSetStrategyCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var harStatusReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .HARSetStrategy(request.AsEntity());

            return Ok(harStatusReply.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HAR Set Strategy request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "HAR Set Strategy request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "HAR Set Strategy rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting HAR strategy for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting HAR strategy.");
        }
    }

    /// <summary>
    /// Activate a voice strategy stored in the HAR controller's memory.
    /// Strategy ID 0 stops the current strategy.
    /// </summary>
    [HttpPost]
    [Route("{device}/HARActivateStrategy")]
    public async Task<IActionResult> HARActivateStrategy(string device, [FromBody] HARActivateStrategyCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .HARActivateStrategy(request.StrategyID);

            return Ok(ackReply.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HAR Activate Strategy request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "HAR Activate Strategy request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "HAR Activate Strategy rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating HAR strategy for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error activating HAR strategy.");
        }
    }

    /// <summary>
    /// Store a plan of up to six strategies in the HAR controller's memory.
    /// A plan can be programmed on a daily or weekly basis.
    /// </summary>
    [HttpPost]
    [Route("{device}/HARSetPlan")]
    public async Task<IActionResult> HARSetPlan(string device, [FromBody] HARSetPlanCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var harStatusReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .HARSetPlan(request.AsEntity());

            return Ok(harStatusReply.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HAR Set Plan request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "HAR Set Plan request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "HAR Set Plan rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting HAR plan for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting HAR plan.");
        }
    }

    /// <summary>
    /// Request a stored voice, strategy, or plan from the HAR controller.
    /// RequestType: 0 = Voice, 1 = Strategy, 2 = Plan
    /// </summary>
    [HttpPost]
    [Route("{device}/HARRequestStoredVoiceStrategyPlan")]
    public async Task<IActionResult> HARRequestStoredVoiceStrategyPlan(string device, [FromBody] HARRequestStoredCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var response = await _signControllerServiceFactory.GetSignControllerService(device)
                .HARRequestStoredVoiceStrategyPlan(request.RequestType, request.RequestID, request.SequenceNumber);

            if (response is HARSetStrategy harSetStrategy)
            {
                return Ok(harSetStrategy.AsDto());
            }
            else if (response is HARSetPlan harSetPlan)
            {
                return Ok(harSetPlan.AsDto());
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected response type.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HAR Request Stored Voice/Strategy/Plan timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "HAR Request Stored Voice/Strategy/Plan timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "HAR Request Stored Voice/Strategy/Plan rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting HAR stored data for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting HAR stored data.");
        }
    }
}
