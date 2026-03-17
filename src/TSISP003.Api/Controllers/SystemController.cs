using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Exceptions;

namespace TSISP003.Api.Controllers;

/// <summary>
/// Handles system-level operations: reset, time update, and extended request messages.
/// </summary>
[ApiController]
[Route("api")]
public class SystemController(ILogger<SystemController> logger, ISignControllerServiceFactory signControllerServiceFactory) : ControllerBase
{
    private readonly ILogger<SystemController> _logger = logger;
    private readonly ISignControllerServiceFactory _signControllerServiceFactory = signControllerServiceFactory;

    // Rolling ID management for ExtendedRequestMessage (per device)
    private static readonly ConcurrentDictionary<string, int> _deviceIds = new();
    private const byte MinId = 120;
    private const byte MaxId = 254;

    private static byte GetNextId(string device)
    {
        var newId = _deviceIds.AddOrUpdate(
            device,
            MinId,
            (_, currentId) => currentId >= MaxId ? MinId : currentId + 1
        );
        return (byte)newId;
    }

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

            SignSetMessageDto signSetMessage = new SignSetMessageDto
            {
                MessageID = GetNextId(device)
            };

            if (extendedRequestMessage.Frame1 != null)
            {
                signSetMessage.Frame1Time = (byte)(extendedRequestMessage.Frame1Time * 10);

                SignSetTextFrameDto signSetTextFrame1 = new SignSetTextFrameDto
                {
                    FrameID = GetNextId(device),
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
                    FrameID = GetNextId(device),
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
                    FrameID = GetNextId(device),
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
                    FrameID = GetNextId(device),
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
                    FrameID = GetNextId(device),
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
                    FrameID = GetNextId(device),
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

        // Log the elapsed time
        _logger.LogDebug("Elapsed Time: {Hours:00}:{Minutes:00}:{Seconds:00}.{Milliseconds:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        return Ok();

    }
}
