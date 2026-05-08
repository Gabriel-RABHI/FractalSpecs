using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.Agent.Tests.Helpers;
using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Configuration;
using FractalSpecs.AgentImplementation.Tools.Grep;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class GrepToolConfigurationShould
    {
        private AgentOptions CreateOptions(TestCodebaseHelper helper)
        {
            var options = new AgentOptions();
            options.ModelProvider = new LMStudioProviderConfiguration();
            options.ReadPaths = helper.ReadPaths.ToList();
            
            // Dummy agent setup to capture outputs
            var client = new TestAgentListenerClient();
            var agent = new GenericAgent(options, client);
            
            return options;
        }

        [Fact]
        public async Task SearchAsync_FindsExactMatches()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            var result = await tool.SearchAsync("GrepTest", helper.RootPath);

            Assert.Contains("Found", result);
            Assert.Contains("Program.cs", result);
            Assert.Contains("Order.cs", result);
            Assert.Contains("config.json", result);
        }

        [Fact]
        public async Task SearchAsync_RespectsGlobFilter()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            var result = await tool.SearchAsync("GrepTest", helper.RootPath, glob: "*.cs");

            Assert.Contains("Program.cs", result);
            Assert.Contains("Order.cs", result);
            Assert.DoesNotContain("config.json", result); // Should be filtered out
        }

        [Fact]
        public async Task SearchAsync_RespectsGitIgnoreAndDefaults()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            var result = await tool.SearchAsync("GrepTest", helper.RootPath);

            // Matches inside .gitignore and defaults
            Assert.DoesNotContain("app.dll", result);
            Assert.DoesNotContain("app.log", result);
            Assert.DoesNotContain(".git", result); // .git/config is ignored by default EnumerateFilesSafe
        }

        [Fact]
        public async Task SearchAsync_EnforcesReadPathScope()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            // Try to search outside the authorized scope
            var tempDir = Path.GetTempPath();
            var result = await tool.SearchAsync("GrepTest", tempDir);

            Assert.Contains("Error", result);
            Assert.Contains("not authorized by ReadPaths scope", result);
        }

        [Fact]
        public async Task SearchAsync_EmptyPathSearchesAllReadPaths()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            // Empty path should trigger search across ReadPaths
            var result = await tool.SearchAsync("GrepTest", "");

            Assert.Contains("Found", result);
            Assert.Contains("Program.cs", result);
        }

        [Fact]
        public async Task SearchAsync_ReturnsContextLines()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            // Program.cs has "var user = new Models.User();" on the line before "user.Name = ""GrepTest"";"
            var result = await tool.SearchAsync("GrepTest", helper.RootPath, glob: "*.cs", context_lines: 1);

            Assert.Contains("Program.cs", result);
            Assert.Contains("var user = new Models.User();", result); // Context line should be present
        }
        
        [Fact]
        public async Task SearchAsync_PublishesHistoryPart()
        {
            using var helper = new TestCodebaseHelper();
            helper.CreateComplexCodebase();

            var options = CreateOptions(helper);
            var tool = new GrepToolConfiguration(options);

            await tool.SearchAsync("GrepTest", helper.RootPath);

            var client = (TestAgentListenerClient)options.OwnerAgent.GetType().GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(options.OwnerAgent);
            
            var grepOutput = client.HistoryParts.OfType<GrepToolCallOutput>().FirstOrDefault();
            
            Assert.NotNull(grepOutput);
            Assert.Equal("GrepTest", grepOutput.Pattern);
            Assert.True(grepOutput.MatchCount > 0);
        }
    }
}
