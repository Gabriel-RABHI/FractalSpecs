using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.FileRead;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class FileReadToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly FileReadToolConfiguration _tool;

        public FileReadToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new FileReadToolConfiguration(_options);
        }

        [Fact]
        public async Task FileRead_Success()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\nLine 3");

            var result = await _tool.FileReadAsync(filePath, 0, 2);

            Assert.Contains("Line 1", result);
            Assert.Contains("Line 2", result);
            Assert.DoesNotContain("Line 3", result);
        }

        [Fact]
        public async Task FileRead_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var filePath = Path.Combine(outOfScopeDir, "test.txt");

            var result = await _tool.FileReadAsync(filePath);

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
