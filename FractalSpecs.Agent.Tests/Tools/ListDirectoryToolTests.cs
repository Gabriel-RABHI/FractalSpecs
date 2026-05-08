using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.ListDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class ListDirectoryToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly ListDirectoryToolConfiguration _tool;

        public ListDirectoryToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new ListDirectoryToolConfiguration(_options);
        }

        [Fact]
        public async Task ListDirectory_Success()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Hello");

            var result = await _tool.ListDirectoryAsync(_testDir);

            Assert.Contains("test.txt", result);
            Assert.Contains("1 entries", result);
        }

        [Fact]
        public async Task ListDirectory_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var result = await _tool.ListDirectoryAsync(outOfScopeDir);

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
