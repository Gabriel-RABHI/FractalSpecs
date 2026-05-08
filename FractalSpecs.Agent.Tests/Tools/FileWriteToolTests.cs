using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.FileWrite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class FileWriteToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly FileWriteToolConfiguration _tool;

        public FileWriteToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new FileWriteToolConfiguration(_options);
        }

        [Fact]
        public async Task FileWrite_Success()
        {
            var filePath = Path.Combine(_testDir, "test.txt");

            var result = await _tool.FileWriteAsync(filePath, "Hello Universe");

            Assert.Contains("Created", result);
            var newContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal("Hello Universe", newContent);
        }

        [Fact]
        public async Task FileWrite_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var filePath = Path.Combine(outOfScopeDir, "test.txt");

            var result = await _tool.FileWriteAsync(filePath, "Hello Universe");

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
