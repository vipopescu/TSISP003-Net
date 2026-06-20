using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles status and diagnostics operations: extended status request, status retrieval,
/// fault log retrieval/reset, and configuration request.
/// </summary>
[ApiController]
[Route("api")]
public class StatusController(ILogger<StatusController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<StatusController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    [HttpPost]
    [Route("{device}/SignExtendedStatusRequest")]
    public async Task<IActionResult> SignExtendedStatusRequest(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controllerResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .SignExtendedStatusRequest();

            return Ok(controllerResponse.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Extended Status Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Extended Status Request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "Sign Extended Status Request rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting extended status for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting extended status.");
        }
    }

    [HttpGet]
    [Route("{device}/RetrieveFaultLog")]
    public async Task<IActionResult> RetrieveFaultLog(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controllerService = _signControllerServiceFactory.GetSignControllerService(device);



            // This method awaits the fault log reply, with a timeout of 3 seconds.
            var faultLogList = await controllerService.RetrieveFaultLog(); // Await the task first

            var faultLogReply = faultLogList.Select(faultLog => faultLog.AsDto())
                            .OrderBy(faultLog => faultLog.EntryNumber)
                            .ToList(); // Then apply Select

            return Ok(faultLogReply);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Fault log reply timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Fault log reply timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fault log for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error processing fault log.");
        }
    }

    [HttpPost]
    [Route("{device}/ResetFaultLog")]
    public async Task<IActionResult> ResetFaultLog(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controllerResponse = await _signControllerServiceFactory.GetSignControllerService(device)
                .ResetFaultLog();

            return Ok(controllerResponse.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Reset Fault Log request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Reset Fault Log request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogWarning(ex, "Reset Fault Log request rejected for device {Device}", device);
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting fault log for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error resetting fault log.");
        }
    }

    [HttpGet]
    [Route("{device}/SignConfigurationRequest")]
    public async Task<IActionResult> SignConfigurationRequest(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device)
                .GetControllerConfigurationAsync();

            if (controller is null)
                return NotFound("Controller configuration not available");

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Configuration Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Configuration Request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting configuration for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting configuration.");
        }
    }
}
