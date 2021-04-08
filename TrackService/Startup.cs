using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackService.IServices;
using TrackService.Services;
using TrackService.RethinkDb_Changefeed.Model;
using TrackService.RethinkDb_Changefeed;
using TrackService.Helper.CronJobServices;
using TrackService.Helper.CronJobServices.CronJobExtensionMethods;
using TrackService.RethinkDb_Changefeed.Model.Common;
using Microsoft.AspNetCore.Mvc;
using TrackService.RethinkDb_Changefeed.DataAccess.Abstraction;
using TrackService.RethinkDb_Changefeed.DataAccess.Repository;
using TrackService.Abstraction;
using TrackService.Repository;

namespace TrackService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            #region RethinkDB
            Console.WriteLine(Configuration.GetSection("RethinkDbDev").GetValue<String>("Host"));
            services.AddRethinkDb(options =>
            {
                options.Host = Configuration.GetSection("RethinkDbDev").GetValue<String>("Host"); // "172.17.0.6";
            });
            #endregion
            services.AddServerSentEvents();

            services.AddSingleton<IRethinkDbConnectionFactory, RethinkDbConnectionFactory>();
            services.AddSingleton<IRethinkDbStore, RethinkDbStore>();
            services.AddSingleton<IDataAccessRepository, DataAccessRepository>();
            services.AddSingleton<ILocationFeedsRepository, LocationFeedsRepository>();
            
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            services.AddSignalR();
            services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 10240 * 3;  // bytes
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(3);
                hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(6);
                hubOptions.EnableDetailedErrors = true;
            });

            services.AddCronJob<SyncCoordinates>(c =>
            {
               c.TimeZoneInfo = TimeZoneInfo.Utc;
               c.CronExpression = @"0 * * * *"; // Runs every hour
            });

            services.AddCronJob<SyncVehicles>(c =>
            {
               c.TimeZoneInfo = TimeZoneInfo.Utc;
               c.CronExpression = @"0 * * * *"; // Runs every hour
            });

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            var dependenciessSection = Configuration.GetSection("Dependencies");
            services.Configure<Dependencies>(dependenciessSection);

            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
            });

            services.Configure<RethinkDbOptions>(Configuration.GetSection("RethinkDbDev"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IRethinkDbConnectionFactory connectionFactory, IRethinkDbStore store)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials()
            );
            store.InitializeDatabase();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TrackServiceHubNew>("/trackServiceHub");
            });
        }
    }
}
