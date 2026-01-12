
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SimpleDnsServer;

namespace SimpleDnsServer
{
    // Entry point for running the server
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfigurationRoot config = CommandLineConfigurationExtensions.AddCommandLine((IConfigurationBuilder)new ConfigurationBuilder(), args).Build();
            var urls = new[]
            {
                DnsConst.ResolveHttpUrl(config),
                DnsConst.ResolveHttpsUrl(config),
                DnsConst.ResolveHttpUrlV6(config),
                DnsConst.ResolveHttpsUrlV6(config)
            };
            return GenericHostBuilderExtensions.ConfigureWebHostDefaults(Host.CreateDefaultBuilder(args), (Action<IWebHostBuilder>)(webBuilder =>
            {
                WebHostBuilderExtensions.UseStartup<Startup>(webBuilder);
                webBuilder.UseUrls(urls);
            }));
        }
    }
}


