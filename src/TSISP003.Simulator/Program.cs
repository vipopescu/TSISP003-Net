using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TSISP003.Simulator.Configuration;
using TSISP003.Simulator.Tcp;

var builder = Host.CreateApplicationBuilder(args);

var options = new SimulatorOptions();
builder.Configuration.GetSection("Simulator").Bind(options);
builder.Services.AddSingleton(options);
builder.Services.AddHostedService<SimulatorListener>();

await builder.Build().RunAsync();
