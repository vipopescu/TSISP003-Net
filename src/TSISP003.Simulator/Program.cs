using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Tcp;

// Root configuration at the binary's directory (where appsettings.json is copied)
// rather than the current working directory, so the controller list loads no
// matter where the process is launched from (dotnet run, Aspire, published exe).
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Bind the list of controllers to host. Fall back to a single default controller
// if none are configured.
var host = new SimulatorHostOptions();
builder.Configuration.GetSection("Simulator").Bind(host);
if (host.Controllers.Count == 0)
    host.Controllers.Add(new SimulatorOptions());

// One TCP listener (with its own in-memory store) per controller.
foreach (var controller in host.Controllers)
{
    builder.Services.AddSingleton<IHostedService>(sp =>
        new SimulatorListener(controller, sp.GetRequiredService<ILogger<SimulatorListener>>()));
}

await builder.Build().RunAsync();
