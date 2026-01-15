namespace DualstackDnsServer;

#nullable disable

public static class DnsConst

{
    public const string DNS_SERVER_PROCESS_NAME = "DualstackDnsServer";
    public const string FRAMEWORK = "net8.0";
    public const string DncControllerName = "dns";
    public const string DNS_ROOT = "/" + DncControllerName;
    public const int UdpPort = 53; // use non-privileged port to avoid conflicts with system DNS
    public const int ApiHttp = 80;
    public const int ApiHttps = 443;

    // Try to increase UDP socket buffer size using reflection (ARSoft.Tools.Net does not expose Socket)
    public const int UDP_BUFFER = 8 * 1024 * 1024; //8MB

    private const string ipKey = "ip";
    private const string ip6Key = "ip6";
    private const string apiPortKey = "apiPort";
    private const string udpPortKey = "udpPort";
    public const string certPathKey = "cert";
    public const string certPasswordKey = "certPassw";

    private static readonly string[] verboseKeys = ["v", "verbose"];

    /// <summary>
    /// Checks if verbose mode is enabled in configuration (supports --v or --verbose)
    /// </summary>
    public static bool IsVerbose(IConfiguration config)
    {
        foreach (var key in verboseKeys)
        {
            var value = config[key];
            if (value != null)
            {
                // If --v or --verbose is present with no value, treat as false (default)
                if (string.IsNullOrEmpty(value))
                    return false;
                if (bool.TryParse(value, out var parsed))
                    return parsed;
                if (string.Equals(value, "1"))
                    return true;
            }
        }
        return false;
    }
    
        /// <summary>
    /// Gets the certificate path from configuration.
    /// </summary>
    public static string GetCertPath(IConfiguration config) => config[certPathKey];

    /// <summary>
    /// Gets the certificate password from configuration.
    /// </summary>
    public static string GetCertPassword(IConfiguration config) => config[certPasswordKey];
    
    //public static bool DEFAULT_ENABLE_HTTP = false;
    
    public static bool DEFAULT_ENABLE_HTTP {
    #if INTEGRATION_TESTS
        get { return true; }
    #else
        get { return false; }
    #endif
        } // HTTP enabled by default for tests

    /// <summary>
    /// Determines if HTTP endpoints should be enabled based on config/args. Default: disabled.
    /// Enable with --http, --http=true, or --http=1 (only double-dash supported)
    /// </summary>
    public static bool IsHttpEnabled(IConfiguration config, string[] args)
    {
        var httpValue = config["http"];
        if (httpValue != null)
        {
            // If --http is present with no value, treat as true
            if (string.IsNullOrEmpty(httpValue))
                return true;
            if (bool.TryParse(httpValue, out var parsed))
                return parsed;
            if (string.Equals(httpValue, "1"))
                return true;
        }
        return DEFAULT_ENABLE_HTTP;
    }

    public static string GetDnsIp(DnsIpMode mode = DnsIpMode.Localhost, IConfiguration config = null)
    {
        return mode switch
        {
            DnsIpMode.Any => "0.0.0.0",
            DnsIpMode.Localhost => "127.0.0.1",
            DnsIpMode.Custom => config?[ipKey] ?? "127.0.0.1",
            _ => "127.0.0.1"
        };
    }    

    public static string GetDnsIpV6(DnsIpMode mode = DnsIpMode.Localhost, IConfiguration config = null)
    {
        return mode switch
        {
            DnsIpMode.Any => "::",
            DnsIpMode.Localhost => "::1",
            DnsIpMode.Custom => config?[ip6Key] ?? "::1",
            _ => "::1"
        };
    }

    public static string GetDnsHostname(DnsIpMode mode = DnsIpMode.Localhost/*, IConfiguration config = null*/)
    {
        return mode switch
        {
            DnsIpMode.Any => "localhost",
            DnsIpMode.Localhost => "localhost",
            DnsIpMode.Custom => "mydns.local", // currenlty only statically, not possible to define via config
            _ => "localhost"
        };
    }

    public static string ResolveDnsIp(IConfiguration config)
    {
        // Always use Custom mode when config is provided
        return GetDnsIp(DnsIpMode.Custom, config);
    }

    public static string ResolveDnsIpV6(IConfiguration config)
    {
        // Always use Custom mode when config is provided
        return GetDnsIpV6(DnsIpMode.Custom, config);
    }

    public static string ResolveApiPort(IConfigurationRoot config) => config[apiPortKey] ?? ApiHttp.ToString();
    public static string ResolveUdpPort(IConfiguration config) => config[udpPortKey] ?? UdpPort.ToString();

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4423", Justification = "HTTP is used only for local development and testing; HTTPS is enforced in production.")]
    public static string ResolveHttpUrl(IConfigurationRoot config)
    {
#pragma warning disable S4423 // HTTP is used only for local development and testing; HTTPS is enforced in production.
        string ipRes = ResolveDnsIp(config);
        return $"http://{ipRes}:{ApiHttp}";
#pragma warning restore S4423
    }

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4423", Justification = "HTTP is used only for local development and testing; HTTPS is enforced in production.")]
    public static string ResolveHttpUrlV6(IConfigurationRoot config)
    {
#pragma warning disable S4423 // HTTP is used only for local development and testing; HTTPS is enforced in production.
    string ipRes = ResolveDnsIpV6(config);
    return $"http://[{ipRes}]:{ApiHttp}";
#pragma warning restore S4423
    }

    // Deprecated: Use dynamic port in Program.cs
    public static string ResolveHttpsUrlV6(IConfigurationRoot config)
    {
        string ipRes = ResolveDnsIpV6(config);
        return $"https://[{ipRes}]:{ApiHttps}";
    }
}