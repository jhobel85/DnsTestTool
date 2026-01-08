using SimpleDnsServer.Utils;
using Xunit;

namespace SimpleDnsServer.Tests
{
    public class DnsServerFixture : IDisposable
    {
        public DnsServerFixture()
        {
            KillAnyRunningServer(); // before tests
            ServerUtils.StartDnsServer();
        }

        private void KillAnyRunningServer()
        {
            ProcessUtils.KillAllServers(DnsConst.UdpPort/*, Constants.DNS_IP*/);
            ProcessUtils.KillAllServers(DnsConst.ApiPort/*, Constants.DNS_IP*/);
        }

        public void Dispose()
        {            
            KillAnyRunningServer(); // after tests       
        }
    }
}
