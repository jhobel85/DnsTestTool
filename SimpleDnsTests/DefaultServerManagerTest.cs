using Xunit;
using SimpleDnsServer.Utils;

namespace SimpleDnsTests
{
    public class DefaultServerManagerTest
    {
        [Fact]
        public void CanInstantiateDefaultServerManager()
        {
            var mgr = new DefaultServerManager();
            Assert.NotNull(mgr);
        }
    }
}
