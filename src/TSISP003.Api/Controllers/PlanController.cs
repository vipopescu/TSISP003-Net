using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles plan operations: set plan, enable plan, disable plan, and request enabled plans.
/// </summary>
[ApiController]
[Route("api")]
public class PlanController(ILogger<PlanController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<PlanController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    /// <summary>
    /// Sends a plan to the specified device.
    /// A plan can contain up to 6 frames or messages scheduled by time and day of week.
    /// </summary>
    [HttpPost]
    [Route("{device}/SignSetPlan")]
    public async Task<IActionResult> SignSetPlan(string device, [FromBody] SignSetPlanDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignSetPlan(request.AsEntity());

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Set Plan timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Set Plan timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Sign Set Plan rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting plan for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error setting plan.");
        }
    }

    /// <summary>
    /// Enables a pre-stored plan in a specified group.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).
    /// </summary>
    [HttpPost]
    [Route("{device}/EnablePlan")]
    public async Task<IActionResult> EnablePlan(string device, [FromBody] EnablePlanCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .EnablePlan(request.GroupID, request.PlanID);

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Enable Plan timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Enable Plan timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Enable Plan rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling plan for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error enabling plan.");
        }
    }

    /// <summary>
    /// Disables a pre-stored plan in a specified group.
    /// Plan ID 0 disables all enabled plans on the specified group (except active plan).
    /// An active plan cannot be disabled.
    /// </summary>
    [HttpPost]
    [Route("{device}/DisablePlan")]
    public async Task<IActionResult> DisablePlan(string device, [FromBody] DisablePlanCommandDto request)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var ackReply = await _signControllerServiceFactory.GetSignControllerService(device)
                .DisablePlan(request.GroupID, request.PlanID);

            return Ok();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Disable Plan timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Disable Plan timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Disable Plan rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling plan for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error disabling plan.");
        }
    }

    /// <summary>
    /// Requests which plans are currently enabled in the device controller.
    /// </summary>
    [HttpGet]
    [Route("{device}/RequestEnabledPlans")]
    public async Task<IActionResult> RequestEnabledPlans(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var reportEnabledPlans = await _signControllerServiceFactory.GetSignControllerService(device)
                .RequestEnabledPlans();

            return Ok(reportEnabledPlans.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Request Enabled Plans timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Request Enabled Plans timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError("Request Enabled Plans rejected: {ErrorCode}", ex.RejectReply.ApplicationErrorCode);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting enabled plans for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting enabled plans.");
        }
    }
}
