using TSISP003.Application.Interfaces;
using TSISP003.Application.Services;
using TSISP003.Infrastructure;
using TSISP003.ServiceDefaults;

namespace TSISP003.Api;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddControllers();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Extended (non-protocol) management operations. Singleton: owns rolling per-device IDs.
        builder.Services.AddSingleton<IExtendedSignService, ExtendedSignService>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
