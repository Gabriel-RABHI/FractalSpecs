using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Tools.ApplyPatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests.Tools
{
    public class ApplyPatchToolTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AgentOptions _options;
        private readonly ApplyPatchToolConfiguration _tool;

        public ApplyPatchToolTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _options = new AgentOptions
            {
                ReadPaths = new List<string> { _testDir },
                WritePaths = new List<string> { _testDir }
            };

            _tool = new ApplyPatchToolConfiguration(_options);
        }

        [Fact]
        public async Task ApplyPatch_Success()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Hello\nWorld\n");

            var patch = $@"--- {filePath}
+++ {filePath}
@@ -1,2 +1,3 @@
 Hello
+Beautiful
 World";

            var result = await _tool.ApplyPatchAsync(patch);

            Assert.Contains("Applied patch:", result);
            var newContent = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Beautiful", newContent);
        }

        [Fact]
        public async Task ApplyPatch_OutOfScope()
        {
            var outOfScopeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(outOfScopeDir);
            var filePath = Path.Combine(outOfScopeDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Hello\nWorld\n");

            var patch = $@"--- {filePath}
+++ {filePath}
@@ -1,2 +1,3 @@
 Hello
+Beautiful
 World";

            var result = await _tool.ApplyPatchAsync(patch);

            Assert.Contains("not authorized by WritePaths", result);

            Directory.Delete(outOfScopeDir, true);
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
