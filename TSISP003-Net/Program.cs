using Microsoft.Extensions.Options;
using TSISP003.ProtocolUtils;
using TSISP003.Settings;
using TSISP003.SignControllerService;

namespace TSISP003
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();


            builder.Services.Configure<SignControllerServiceOptions>(builder.Configuration.GetSection("SignControllerServices"));
            builder.Services.AddSingleton<IHostedService, SignControllerServiceFactory>();


            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // if (app.Environment.IsDevelopment())
            // {
            //     app.UseSwagger();
            //     app.UseSwaggerUI();
            // }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}