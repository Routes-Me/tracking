using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.Model;

namespace TrackService.RethinkDb_Changefeed
{
    public static class RethinkDbServiceCollectionExtensions
    {
        public static IServiceCollection AddRethinkDb(this IServiceCollection services, Action<RethinkDbOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);
            services.TryAddSingleton<IRethinkDbSingletonProvider, RethinkDbSingletonProvider>();
            services.TryAddTransient<ICoordinateChangeFeedbackBackgroundService, ThreadStatsRethinkDbService>();

            return services;
        }
    }
}
