using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.Glob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class GlobToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly GlobToolConfiguration _tool;

        public GlobToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new GlobToolConfiguration(_options);
        }

        [Fact]
        public async Task Glob_Success()
        {
            var filePath = Path.Combine(_testDir, "test.cs");
            await File.WriteAllTextAsync(filePath, "public class Test {}");

            var result = await _tool.GlobAsync("**/*.cs", _testDir);

            Assert.Contains("Found 1 file(s)", result);
            Assert.Contains("test.cs", result);
        }

        [Fact]
        public async Task Glob_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var result = await _tool.GlobAsync("**/*.cs", outOfScopeDir);

            Assert.Contains("not authorized", result);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
    }
}
