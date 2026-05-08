using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.ListDirectory
{
    public class ListDirectoryToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "ListDirectory";

        public string Description => "List files and directories at a given path. Shows file sizes and modification times.";

        public ListDirectoryToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(ListDirectoryAsync, ToolKey, Description);
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

        [Description("List files and directories at a given path. Shows file sizes and modification times.")]
        public async Task<string> ListDirectoryAsync(
            [Description("Directory path to list (default: working directory)")] string path = ".",
            [Description("List recursively (default: false)")] bool recursive = false,
            [Description("Maximum entries to return (default: 200)")] int max_entries = 200)
        {
            var outputInfo = new ListDirectoryToolCallOutput(path, recursive, max_entries);
            var workingDirectory = _parent.ReadPaths.FirstOrDefault() ?? "c:/";

            var dirPath = string.IsNullOrEmpty(path) || path == "."
                ? workingDirectory
                : Path.GetFullPath(path, workingDirectory);

            if (!IsPathAllowed(dirPath, _parent.ReadPaths))
            {
                outputInfo.ErrorMessage = $"Error: Path '{dirPath}' is not authorized by ReadPaths scope.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            if (!Directory.Exists(dirPath))
            {
                outputInfo.ErrorMessage = $"Error: Directory not found: {dirPath}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var entries = new List<string>();

                foreach (var dir in Directory.EnumerateDirectories(dirPath, "*", searchOption))
                {
                    if (entries.Count >= max_entries) break;
                    
                    if (!IsPathAllowed(dir, _parent.ReadPaths)) continue;

                    var rel = Path.GetRelativePath(dirPath, dir);
                    entries.Add($"  {rel}/");
                }

                foreach (var file in Directory.EnumerateFiles(dirPath, "*", searchOption))
                {
                    if (entries.Count >= max_entries) break;
                    
                    if (!IsPathAllowed(file, _parent.ReadPaths)) continue;

                    var rel = Path.GetRelativePath(dirPath, file);
                    var info = new FileInfo(file);
                    var size = FormatSize(info.Length);
                    entries.Add($"  {rel}  ({size})");
                }

                if (entries.Count == 0)
                {
                    outputInfo.ResultOutput = $"{dirPath}/ (empty)";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ResultOutput;
                }

                var truncated = entries.Count >= max_entries ? $"\n... (truncated at {max_entries} entries)" : "";
                var header = $"{dirPath}/ ({entries.Count} entries)";
                var finalOutput = $"{header}\n{string.Join('\n', entries)}{truncated}";

                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (UnauthorizedAccessException)
            {
                outputInfo.ErrorMessage = $"Error: Permission denied: {dirPath}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error listing directory: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes}B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1}KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1}MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1}GB",
        };
    }
}
