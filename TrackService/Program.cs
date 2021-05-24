using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TrackService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string standardVersion = "Standard version: " + "{0}.{1}.{2}";
            Version standard = new Version(1, 0, 0);
            Console.WriteLine(standardVersion, standard.Major, standard.Minor, standard.Build);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
