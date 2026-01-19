using DualstackDnsServer;
using DualstackDnsServer.Client;
using DualstackDnsServer.Services;
using DualstackDnsServer.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading;

namespace DualstackDnsServer.RestApi;

[ApiController]
[Route("dns/query")]
public class DnsQueryController : ControllerBase
{
    private readonly IDnsUdpClient _dnsUdpClient;
    private readonly ServerOptions? _serverOptions;
    private readonly ILogger<DnsQueryController> _logger;

    public DnsQueryController(IDnsUdpClient dnsUdpClient, ServerOptions? serverOptions = null)
    {
        _dnsUdpClient = dnsUdpClient;
        _serverOptions = serverOptions;
        // ILogger will be injected by DI
        _logger = (ILogger<DnsQueryController>?)AppDomain.CurrentDomain.GetData("DnsQueryControllerLogger");
    }
    
    /// <summary>
    /// Queries a DNS server for the specified domain using UDP (A or AAAA record).
    /// </summary>
    /// <param name="domain">Domain name to resolve.</param>
    /// <param name="type">Record type: AAAA or A.</param>
    /// <returns>Resolved IP address or error message.</returns>
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Query(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return BadRequest("Domain is required.");

        // If DNS server is not configured, treat as BadRequest
        if (_serverOptions == null || (string.IsNullOrWhiteSpace(_serverOptions.Ip) && string.IsNullOrWhiteSpace(_serverOptions.IpV6)))
            return BadRequest("DNS server is required.");

        try
        {
            // Service already tries AAAA then A; call once and classify the result
            string resolvedIp = await _dnsUdpClient.QueryDnsAsync(domain, cancellationToken);
            if (string.IsNullOrWhiteSpace(resolvedIp))
                return NotFound($"No IP found for domain {domain}.");

            if (IPAddress.TryParse(resolvedIp, out var parsedIp))
            {
                return parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                    ? Ok(new { IPv6 = resolvedIp })
                    : Ok(new { IPv4 = resolvedIp });
            }

            return Ok(new { IP = resolvedIp });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"DNS query failed: {ex.Message}");
        }
    }

        /// <summary>
    /// Queries a DNS server for the specified domain using UDP (A or AAAA record).
    /// </summary>
    /// <param name="domain">Domain name to resolve.</param>
    /// <param name="dnsServer">DNS server IP address (default: 8.8.8.8).</param>
    /// <param name="port">DNS server port (default: 53).</param>
    /// <param name="type">Record type: A or AAAA (default: A).</param>
    /// <returns>Resolved IP address or error message.</returns>
    [HttpGet("server")]
    public async Task<IActionResult> QueryWithServer(string domain, string dnsServer, int port, string type = "A", CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("[DnsQueryController] Entered Query(domain={domain}, dnsServer={dnsServer}, port={port}, type={type})", domain, dnsServer, port, type);
        _logger?.LogInformation("[DnsQueryController] Entered Query action with domain={domain}, dnsServer={dnsServer}, port={port}, type={type}", domain, dnsServer, port, type);
        if (string.IsNullOrWhiteSpace(domain))
            return BadRequest("Domain is required.");
        
        if (string.IsNullOrWhiteSpace(dnsServer))
            return BadRequest("DNS server is required.");

        try
        {
            _logger?.LogInformation("[DnsQueryController] Before QueryDnsAsync");
            QueryType qtype = type.ToUpper() == "AAAA" ? QueryType.AAAA : QueryType.A;
            var queryTask = _dnsUdpClient.QueryDnsAsync(dnsServer, domain, port, qtype, cancellationToken);
            _logger?.LogInformation("[DnsQueryController] Awaiting QueryDnsAsync");
            string ip = await queryTask;
            _logger?.LogInformation("[DnsQueryController] After QueryDnsAsync, ip={ip}", ip);
            if (string.IsNullOrWhiteSpace(ip))
                return NotFound($"No IP found for domain {domain}.");
            return Ok(ip);
        }
        catch (Exception ex)
        {
            var errorDetails = $"DNS query failed: {ex.Message}\nInner: {ex.InnerException?.Message}\nStack: {ex.StackTrace}";
            _logger?.LogError(ex, errorDetails);
            return StatusCode(500, errorDetails);
        }
    }    
}
