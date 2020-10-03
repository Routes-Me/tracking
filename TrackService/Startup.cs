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
            services.AddThreadStats();

            //services.AddCronJob<SyncCoordinates>(c =>
            //{
            //    c.TimeZoneInfo = TimeZoneInfo.Local;
            //    c.CronExpression = @"0 1 */1 * * ";
            //    //c.CronExpression = @"* * */1 * * ";
            //});

            //services.AddCronJob<SyncVehicles>(c =>
            //{
            //    c.TimeZoneInfo = TimeZoneInfo.Local;
            //    //c.CronExpression = @"4 13 * * * "; 
            //    c.CronExpression = @"0 2 */7 * * "; // Run every 7 days at 2 AM
            //});

            services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 1024;  // bytes
                hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(3);
                hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(6);
            });
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var dependenciessSection = Configuration.GetSection("Dependencies");
            services.Configure<Dependencies>(dependenciessSection);

            services.Configure<RethinkDbOptions>(Configuration.GetSection("RethinkDbDev"));
            services.AddSingleton<IRethinkDbConnectionFactory, RethinkDbConnectionFactory>();

            services.AddSingleton<IRethinkDbStore, RethinkDbStore>();
            services.AddSingleton<TrackServiceHub, TrackServiceHub>();
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });
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
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TrackServiceHub>("/trackServiceHub");
            });
        }
    }
}
