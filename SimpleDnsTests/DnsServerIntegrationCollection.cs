using Xunit;

namespace SimpleDnsServer.Tests
{
    [CollectionDefinition("DnsServerIntegration", DisableParallelization = true)]
    public class DnsServerIntegrationCollection : ICollectionFixture<DnsServerFixture>
    {
        // This class has no code, and is never created. Its purpose is just to be the place to apply [CollectionDefinition] and ICollectionFixture.
    }
}
