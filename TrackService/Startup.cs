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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TrackService.Helper;

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

            services.AddSingleton<IRethinkDbConnectionFactory, RethinkDbConnectionFactory>();
            services.AddSingleton<IRethinkDbStore, RethinkDbStore>();
            services.AddSingleton<TrackServiceHub, TrackServiceHub>();

            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            services.AddSignalR();
            services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 1024;  // bytes
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(3);
                hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(6);
                hubOptions.EnableDetailedErrors = true;
            });

            services.AddCronJob<SyncCoordinates>(c =>
            {
                c.TimeZoneInfo = TimeZoneInfo.Utc;
                c.CronExpression = @"0 1 */1 * * "; // Run every day at 1 AM
            });

            services.AddCronJob<SyncVehicles>(c =>
            {
                c.TimeZoneInfo = TimeZoneInfo.Utc;
                c.CronExpression = @"0 3 */7 * * "; // Run every 7 day at 3 AM
            });
            
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            var dependenciessSection = Configuration.GetSection("Dependencies");
            services.Configure<Dependencies>(dependenciessSection);

            services.Configure<RethinkDbOptions>(Configuration.GetSection("RethinkDbDev"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = appSettings.ValidAudience,
                    ValidIssuer = appSettings.ValidIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Secret)),
                    // verify signature to avoid tampering
                    ValidateLifetime = true, // validate the expiration
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMilliseconds(0) // tolerance for the expiration date
                };
                x.Events = new JwtBearerEvents
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
                            ErrorMessage errorMessage = new ErrorMessage();
                            errorMessage.Message = "Authentication failed.";
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(errorMessage));
                        });
                        return Task.CompletedTask;
                    },

                    OnMessageReceived = context =>
                    {
                        string accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/trackServiceHub")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                        logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TrackServiceHub>("/trackServiceHub");
            });
        }
    }
}
