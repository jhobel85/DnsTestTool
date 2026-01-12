namespace SimpleDnsServer.Utils;

public interface IServerManager
{
    void StartDnsServer();
    void StartDnsServer(string ip, string ip6, int apiPort, int udpPort);
    void StartDnsServer(string serverExe, string ip, string ip6, int apiPort, int udpPort);
}
