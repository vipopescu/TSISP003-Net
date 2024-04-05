using Microsoft.AspNetCore.Mvc;

namespace TSISP003.Controllers;

[ApiController]
[Route("api")]
public class SignApiController : ControllerBase
{
    private readonly ILogger<SignApiController> _logger; // I am unsure whether injecting ILogger here is best practice

    public SignApiController(ILogger<SignApiController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [Route("{device}/SystemReset")]
    public async Task<IActionResult> SystemReset(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/SignSetTextFrame")]
    public async Task<IActionResult> SignSetTextFrame(string device)
    {
        // TODO: Implement
        return Ok();
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

    [HttpPost]
    [Route("{device}/SignConfigurationRequest")]
    public async Task<IActionResult> SignConfigurationRequest(string device)
    {
        // TODO: Implement
        return Ok();
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
    public async Task<IActionResult> SignSetMessage(string device)
    {
        // TODO: Implement
        return Ok();
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
    public async Task<IActionResult> SignDisplayMessage(string device)
    {
        // TODO: Implement
        return Ok();
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
    public async Task<IActionResult> SignRequestStoredFrameMessagePlan(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/SignExtendedStatusRequest")]
    public async Task<IActionResult> SignExtendedStatusRequest(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/RetrieveFaultLog")]
    public async Task<IActionResult> RetrieveFaultLog(string device)
    {
        // TODO: Implement
        return Ok();
    }

    [HttpPost]
    [Route("{device}/ResetFaultLog")]
    public async Task<IActionResult> ResetFaultLog(string device)
    {
        // TODO: Implement
        return Ok();
    }
}
