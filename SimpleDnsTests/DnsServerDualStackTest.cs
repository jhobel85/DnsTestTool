using System.Net;
using System.Threading.Tasks;
using Xunit;
using SimpleDnsClient;

namespace SimpleDnsServer.Tests
{    
    public class DnsServerDualStackTest(DnsServerFixture fixture) : IClassFixture<DnsServerFixture>
    {
        private const string TestDomain_V4 = "dualstack4.local";
        private const string TestIp_V4 = "192.168.1.101";
        private const string TestDomain_V6 = "dualstack6.local";
        private const string TestIp_V6 = "fd00::101";
        private readonly DnsServerFixture _fixture = fixture;  
        
        [Fact]
        public async Task RegisterAndResolve_BothIPv4AndIPv6_Success()
        {
            // Arrange
            string dns_ip_v4 = DnsConst.DNS_IP;
            string dns_ip_v6 = DnsConst.DNS_IPv6;
            var dnsClientV4 = new RestClient(dns_ip_v4, DnsConst.ApiPort);
            var dnsClientV6 = new RestClient(dns_ip_v6, DnsConst.ApiPort, useIPv6: true);

            // Register both records
            await dnsClientV4.RegisterAsync(TestDomain_V4, TestIp_V4);
            await dnsClientV6.RegisterAsync(TestDomain_V6, TestIp_V6);

            // Act
            var resolvedIpV4 = ClientUtils.SendDnsQueryIPv4(dns_ip_v4, TestDomain_V4, DnsConst.UdpPort);
            var resolvedIpV6 = ClientUtils.SendDnsQueryIPv6(dns_ip_v6, TestDomain_V6, DnsConst.UdpPort);

            // Assert
            Assert.Equal(TestIp_V4, resolvedIpV4);
            Assert.Equal(TestIp_V6.ToLowerInvariant(), resolvedIpV6.ToLowerInvariant());
        }

        [Fact]
        public async Task RegisterAndResolve_MultipleIPv4AndIPv6_Success()
        {
            // Arrange
            var ipv4Domains = new[]
            {
                (domain: "multi4a.local", ip: "192.168.1.111"),
                (domain: "multi4b.local", ip: "192.168.1.112"),
                (domain: "multi4c.local", ip: "192.168.1.113")
            };
            var ipv6Domains = new[]
            {
                (domain: "multi6a.local", ip: "fd00::111"),
                (domain: "multi6b.local", ip: "fd00::112"),
                //(domain: "multi6c.local", ip: "fd00::113")
            };

            string dns_ip_v4 = DnsConst.DNS_IP;
            string dns_ip_v6 = DnsConst.DNS_IPv6;
            var dnsClientV4 = new RestClient(dns_ip_v4, DnsConst.ApiPort);
            var dnsClientV6 = new RestClient(dns_ip_v6, DnsConst.ApiPort, useIPv6: true);

            // Register all records in parallel
            var registerTasks = ipv4Domains.Select(d => dnsClientV4.RegisterAsync(d.domain, d.ip))
                .Concat(ipv6Domains.Select(d => dnsClientV6.RegisterAsync(d.domain, d.ip)));
            await Task.WhenAll(registerTasks);

            try
            {
                // Act & Assert: Resolve all IPv4
                foreach (var (domain, ip) in ipv4Domains)
                {
                    var resolvedIp = ClientUtils.SendDnsQueryIPv4(dns_ip_v4, domain, DnsConst.UdpPort);
                    Assert.Equal(ip, resolvedIp);
                }
                // Act & Assert: Resolve all IPv6
                foreach (var (domain, ip) in ipv6Domains)
                {
                    var resolvedIp = ClientUtils.SendDnsQueryIPv6(dns_ip_v6, domain, DnsConst.UdpPort);
                    Assert.Equal(ip.ToLowerInvariant(), resolvedIp.ToLowerInvariant());
                }
            }
            finally
            {
                // Cleanup: Unregister all test domains to avoid polluting server state
                var unregisterTasks = ipv4Domains.Select(d => dnsClientV4.UnregisterAsync(d.domain))
                    .Concat(ipv6Domains.Select(d => dnsClientV6.UnregisterAsync(d.domain)));
                await Task.WhenAll(unregisterTasks);
            }
        }
    }
}
