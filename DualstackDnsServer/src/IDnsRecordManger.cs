namespace DualstackDnsServer;

using DualstackDnsServer.RestApi;
using System.Threading;
using System.Threading.Tasks;

public interface IDnsRecordManger
{
    void Register(string domain, string ip, string? sessionId = null);
    void Unregister(string domain);
    string? Resolve(string domain);
    int GetCount();
    int GetSessionCount(string sessionId);
    void UnregisterSession(string sessionId);
    void UnregisterAll();

    IEnumerable<DnsEntryDto> GetAllEntries();
}
