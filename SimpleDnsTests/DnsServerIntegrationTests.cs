using SimpleDnsClient;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace SimpleDnsServer.Tests
{
    public class DnsServerIntegrationTests : IClassFixture<DnsServerFixture>
    {
        private const string TestDomain = "test.local";
        private const string TestIp = "192.168.1.100";

        private readonly DnsServerFixture _fixture;

        public DnsServerIntegrationTests(DnsServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void RegisterAndResolveDomain_ReturnsCorrectIPv4()
        {
            // Arrange: Register domain (assumes server is already running)
            var dnsClient = new RestClient("127.0.0.1", Constants.ApiPort);
            dnsClient.Register(TestDomain, TestIp);

            // Act: Send DNS query
            var resolvedIp = SendDnsQuery(TestDomain, Constants.UdpPort);

            // Assert
            Assert.Equal(TestIp, resolvedIp);
        }

        private string SendDnsQuery(string domain, int port)
        {
            // This is a simplified UDP DNS query for A record
            using (var client = new UdpClient())
            {
                client.Connect("127.0.0.1", port);
                var query = BuildDnsQuery(domain);
                client.Send(query, query.Length);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                var response = client.Receive(ref remoteEP);
                return ParseDnsResponseForARecord(response);
            }
        }

        private byte[] BuildDnsQuery(string domain)
        {
            // Minimal DNS query for A record
            var rand = new Random();
            ushort id = (ushort)rand.Next(0, ushort.MaxValue);
            var header = new byte[] {
                (byte)(id >> 8), (byte)(id & 0xFF), // ID
                0x01, 0x00, // Standard query
                0x00, 0x01, // QDCOUNT
                0x00, 0x00, // ANCOUNT
                0x00, 0x00, // NSCOUNT
                0x00, 0x00  // ARCOUNT
            };
            var qname = new List<byte>();
            foreach (var part in domain.Split('.'))
            {
                qname.Add((byte)part.Length);
                qname.AddRange(Encoding.ASCII.GetBytes(part));
            }
            qname.Add(0); // End of QNAME
            var qtype = new byte[] { 0x00, 0x01 }; // Type A
            var qclass = new byte[] { 0x00, 0x01 }; // Class IN
            return header.Concat(qname).Concat(qtype).Concat(qclass).ToArray();
        }

        private string ParseDnsResponseForARecord(byte[] response)
        {
            // Find the answer section and extract IPv4
            // This is a simplified parser, assumes one answer
            int answerStart = response.Length - 4; // Last 4 bytes for IPv4
            return string.Join(".", response.Skip(answerStart).Take(4));
        }
    }
}
