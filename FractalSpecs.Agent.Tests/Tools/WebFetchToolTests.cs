using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.WebFetch;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class WebFetchToolTests
    {
        private readonly WebFetchToolConfiguration _tool;

        public WebFetchToolTests()
        {
            _tool = new WebFetchToolConfiguration(new AgentOptions());
        }

        [Fact]
        public async Task WebFetch_InvalidUrl()
        {
            var result = await _tool.WebFetchAsync("invalid-url");

            Assert.Contains("Error: Invalid URL", result);
        }

        [Fact]
        public async Task WebFetch_Success()
        {
            // We shouldn't rely on external HTTP requests ideally, but since we are porting this tool 
            // and keeping it simple, we do a basic test. If it fails, we know there's a problem.
            var result = await _tool.WebFetchAsync("https://example.com", 100);

            Assert.Contains("[200]", result);
        }
    }
}
