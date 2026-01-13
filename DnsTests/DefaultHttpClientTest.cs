namespace DnsClient
{
    public class DefaultHttpClientTest
    {
        [Theory]
        [InlineData("https://httpbin.org/get")]
        [InlineData("http://httpbin.org/get")]
        public async Task GetAsync_ReturnsResponse(string url)
        {
            var client = new DefaultHttpClient();
            var response = await client.GetAsync(url);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
