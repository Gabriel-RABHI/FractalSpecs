using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.FileEdit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class FileEditToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly FileEditToolConfiguration _tool;

        public FileEditToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new FileEditToolConfiguration(_options);
        }

        [Fact]
        public async Task FileEdit_Success()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Hello World");

            var result = await _tool.FileEditAsync(filePath, "World", "Universe");

            Assert.Contains("Replaced 1 occurrence", result);
            var newContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal("Hello Universe", newContent);
        }

        [Fact]
        public async Task FileEdit_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var filePath = Path.Combine(outOfScopeDir, "test.txt");

            var result = await _tool.FileEditAsync(filePath, "World", "Universe");

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
