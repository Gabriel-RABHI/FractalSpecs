using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.FileWrite
{
    public class FileWriteToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "FileWrite";

        public string Description => "Create a new file or overwrite an existing file with the provided content.";

        public FileWriteToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(FileWriteAsync, ToolKey, Description);
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

        [Description("Create a new file or overwrite an existing file with the provided content.")]
        public async Task<string> FileWriteAsync(
            [Description("Absolute path to the file to write")] string file_path,
            [Description("The content to write to the file")] string content)
        {
            var outputInfo = new FileWriteToolCallOutput(file_path, content);
            var workingDirectory = _parent.WritePaths.FirstOrDefault() ?? "c:/";

            var resolvedPath = Path.GetFullPath(file_path, workingDirectory);

            if (!IsPathAllowed(resolvedPath, _parent.WritePaths))
            {
                outputInfo.ErrorMessage = $"Error: Path '{resolvedPath}' is not authorized by WritePaths scope.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            try
            {
                var dir = Path.GetDirectoryName(resolvedPath);
                if (dir is not null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var existed = File.Exists(resolvedPath);

                await File.WriteAllTextAsync(resolvedPath, content);

                var lineCount = content.Split('\n').Length;
                var verb = existed ? "Overwrote" : "Created";

                var finalOutput = $"{verb} {resolvedPath} ({lineCount} lines)";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (UnauthorizedAccessException)
            {
                outputInfo.ErrorMessage = $"Error: Cannot write to '{resolvedPath}': access denied.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (IOException ex) when (ex.HResult == unchecked((int)0x80070020) || ex.Message.Contains("being used by another process"))
            {
                outputInfo.ErrorMessage = $"Error: Cannot write to '{resolvedPath}': file is locked by another process.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error writing file: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }
    }
}
