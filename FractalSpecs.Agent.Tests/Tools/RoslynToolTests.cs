using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.Roslyn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class RoslynToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly RoslynToolConfiguration _tool;

        public RoslynToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new RoslynToolConfiguration(_options);
        }

        [Fact]
        public async Task Roslyn_NoCsFiles()
        {
            var result = await _tool.RoslynAsync("overview", ".");

            Assert.Contains("Error: No C# files found", result);
        }

        [Fact]
        public async Task Roslyn_Overview_Success()
        {
            var filePath = Path.Combine(_testDir, "TestClass.cs");
            await File.WriteAllTextAsync(filePath, "namespace TestNamespace { public class TestClass { } }");

            var result = await _tool.RoslynAsync("overview", ".");

            Assert.Contains("Project Overview", result);
            Assert.Contains("TestClass", result);
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
