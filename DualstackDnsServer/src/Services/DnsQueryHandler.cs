using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System.Net;

namespace DualstackDnsServer.Services;

public class DnsQueryHandler : IDnsQueryHandler
{
    private readonly IDnsRecordManger recordManager;
    private readonly ILogger logger;

    public DnsQueryHandler(IDnsRecordManger recordManager, ILogger logger)
    {
        this.recordManager = recordManager;
        this.logger = logger;
    }

    public Task<DnsMessageBase?> HandleQueryAsync(DnsMessage query)
    {
        // Only log result, not every step
        DnsMessage responseInstance = query.CreateResponseInstance();
        if (query.Questions.Count == 1)
        {
            string str = query.Questions[0].Name.ToString();
            string? ipString = recordManager.Resolve(str);
            if (ipString != null)
            {
                responseInstance.ReturnCode = ReturnCode.NoError;
                var ip = IPAddress.Parse(ipString);
                if (query.Questions[0].RecordType == RecordType.A)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        responseInstance.AnswerRecords.Add(new ARecord(DomainName.Parse(str), 3600, ip));
                        logger.LogDebug($"DNS resolved: {str} -> {ip} (A)");
                    }
                }
                else if (query.Questions[0].RecordType == RecordType.Aaaa)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        responseInstance.AnswerRecords.Add(new AaaaRecord(DomainName.Parse(str), 3600, ip));
                        logger.LogDebug($"DNS resolved: {str} -> {ip} (AAAA)");
                    }
                }
                // No else: do not add a record if the address family does not match!
            }
            else
            {
                responseInstance.ReturnCode = ReturnCode.NxDomain;
                logger.LogDebug($"DNS not resolved: {str} (NXDOMAIN)");
            }
        }
        else
        {
            responseInstance.ReturnCode = ReturnCode.ServerFailure;
            // No extra debug log for multiple questions
        }
        // Only log result, not every step
        return Task.FromResult<DnsMessageBase?>(responseInstance);
    }
}
