using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TSISP003.SignControllerService;
using TSISP003_Net;
using TSISP003_Net.SignControllerDataStore.Entities;
using TSISP003_Net.Utils;

namespace TSISP003.Controllers;

[ApiController]
[Route("api")]
public class SignApiController(ILogger<SignApiController> logger, SignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<SignApiController> _logger = logger; // I am unsure whether injecting ILogger here is best practice

    private readonly SignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    [HttpPost]
    [Route("{device}/SystemReset")]
    public async Task<IActionResult> SystemReset(string device)
    {
        // TODO: Implement
        return Ok();
    }

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

    [HttpPost]
    [Route("{device}/SignSetGraphicsFrame")]
    public async Task<IActionResult> SignSetGraphicsFrame(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/SignSetHighResolutionGraphicsFrame")]
    public async Task<IActionResult> SignSetHighResolutionGraphicsFrame(string device)
    {
        // TODO: Implement  


        return Ok();
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

    [HttpPost]
    [Route("{device}/SignDisplayAtomicFrames")]
    public async Task<IActionResult> SignDisplayAtomicFrames(string device)
    {
        // TODO: Implement
        return Ok();
    }

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
            Console.WriteLine($"Request rejected: {ex.RejectReply.ApplicationErrorCode}");
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Message - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Message - Error");
        }
    }

    [HttpPost]
    [Route("{device}/SignSetPlan")]
    public async Task<IActionResult> SignSetPlan(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/SignDisplayFrame")]
    public async Task<IActionResult> SignDisplayFrame(string device)
    {
        // TODO: Implement
        return Ok();
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
            Console.WriteLine($"Request rejected: {ex.RejectReply.ApplicationErrorCode}");
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign Display Message - Error for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Sign Display Message - Error");
        }
    }

    [HttpPost]
    [Route("{device}/EnablePlan")]
    public async Task<IActionResult> EnablePlan(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/DisablePlan")]
    public async Task<IActionResult> DisablePlan(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/RequestEnabledPlans")]
    public async Task<IActionResult> RequestEnabledPlans(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/SignSetDimmingLevel")]
    public async Task<IActionResult> SignSetDimmingLevel(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/PowerOnOff")]
    public async Task<IActionResult> PowerOnOff(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/DisableEnableDevice")]
    public async Task<IActionResult> DisableEnableDevice(string device)
    {
        // TODO: Implement
        return Ok();
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
                .SignRequestStoredFrameMessagePlan((Enums.RequestType)request.TypeRequest, request.RequestID);

            if (controllerResponse is SignSetMessage signSetMessage)
            {
                return Ok(signSetMessage.AsDto());
            }
            else if (controllerResponse is SignSetTextFrame signSetTextFrame)
            {
                return Ok(signSetTextFrame.AsDto());
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
            Console.WriteLine($"Request rejected: {ex.RejectReply.ApplicationErrorCode}");
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting configuration for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting configuration.");
        }
    }

    [HttpPost]
    [Route("{device}/SignExtendedStatusRequest")]
    public async Task<IActionResult> SignExtendedStatusRequest(string device)
    {
        // TODO: Implement
        return Ok();
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
        // TODO: Implement
        return Ok();
    }

    [HttpGet]
    [Route("{device}/extended/status")]
    public async Task<IActionResult> StatusRequestExtended(string device)
    {
        try
        {
            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controller = await _signControllerServiceFactory.GetSignControllerService(device).GetStatus();

            return Ok(controller.AsDto());
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Status Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Status Request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting status for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting status.");
        }
    }

    [HttpPost]
    [Route("{device}/extended/request")]
    public async Task<IActionResult> ExtendedRequestMessage(string device, [FromBody] ExtendedRequestMessageDto extendedRequestMessage)
    {
        // Create a new Stopwatch instance
        Stopwatch stopwatch = new Stopwatch();

        // Start measuring time
        stopwatch.Start();

        try
        {

            if (!_signControllerServiceFactory.ContainsSignController(device))
                return NotFound("Device not found");

            var controllerService = _signControllerServiceFactory.GetSignControllerService(device);

            // TODO Parameterize
            byte currentId = 120;

            SignSetMessageDto signSetMessage = new SignSetMessageDto
            {
                MessageID = currentId++
            };

            if (extendedRequestMessage.Frame1 != null)
            {
                signSetMessage.Frame1Time = (byte)(extendedRequestMessage.Frame1Time * 10);

                SignSetTextFrameDto signSetTextFrame1 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame1.Font,
                    Colour = extendedRequestMessage.Frame1.Colour,
                    Conspicuity = extendedRequestMessage.Frame1.Conspicuity,
                    Text = extendedRequestMessage.Frame1.Text
                };

                signSetMessage.Frame1ID = signSetTextFrame1.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame1.AsEntity());
            }

            if (extendedRequestMessage.Frame2 != null)
            {
                signSetMessage.Frame2Time = (byte)(extendedRequestMessage.Frame2Time * 10);

                SignSetTextFrameDto signSetTextFrame2 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame2.Font,
                    Colour = extendedRequestMessage.Frame2.Colour,
                    Conspicuity = extendedRequestMessage.Frame2.Conspicuity,
                    Text = extendedRequestMessage.Frame2.Text
                };

                signSetMessage.Frame2ID = signSetTextFrame2.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame2.AsEntity());
            }

            if (extendedRequestMessage.Frame3 != null)
            {
                signSetMessage.Frame3Time = (byte)(extendedRequestMessage.Frame3Time * 10);

                SignSetTextFrameDto signSetTextFrame3 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame3.Font,
                    Colour = extendedRequestMessage.Frame3.Colour,
                    Conspicuity = extendedRequestMessage.Frame3.Conspicuity,
                    Text = extendedRequestMessage.Frame3.Text
                };

                signSetMessage.Frame3ID = signSetTextFrame3.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame3.AsEntity());
            }

            if (extendedRequestMessage.Frame4 != null)
            {
                signSetMessage.Frame4Time = (byte)(extendedRequestMessage.Frame4Time * 10);

                SignSetTextFrameDto signSetTextFrame4 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame4.Font,
                    Colour = extendedRequestMessage.Frame4.Colour,
                    Conspicuity = extendedRequestMessage.Frame4.Conspicuity,
                    Text = extendedRequestMessage.Frame4.Text
                };

                signSetMessage.Frame4ID = signSetTextFrame4.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame4.AsEntity());
            }

            if (extendedRequestMessage.Frame5 != null)
            {
                signSetMessage.Frame5Time = (byte)(extendedRequestMessage.Frame5Time * 10);

                SignSetTextFrameDto signSetTextFrame5 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame5.Font,
                    Colour = extendedRequestMessage.Frame5.Colour,
                    Conspicuity = extendedRequestMessage.Frame5.Conspicuity,
                    Text = extendedRequestMessage.Frame5.Text
                };

                signSetMessage.Frame5ID = signSetTextFrame5.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame5.AsEntity());
            }

            if (extendedRequestMessage.Frame6 != null)
            {
                signSetMessage.Frame6Time = (byte)(extendedRequestMessage.Frame6Time * 10);

                SignSetTextFrameDto signSetTextFrame6 = new SignSetTextFrameDto
                {
                    FrameID = currentId++,
                    Revision = 0,
                    Font = extendedRequestMessage.Frame6.Font,
                    Colour = extendedRequestMessage.Frame6.Colour,
                    Conspicuity = extendedRequestMessage.Frame6.Conspicuity,
                    Text = extendedRequestMessage.Frame6.Text
                };

                signSetMessage.Frame6ID = signSetTextFrame6.FrameID;

                await controllerService.SignSetTextFrame(signSetTextFrame6.AsEntity());
            }

            await controllerService.SignSetMessage(signSetMessage.AsEntity());

            await controllerService.SignDisplayMessage(new SignDisplayMessage
            {
                GroupID = 1,
                MessageID = signSetMessage.MessageID
            });


        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Sign Status Request timed out for device {Device}", device);
            return StatusCode(StatusCodes.Status408RequestTimeout, "Sign Status Request timed out.");
        }
        catch (SignRequestRejectedException ex)
        {
            _logger.LogError($"Request rejected: {ex.RejectReply.ApplicationErrorCode}");
            return StatusCode(StatusCodes.Status400BadRequest, ex.RejectReply.AsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting status for device {Device}", device);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error requesting status.");
        }
        // Get the elapsed time as a TimeSpan value
        TimeSpan ts = stopwatch.Elapsed;

        // Format and display the TimeSpan value
        Console.WriteLine("Elapsed Time: {0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        return Ok();

    }
}
