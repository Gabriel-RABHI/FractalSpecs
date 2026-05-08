using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.FileRead
{
    public class FileReadToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "FileRead";

        public string Description => "Read a file from the filesystem. Returns the contents with line numbers.";

        public FileReadToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(FileReadAsync, ToolKey, Description);
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

        [Description("Read a file from the filesystem. Returns the contents with line numbers.")]
        public async Task<string> FileReadAsync(
            [Description("Absolute path to the file to read")] string file_path,
            [Description("Line number to start reading from (0-based)")] int offset = 0,
            [Description("Maximum number of lines to read")] int limit = 2000)
        {
            var outputInfo = new FileReadToolCallOutput(file_path, offset, limit, null, 0);
            var workingDirectory = _parent.ReadPaths.FirstOrDefault() ?? "c:/";

            if (string.IsNullOrEmpty(file_path))
            {
                outputInfo.ErrorMessage = "Error: Missing file_path. Provide a valid file path.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            var resolvedPath = Path.GetFullPath(file_path, workingDirectory);

            if (!IsPathAllowed(resolvedPath, _parent.ReadPaths))
            {
                outputInfo.ErrorMessage = $"Error: Path '{resolvedPath}' is not authorized by ReadPaths scope.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            if (!File.Exists(resolvedPath))
            {
                outputInfo.ErrorMessage = $"Error: File not found: {resolvedPath}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(resolvedPath);
                var totalLines = lines.Length;

                if (totalLines == 0)
                {
                    var emptyOutput = $"File is empty: {resolvedPath}";
                    outputInfo.ResultOutput = emptyOutput;
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return emptyOutput;
                }

                var selectedLines = lines
                    .Skip(offset)
                    .Take(limit)
                    .Select((line, idx) => $"{offset + idx + 1}\t{line}");

                var content = string.Join('\n', selectedLines);
                var header = $"[{resolvedPath}] ({totalLines} lines total)";

                if (offset > 0 || totalLines > offset + limit)
                {
                    header += $" showing lines {offset + 1}-{Math.Min(offset + limit, totalLines)}";
                }

                var finalOutput = $"{header}\n{content}";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (UnauthorizedAccessException)
            {
                outputInfo.ErrorMessage = $"Error: Access denied: cannot read '{resolvedPath}'.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error reading file: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }
    }
}
