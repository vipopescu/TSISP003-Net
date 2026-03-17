using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TSISP003.Application.Interfaces;
using TSISP003.Infrastructure.Configuration;
using TSISP003.Infrastructure.Services;

namespace TSISP003.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SignControllerServiceOptions>(configuration.GetSection("SignControllerServices"));
        services.AddSingleton<SignControllerServiceFactory>();
        services.AddSingleton<ISignControllerServiceFactory>(provider => provider.GetRequiredService<SignControllerServiceFactory>());
        services.AddHostedService(provider => provider.GetRequiredService<SignControllerServiceFactory>());

        return services;
    }
}
