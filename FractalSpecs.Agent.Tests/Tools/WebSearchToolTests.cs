using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.WebSearch;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class WebSearchToolTests
    {
        private readonly WebSearchToolConfiguration _tool;

        public WebSearchToolTests()
        {
            _tool = new WebSearchToolConfiguration(new AgentOptions());
        }

        [Fact]
        public async Task WebSearch_Success()
        {
            var result = await _tool.WebSearchAsync("test query", 1);

            // We do a real search against DDG, which might fail or be throttled, 
            // but this checks the basic flow.
            Assert.True(result.Contains("Search results for:") || result.Contains("No results found for:"));
        }
    }
}
