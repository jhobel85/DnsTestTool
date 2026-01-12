using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Microsoft.Extensions.Logging;
using SimpleDnsServer.Utils;

#nullable enable //compiler will warn if might be dereferencing a variable that could be null
namespace SimpleDnsServer;


public class DnsUdpListener : BackgroundService
{
    private readonly DnsServer udpServer;
    private readonly IDnsQueryHandler queryHandler;
    private static readonly SemaphoreSlim QuerySemaphore = new(16); // e.g., max 16 concurrent queries
    private readonly ILogger<DnsUdpListener> _logger;

    public DnsUdpListener(IDnsQueryHandler queryHandler, IConfiguration config, ILogger<DnsUdpListener> logger)
    {
        string ipString = DnsConst.ResolveDnsIp(config);
        string ipStringV6 = DnsConst.ResolveDnsIpV6(config);
        int port = int.Parse(DnsConst.ResolveUdpPort(config));
        this.queryHandler = queryHandler;
        _logger = logger;

        // Best effort dual-stack: bind both IPv4 and IPv6 endpoints
        var transportV4 = new UdpServerTransport(new IPEndPoint(IPAddress.Parse(ipString), port));
        var transportV6 = new UdpServerTransport(new IPEndPoint(IPAddress.Parse(ipStringV6), port));

        try
        {
            var socketField = typeof(UdpServerTransport).GetField("_udpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (socketField != null)
            {
                var udpClientV4 = socketField.GetValue(transportV4) as UdpClient;
                var udpClientV6 = socketField.GetValue(transportV6) as UdpClient;

                udpClientV4?.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, DnsConst.UDP_BUFFER);
                udpClientV6?.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, DnsConst.UDP_BUFFER);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DnsUdpListener] Could not set UDP socket buffer size");
        }

        udpServer = new DnsServer([transportV4, transportV6]);
        udpServer.QueryReceived += new AsyncEventHandler<QueryReceivedEventArgs>(OnQueryReceived);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
            udpServer.Start();
        else
            udpServer.Stop();

        // Keep the background service alive until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
        await QuerySemaphore.WaitAsync();
        try
        {
            if (e.Query is not DnsMessage query)
                return;
            try
            {
                var response = await queryHandler.HandleQueryAsync(query);
                e.Response = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DnsUdpListener] Exception in query handler");
            }
        }
        finally
        {
            QuerySemaphore.Release();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        udpServer?.Stop();
    }
}
