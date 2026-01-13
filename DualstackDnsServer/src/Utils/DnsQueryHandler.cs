namespace DualstackDnsServer.Utils;

using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DualstackDnsServer.Services;
using System.Net;

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
        logger.LogDebug("Received DNS query: {QueryName}", query.Questions.FirstOrDefault()?.Name);
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
                        responseInstance.AnswerRecords.Add(new ARecord(DomainName.Parse(str), 3600, ip));
                }
                else if (query.Questions[0].RecordType == RecordType.Aaaa)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        responseInstance.AnswerRecords.Add(new AaaaRecord(DomainName.Parse(str), 3600, ip));
                }
                else
                {
                    responseInstance.ReturnCode = ReturnCode.ServerFailure;
                }
            }
            else
            {
                responseInstance.ReturnCode = ReturnCode.NxDomain;
            }
        }
        else
        {
            responseInstance.ReturnCode = ReturnCode.ServerFailure;
        }
        logger.LogDebug("DNS response: {AnswerCount} answers", responseInstance.AnswerRecords.Count);
        return Task.FromResult<DnsMessageBase?>(responseInstance);
    }
}
