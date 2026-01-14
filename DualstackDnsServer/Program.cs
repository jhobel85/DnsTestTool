
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
            // Populate ServerOptions
            var ip = DnsConst.ResolveDnsIp(config);
            var ipV6 = DnsConst.ResolveDnsIpV6(config);
            // Check if the IPs are assigned to any local adapter
            bool ipv4Ok = string.IsNullOrWhiteSpace(ip) || ip == "0.0.0.0" || ip == "127.0.0.1" || System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Any(addr => addr.Address.ToString() == ip);
            bool ipv6Ok = string.IsNullOrWhiteSpace(ipV6) || ipV6 == "::" || ipV6 == "::1" || System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Any(addr => addr.Address.ToString() == ipV6);
            if (!ipv4Ok)
                Console.WriteLine($"[WARNING] The IPv4 address '{ip}' is not assigned to any local network adapter. You may get a SocketException (10049). Use 'ipconfig' to see your assigned IPs.");
            if (!ipv6Ok)
                Console.WriteLine($"[WARNING] The IPv6 address '{ipV6}' is not assigned to any local network adapter. You may get a SocketException (10049). Use 'ipconfig' to see your assigned IPs.");
            var serverOptions = new ServerOptions
            {
                Ip = ip,
                IpV6 = ipV6,
                ApiPort = int.TryParse(DnsConst.ResolveApiPort(config), out var apiPort) ? apiPort : DnsConst.ApiHttp,
                UdpPort = int.TryParse(DnsConst.ResolveUdpPort(config), out var udpPort) ? udpPort : DnsConst.UdpPort,
                CertPath = DnsConst.GetCertPath(config),
                CertPassword = DnsConst.GetCertPassword(config),
                EnableHttp = DnsConst.IsHttpEnabled(config, args),
                IsVerbose = DnsConst.IsVerbose(config),
                Args = args
            };
            
            var urlList = new List<string>();
            bool httpEnabled = DnsConst.IsHttpEnabled(config, args);
            urlList.Add(DnsConst.ResolveHttpsUrl(config));
            urlList.Add(DnsConst.ResolveHttpsUrlV6(config));
            var urls = urlList.ToArray();
            Console.WriteLine("[INFO] Binding endpoints:");
            foreach (var u in urls) Console.WriteLine($"  {u}");

            // Read certPath and certPassword from args or config
            string certPath = serverOptions.CertPath;
            string certPassword = serverOptions.CertPassword;

            var builder = Host.CreateDefaultBuilder(args);
                if (serverOptions.IsVerbose)
                {
                    builder.ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Debug);
                        logging.AddFilter((category, level) => level >= LogLevel.Debug);
                    });
                    Console.WriteLine("[INFO] Verbose logging enabled (Debug level)");
                }
            return GenericHostBuilderExtensions.ConfigureWebHostDefaults(builder, (Action<IWebHostBuilder>)(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(serverOptions);
                });
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


