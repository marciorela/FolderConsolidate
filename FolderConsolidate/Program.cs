using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MR.Config;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FolderConsolidate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Config.CfgFileName())
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithAssemblyVersion()
                .Enrich.WithMemoryUsage()
                .WriteTo.Async(w =>
                {
                    w.File(
                        System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\logs\\log_.txt",
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 1024 * 1024 * 20,
                        buffered: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1),
                        rollOnFileSizeLimit: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u4}] | {AssemblyVersion} | {MemoryUsage} | {Message:l}{NewLine}{Exception}"
                     );
                    //w.File(new CompactJsonFormatter(), System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\logs\\log_.txt", rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 1024 * 1024 * 20, buffered: true, flushToDiskInterval: TimeSpan.FromSeconds(1), rollOnFileSizeLimit: true);
                }, bufferSize: 500)
                .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging(logging =>
                {
                    logging.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
