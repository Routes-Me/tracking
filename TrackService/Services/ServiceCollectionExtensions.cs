using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrackService.Helper;

namespace TrackService.Services
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddThreadStats(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, CoordinateChangeFeedbackBackgroundService>();
            services.AddSingleton<IHostedService, MonitorVehicles>();
            return services;
        }
    }
}
