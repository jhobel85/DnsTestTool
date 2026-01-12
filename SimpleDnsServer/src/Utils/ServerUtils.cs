using System.Diagnostics;
using static SimpleDnsServer.DnsConst;

namespace SimpleDnsServer.Utils;

public class ServerUtils
{
    [Obsolete("Use IServerManager and DefaultServerManager instead.")]
    public static void StartDnsServer()
        => new DefaultServerManager().StartDnsServer();

    [Obsolete("Use IServerManager and DefaultServerManager instead.")]
    public static void StartDnsServer(string ip, string ip6, int apiPort, int udpPort)
        => new DefaultServerManager().StartDnsServer(ip, ip6, apiPort, udpPort);

    [Obsolete("Use IServerManager and DefaultServerManager instead.")]
    public static void StartDnsServer(string serverExe, string ip, string ip6, int apiPort, int udpPort)
        => new DefaultServerManager().StartDnsServer(serverExe, ip, ip6, apiPort, udpPort);
}
