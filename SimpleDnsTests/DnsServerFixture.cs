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
            WaitForPortToBeFree(DnsConst.UdpPort);
            WaitForPortToBeFree(DnsConst.ApiPort);
        }

        public void Dispose()
        {
            KillAnyRunningServer(); // after tests
        }

        // Wait for OS to release sockets (avoid port conflict)
        private void WaitForPortToBeFree(int port, int maxWaitMs = 10000, int pollMs = 250)
        {
            int waited = 0;
            while (ProcessUtils.IsServerRunning(port))
            {
                if (waited >= maxWaitMs)
                    throw new TimeoutException($"Port {port} is still in use after waiting {maxWaitMs}ms");
                System.Threading.Thread.Sleep(pollMs);
                waited += pollMs;
            }
        }
    }
}
