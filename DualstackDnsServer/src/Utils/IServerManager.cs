namespace DualstackDnsServer.Utils;

public interface IServerManager
{
    void StartDnsServer();
    void StartDnsServer(string ip, string ip6, int apiPort, int udpPort, bool httpEnabled, string? cert = null, string? certPass = null);
    void StartDnsServer(string serverExe, string ip, string ip6, int apiPort, int udpPort, bool httpEnabled, string? cert = null, string? certPass = null);
}
