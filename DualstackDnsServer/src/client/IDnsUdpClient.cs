namespace DualstackDnsServer.Client;

public interface IDnsUdpClient
{
    Task<string> QueryDnsAsync(string domain, CancellationToken cancellationToken = default);
    Task<string> QueryDnsAsync(string dnsServer, string domain, int port, QueryType type, CancellationToken cancellationToken = default);
    Task<string> QueryDnsIPv4Async(string dnsServer, string domain, int port, CancellationToken cancellationToken = default);
    Task<string> QueryDnsIPv6Async(string dnsServer, string domain, int port, CancellationToken cancellationToken = default);
}
