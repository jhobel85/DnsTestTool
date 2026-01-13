using Xunit;
using DualstackDnsServer.Utils;

namespace DualstackDnsServer
{
    public class DefaultServerManagerTest
    {
        [Fact]
        public void CanInstantiateDefaultServerManager()
        {
            var mgr = new ServerManager();
            Assert.NotNull(mgr);
        }
    }
}
