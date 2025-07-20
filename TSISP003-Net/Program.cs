using TSISP003_Net.Settings;
using TSISP003_Net.SignControllerService;

namespace TSISP003_Net;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();


        builder.Services.Configure<SignControllerServiceOptions>(builder.Configuration.GetSection("SignControllerServices"));
        builder.Services.AddSingleton<SignControllerServiceFactory>();

        builder.Services.AddHostedService(provider => provider.GetRequiredService<SignControllerServiceFactory>());


        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
