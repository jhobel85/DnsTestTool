
using Microsoft.AspNetCore.Server.Kestrel.Core;
using DualstackDnsServer;
using System.Net;

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

            // Warn on unsupported CLI keys using config's parsed keys                        
            bool printedHelp = false;
            var supportedKeys = DnsConst.SupportedKeys;
            // Only check keys that are present in the command line args (not config defaults)
            foreach (var arg in args)
            {
                if (!arg.StartsWith("--")) continue;
                var trimmed = arg.TrimStart('-');
                var key = trimmed.Split('=', 2, StringSplitOptions.RemoveEmptyEntries)[0];
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!supportedKeys.Contains(key))
                {
                    Console.WriteLine($"[WARN] Unsupported parameter '{key}' will be ignored.");
                    if (!printedHelp) {
                        Console.WriteLine("\nUsage: DualstackDnsServer [--ip <IPv4>] [--ip6 <IPv6>] [--portHttp <port>] [--portHttps <port>] [--portUdp <port>] [--http] [--cert <path>] [--certPassw <password>] [--v|--verbose]\n");
                        Console.WriteLine("  --ip           Bind IPv4 address (default: 127.0.0.1)");
                        Console.WriteLine("  --ip6          Bind IPv6 address (default: disabled)");
                        Console.WriteLine("  --portHttp     HTTP port (default: 80, only if --http)");
                        Console.WriteLine("  --portHttps    HTTPS port (default: 443)");
                        Console.WriteLine("  --portUdp      UDP DNS port (default: 53)");
                        Console.WriteLine("  --http         Enable HTTP endpoint (default: disabled)");
                        Console.WriteLine("  --cert         Path to HTTPS certificate");
                        Console.WriteLine("  --certPassw    Password for HTTPS certificate");
                        Console.WriteLine("  --v, --verbose Enable verbose logging");
                        printedHelp = true;
                    }
                }
            }

            string ReadIp(string key, string fallback, bool allowEmpty)
            {
                var raw = config[key];
                if (string.IsNullOrWhiteSpace(raw))
                    return allowEmpty ? string.Empty : fallback;
                if (IPAddress.TryParse(raw, out _))
                    return raw;
                Console.WriteLine($"[WARN] Invalid {key} address '{raw}', {(allowEmpty ? "disabling" : "falling back to " + fallback)}");
                return allowEmpty ? string.Empty : fallback;
            }

            int ReadPort(string key, int fallback)
            {
                var raw = config[key];
                if (string.IsNullOrWhiteSpace(raw))
                    return fallback;
                if (int.TryParse(raw, out var val))
                    return val;
                Console.WriteLine($"[WARN] Invalid {key} '{raw}', falling back to {fallback}");
                return fallback;
            }

            // Resolve IPs and ports with validation and warnings
            string ip = ReadIp("ip", DnsConst.GetDnsIp(), allowEmpty: false);
            string ipV6 = ReadIp("ip6", string.Empty, allowEmpty: true);
            int httpsPortValue = ReadPort("portHttps", DnsConst.PortHttps);
            int httpPortValue = ReadPort("portHttp", DnsConst.PortHttp);
            int udpPortValue = ReadPort("portUdp", DnsConst.PortUdp);

            var serverOptions = new ServerOptions
            {
                Ip = ip,
                IpV6 = ipV6,
                // Default HTTPS port is 443; HTTP (if enabled) defaults to 80
                HttpsPort = httpsPortValue,
                HttpPort = httpPortValue,
                UdpPort = udpPortValue,
                CertPath = DnsConst.GetCertPath(config),
                CertPassword = DnsConst.GetCertPassword(config),
                EnableHttp = DnsConst.IsHttpEnabled(config, args),
                IsVerbose = DnsConst.IsVerbose(config),
                Args = args
            };
            
            var urlList = new List<string>();
            bool httpEnabled = DnsConst.IsHttpEnabled(config, args);
            var httpsPort = serverOptions.HttpsPort;
            var httpPort = serverOptions.HttpPort;
            if (httpEnabled)
            {
                // HTTP URLs (for dev/testing)
                urlList.Add($"http://{ip}:{httpPort}");
                if (!string.IsNullOrWhiteSpace(ipV6))
                {
                    urlList.Add($"http://[{ipV6}]:{httpPort}");
                }
            }
            // HTTPS URLs (production/dev)
            urlList.Add($"https://{ip}:{httpsPort}");
            if (!string.IsNullOrWhiteSpace(ipV6))
            {
                urlList.Add($"https://[{ipV6}]:{httpsPort}");
            }
            var urls = urlList.ToArray();

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
                            try
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
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Failed to bind {scheme.ToUpper()} endpoint {addr}:{port} - {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                    }
                });
            }));
        }
    }
}


