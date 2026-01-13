using ARSoft.Tools.Net.Dns;

namespace DualstackDnsServer.Services;

public interface IDnsQueryHandler
{
    Task<DnsMessageBase?> HandleQueryAsync(DnsMessage query);
}
