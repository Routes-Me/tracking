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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            services.AddHttpClient<ICheckinService, CheckinService>();
                
            //     client =>
            // {
            //     client.BaseAddress = new Uri("https://stage.api.routesme.com/v1.0/checkins"); // Configuration["BaseUrl"]
            // });
            services.AddControllers();
            

            // #region RethinkDB
            // Console.WriteLine(Configuration.GetSection("Rethinkdb").GetValue<String>("Host"));
            // services.AddRethinkDb(options =>
            // {
            //     options.Host = Configuration.GetSection("Rethinkdb").GetValue<String>("Host"); // "172.17.0.6";
            // });
            // #endregion
            // services.AddServerSentEvents();

            // services.AddSingleton<IRethinkDbConnectionFactory, RethinkDbConnectionFactory>();
            // services.AddSingleton<IRethinkDbStore, RethinkDbStore>();
            // services.AddSingleton<IDataAccessRepository, DataAccessRepository>();
            // services.AddSingleton<ILocationFeedsRepository, LocationFeedsRepository>();

            // services.AddHostedService<QueuedHostedService>();
            // services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });



            var dependenciessSection = Configuration.GetSection("Dependencies");
            services.Configure<Dependencies>(dependenciessSection);

            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
            });

            // services.Configure<RethinkDbOptions>(Configuration.GetSection("Rethinkdb"));

            #region AppSettings
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            #endregion


            #region JWT

            JwtBearerEvents jwtBearerEvents = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    logger.LogError("Authentication failed.", context.Exception);
                    context.Response.StatusCode = 401;
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    context.Response.OnStarting(async () =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("Authentication failed.");
                    });
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                    {
                        string accessToken = context.Request.Query["access_token"];
                        Console.WriteLine("########################################");
                        Console.WriteLine(accessToken);
                        Console.WriteLine("########################################");
                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/trackServiceHub")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                    // OnChallenge = context =>
                    // {
                    //     var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    //     logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);
                    //     return Task.CompletedTask;
                    // },
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidAudiences = new List<string>
                    {
                        appSettings.DashboardAudience,
                        appSettings.ScreenAudience,
                        appSettings.RoutesAppAudience,
                        appSettings.BusValidatorAudience
                    },
                    ValidIssuer = appSettings.SessionTokenIssuer,
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettings.AccessSecretKey)),
                    // verify signature to avoid tampering
                    ValidateLifetime = false, // validate the expiration
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.FromMinutes(5) // tolerance for the expiration date
                };
                x.Events = jwtBearerEvents;
            });
            #endregion

            services.AddSignalR();
            services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 10240 * 3;  // bytes
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(3);
                hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(6);
                hubOptions.EnableDetailedErrors = true;
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
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
            // store.InitializeDatabase();
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
