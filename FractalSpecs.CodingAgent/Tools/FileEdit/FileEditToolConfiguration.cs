using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.FileEdit
{
    public class FileEditToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "FileEdit";

        public string Description => "Perform an exact string replacement in a file. The old_string must match exactly one location in the file.";

        public FileEditToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(FileEditAsync, ToolKey, Description);
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

        [Description("Perform an exact string replacement in a file. The old_string must match exactly one location in the file.")]
        public async Task<string> FileEditAsync(
            [Description("Absolute path to the file to edit")] string file_path,
            [Description("The exact text to find and replace")] string old_string,
            [Description("The replacement text")] string new_string,
            [Description("Replace all occurrences (default: false)")] bool replace_all = false)
        {
            var outputInfo = new FileEditToolCallOutput(file_path, old_string, new_string, replace_all);
            var workingDirectory = _parent.WritePaths.FirstOrDefault() ?? "c:/";

            if (string.IsNullOrEmpty(old_string))
            {
                outputInfo.ErrorMessage = "Error: old_string must not be empty. Use FileWrite to create a new file, or supply non-empty text to find and replace.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            if (old_string == new_string)
            {
                outputInfo.ErrorMessage = "Error: old_string and new_string are identical — nothing to replace.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            var resolvedPath = Path.GetFullPath(file_path, workingDirectory);

            if (!IsPathAllowed(resolvedPath, _parent.WritePaths))
            {
                outputInfo.ErrorMessage = $"Error: Path '{resolvedPath}' is not authorized by WritePaths scope.";
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
                var content = await File.ReadAllTextAsync(resolvedPath);
                var occurrences = CountOccurrences(content, old_string);

                if (occurrences == 0)
                {
                    outputInfo.ErrorMessage = $"Error: old_string not found in {resolvedPath}";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ErrorMessage;
                }

                if (occurrences > 1 && !replace_all)
                {
                    outputInfo.ErrorMessage = $"Error: old_string found {occurrences} times in {resolvedPath}. Provide more context to make it unique, or set replace_all=true.";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ErrorMessage;
                }

                string updated;
                if (replace_all)
                {
                    updated = content.Replace(old_string, new_string);
                }
                else
                {
                    updated = ReplaceFirst(content, old_string, new_string);
                }

                await File.WriteAllTextAsync(resolvedPath, updated);

                var replacements = replace_all ? occurrences : 1;
                var finalOutput = $"Replaced {replacements} occurrence(s) in {resolvedPath}";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (UnauthorizedAccessException)
            {
                outputInfo.ErrorMessage = $"Error: Cannot edit '{resolvedPath}': access denied.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (IOException ex) when (ex.HResult == unchecked((int)0x80070020) || ex.Message.Contains("being used by another process"))
            {
                outputInfo.ErrorMessage = $"Error: Cannot edit '{resolvedPath}': file is locked by another process.";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error editing file: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private static int CountOccurrences(string text, string search)
        {
            int count = 0, index = 0;
            while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += search.Length;
            }
            return count;
        }

        private static string ReplaceFirst(string text, string oldValue, string newValue)
        {
            var index = text.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0) return text;
            return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
        }
    }
}
