using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.Glob
{
    public class GlobToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "Glob";

        public string Description => "Find files matching a glob pattern. Returns paths sorted by modification time.";

        public GlobToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(GlobAsync, ToolKey, Description);
        }

        private bool IsPathAllowed(string targetPath, IEnumerable<string> allowedPaths)
        {
            var fullTargetPath = Path.GetFullPath(targetPath);
            foreach (var rp in allowedPaths)
            {
                var fullRp = Path.GetFullPath(rp);
                if (fullTargetPath.Equals(fullRp, StringComparison.OrdinalIgnoreCase) || 
                    fullTargetPath.StartsWith(fullRp.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullTargetPath.StartsWith(fullRp.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        [Description("Find files matching a glob pattern. Returns paths sorted by modification time.")]
        public async Task<string> GlobAsync(
            [Description("Glob pattern (e.g. **/*.cs, src/**/*.json)")] string pattern,
            [Description("Directory to search in (default: working directory)")] string path = ".")
        {
            var outputInfo = new GlobToolCallOutput(pattern, path);
            var workingDirectory = _parent.ReadPaths.FirstOrDefault() ?? "c:/";

            var searchPath = string.IsNullOrEmpty(path) || path == "."
                ? workingDirectory
                : Path.GetFullPath(path, workingDirectory);

            if (!IsPathAllowed(searchPath, _parent.ReadPaths))
            {
                outputInfo.ErrorMessage = $"Error: Path '{searchPath}' is not authorized by ReadPaths scope.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            if (!Directory.Exists(searchPath))
            {
                outputInfo.ErrorMessage = $"Error: Directory not found: {searchPath}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            try
            {
                var matcher = new Matcher();
                matcher.AddInclude(pattern);

                var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(searchPath));
                var result = matcher.Execute(directoryInfo);

                var files = result.Files
                    .Select(f => Path.Combine(searchPath, f.Path))
                    .Where(f => File.Exists(f) && IsPathAllowed(f, _parent.ReadPaths))
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .Take(250)
                    .ToList();

                if (files.Count == 0)
                {
                    outputInfo.ResultOutput = $"No files matching '{pattern}' in {searchPath}";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ResultOutput;
                }

                var outputStr = string.Join('\n', files);
                var finalOutput = $"Found {files.Count} file(s):\n{outputStr}";
                
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error: Glob error: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }
    }
}
