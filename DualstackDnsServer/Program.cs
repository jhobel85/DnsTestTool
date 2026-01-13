
using Microsoft.AspNetCore.Server.Kestrel.Core;
using DualstackDnsServer;

namespace DualstackDnsServer
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
            var urlList = new List<string>();
            if (DnsConst.IsHttpEnabled(config, args))
            {
                urlList.Add(DnsConst.ResolveHttpUrl(config));
                urlList.Add(DnsConst.ResolveHttpUrlV6(config));
            }
            urlList.Add(DnsConst.ResolveHttpsUrl(config));
            urlList.Add(DnsConst.ResolveHttpsUrlV6(config));
            var urls = urlList.ToArray();

            // Read certPath and certPassword from args or config
            string certPath = DnsConst.GetCertPath(config);
            string certPassword = DnsConst.GetCertPassword(config);

            return GenericHostBuilderExtensions.ConfigureWebHostDefaults(Host.CreateDefaultBuilder(args), (Action<IWebHostBuilder>)(webBuilder =>
            {
                WebHostBuilderExtensions.UseStartup<Startup>(webBuilder);
                webBuilder.ConfigureKestrel((context, kestrelOptions) =>
                {
                    foreach (var url in urls)
                    {
                        var uri = new Uri(url);
                        var port = uri.Port;
                        var host = uri.Host;
                        var scheme = uri.Scheme;

                        // Resolve host to IP(s); bind only to explicit IPs
                        var addresses = new List<System.Net.IPAddress>();
                        if (System.Net.IPAddress.TryParse(host, out var parsedIp))
                        {
                            addresses.Add(parsedIp);
                        }
                        else
                        {
                            try
                            {
                                addresses.AddRange(System.Net.Dns.GetHostAddresses(host));
                            }
                            catch
                            {
                                // Cannot resolve hostname; skip binding
                                continue;
                            }
                        }

                        foreach (var addr in addresses)
                        {
                            kestrelOptions.Listen(addr, port, listenOptions =>
                            {
                                if (scheme == "https")
                                {
                                    if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
                                    {
                                        listenOptions.UseHttps(certPath, certPassword);
                                    }
                                    else
                                    {
                                        listenOptions.UseHttps(); // fallback to default cert (e.g., dev cert)
                                    }
                                }
                            });
                        }
                    }
                });
            }));
        }
    }
}


