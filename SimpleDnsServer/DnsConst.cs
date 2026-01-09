namespace SimpleDnsServer;

#nullable disable

public static class DnsConst
{
    public const string DNS_SERVER_PROCESS_NAME = "SimpleDnsServer";
    public const string FRAMEWORK = "net8.0";
    public const string DncControllerName = "dns";
    public const string DNS_ROOT = "/" + DncControllerName;
    public const int UdpPort = 53;
    public const int ApiPort = 60;

    // Try to increase UDP socket buffer size using reflection (ARSoft.Tools.Net does not expose Socket)
    public const int UDP_BUFFER = 8 * 1024 * 1024; //8MB

    public enum DnsIpMode
    {
        Any,
        Localhost,
        Custom
    }

    private const string ipKey = "ip";
    private const string ip6Key = "ip6";
    private const string apiPortKey = "apiPort";
    private const string udpPortKey = "udpPort";

    public static string GetDnsIp(DnsIpMode mode, IConfiguration config)
    {
        return mode switch
        {
            DnsIpMode.Any => "0.0.0.0",
            DnsIpMode.Localhost => "127.0.0.1",
            DnsIpMode.Custom => config[ipKey] ?? "127.0.0.1",
            _ => "127.0.0.1"
        };
    }

    public static string GetDnsIpV6(DnsIpMode mode, IConfiguration config)
    {
        return mode switch
        {
            DnsIpMode.Any => "::",
            DnsIpMode.Localhost => "::1",
            DnsIpMode.Custom => config[ip6Key] ?? "::1",
            _ => "::1"
        };
    }

    public static string ResolveDnsIp(IConfiguration config)
    {
        var modeStr = config["ipMode"];
        DnsIpMode mode = Enum.TryParse(modeStr, out DnsIpMode parsed) ? parsed : DnsIpMode.Localhost;
        return GetDnsIp(mode, config);
    }

    public static string ResolveDnsIpV6(IConfiguration config)
    {
        var modeStr = config["ip6Mode"];
        DnsIpMode mode = Enum.TryParse(modeStr, out DnsIpMode parsed) ? parsed : DnsIpMode.Localhost;
        return GetDnsIpV6(mode, config);
    }

    public static string ResolveApiPort(IConfigurationRoot config) => config[apiPortKey] ?? ApiPort.ToString();
    public static string ResolveUdpPort(IConfiguration config) => config[udpPortKey] ?? UdpPort.ToString();

    public static string ResolveUrl(IConfigurationRoot config)
    {
        string ipRes = ResolveDnsIp(config);
        string port = config[apiPortKey] ?? ApiPort.ToString();
        return $"http://{ipRes}:{port}";
    }

    public static string ResolveUrlV6(IConfigurationRoot config)
    {
        string ipRes = ResolveDnsIpV6(config);
        string port = config[apiPortKey] ?? ApiPort.ToString();
        return $"http://[{ipRes}]:{port}";
    }
}