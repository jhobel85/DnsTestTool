using Microsoft.AspNetCore.Mvc.Testing;

namespace DualstackDnsServer
{
    // Uses real DNS server started by DnsServerFixture
    public class DnsApiControllerIntegrationTest : IClassFixture<DnsServerFixture>
    {
        private readonly HttpClient _client;
        private readonly string dns_ip_v4 = DnsConst.GetDnsIp(DnsIpMode.Localhost);
        private readonly string dns_ip_v6 = DnsConst.GetDnsIpV6(DnsIpMode.Localhost);
        private readonly int udp_port = DnsConst.UdpPort;

        public DnsApiControllerIntegrationTest(DnsServerFixture fixture)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri($"http://{dns_ip_v4}:{DnsConst.ApiHttp}")
            };
        }

            [Fact]
            public async Task Query_Endpoint_ResolvesIPv4OrIPv6_DomainOnly()
            {
                // Register IPv4
                var registerV4 = await _client.PostAsync("/dns/register?domain=testquery.com&ip=1.2.3.4", null);
                registerV4.EnsureSuccessStatusCode();

                //IPv4: Act and verify
                var response = await _client.GetAsync("/dns/query?domain=testquery.com");
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("1.2.3.4", content);

                // Register IPv6 (and IPVv4)
                var registerV6 = await _client.PostAsync("/dns/register?domain=testquery.com&ip=2001:db8::2", null);
                registerV6.EnsureSuccessStatusCode();

                //IPv6 Act and verify
                response = await _client.GetAsync("/dns/query?domain=testquery.com");
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                Assert.Contains("2001:db8::2", content);

                // Register IPv4 again
                registerV4 = await _client.PostAsync("/dns/register?domain=testquery.com&ip=1.2.3.4", null);
                registerV4.EnsureSuccessStatusCode();
                
                //IPv4 Act and verify again IPv4
                response = await _client.GetAsync("/dns/query?domain=testquery.com");
                content = await response.Content.ReadAsStringAsync();
                Assert.Contains("1.2.3.4", content);
            }

            [Fact]
            public async Task Query_Endpoint_ResolvesWithDnsServerAndType()
            {
                Console.WriteLine("[TEST] Starting Query_Endpoint_ResolvesWithDnsServerAndType");
                // Register IPv4 and IPv6 records
                var registerV4 = await _client.PostAsync("/dns/register?domain=testquery2.com&ip=1.2.3.5", null);
                registerV4.EnsureSuccessStatusCode();

                var responseA = await _client.GetAsync($"/dns/query/server?domain=testquery2.com&dnsServer={dns_ip_v4}&port={udp_port}&type=A");
                var contentA = await responseA.Content.ReadAsStringAsync();
                Console.WriteLine($"[TEST] responseA.StatusCode={{responseA.StatusCode}}, contentA={{contentA}}");
                Assert.True(responseA.IsSuccessStatusCode, $"Query A failed: {responseA.StatusCode} - {contentA}");
                Assert.Contains("1.2.3.5", contentA);

                var registerV6 = await _client.PostAsync("/dns/register?domain=testquery2.com&ip=2001:db8::3", null);
                registerV6.EnsureSuccessStatusCode();

                // Act: Query for AAAA record
                var responseAAAA = await _client.GetAsync($"/dns/query/server?domain=testquery2.com&dnsServer={dns_ip_v6}&port={udp_port}&type=AAAA");
                responseAAAA.EnsureSuccessStatusCode();
                var contentAAAA = await responseAAAA.Content.ReadAsStringAsync();
                Console.WriteLine($"[TEST] responseAAAA.StatusCode={{responseAAAA.StatusCode}}, contentAAAA={{contentAAAA}}");
                Assert.Contains("2001:db8::3", contentAAAA);
                Console.WriteLine("[TEST] Finished Query_Endpoint_ResolvesWithDnsServerAndType");
            }

            [Fact]
            public async Task Query_Endpoint_ReturnsBadRequest_WhenDomainMissing()
            {
                var response = await _client.GetAsync("/dns/query");
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }

            [Fact]
            public async Task Query_Endpoint_ReturnsBadRequest_WhenDnsServerMissing()
            {
                var response = await _client.GetAsync("/dns/query/server?domain=testquery3.com&port=53&type=A");
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }

        [Fact]
        public async Task Resolve_Endpoint_ReturnsOk()
        {
            // Arrange: Register a domain first (if needed, depending on your API logic)
            var registerResponse = await _client.PostAsync("/dns/register?domain=example.com&ip=1.2.3.4", null);
            registerResponse.EnsureSuccessStatusCode();

            // Act: Call the resolve endpoint
            var response = await _client.GetAsync("/dns/resolve?domain=example.com");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("1.2.3.4", content);
        }

        [Fact]
        public async Task GetAllEntries_ReturnsBothIPv4AndIPv6()
        {
            // Arrange: Register IPv4 and IPv6 records
            var registerV4 = await _client.PostAsync("/dns/register?domain=example.com&ip=1.2.3.4", null);
            registerV4.EnsureSuccessStatusCode();
            var registerV6 = await _client.PostAsync("/dns/register?domain=ipv6.com&ip=2001:db8::1", null);
            registerV6.EnsureSuccessStatusCode();

            // Act: Call the GetAllEntries endpoint
            var response = await _client.GetAsync("/dns/entries");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("1.2.3.4", content); // IPv4
            Assert.Contains("2001:db8::1", content); // IPv6
            Assert.Contains("example.com", content);
            Assert.Contains("ipv6.com", content);
        }
    }
}
