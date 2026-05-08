using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.ApplyPatch
{
    public class ApplyPatchToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "ApplyPatch";

        public string Description => "Apply a unified diff patch to one or more files. Supports standard unified diff format.";

        public ApplyPatchToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(ApplyPatchAsync, ToolKey, Description);
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

        [Description("Apply a unified diff patch to one or more files. Supports standard unified diff format.")]
        public async Task<string> ApplyPatchAsync(
            [Description("The unified diff patch content")] string patch,
            [Description("Preview changes without writing (default: false)")] bool dry_run = false)
        {
            var outputInfo = new ApplyPatchToolCallOutput(patch, dry_run);
            var workingDirectory = _parent.WritePaths.FirstOrDefault() ?? "c:/";

            try
            {
                var hunks = ParsePatch(patch);
                if (hunks.Count == 0)
                {
                    outputInfo.ErrorMessage = "Error: No valid hunks found in patch";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ErrorMessage;
                }

                var results = new List<string>();
                var filesModified = 0;

                var fileHunks = hunks.GroupBy(h => h.FilePath);

                foreach (var group in fileHunks)
                {
                    var filePath = Path.GetFullPath(group.Key, workingDirectory);

                    if (!IsPathAllowed(filePath, _parent.WritePaths))
                    {
                        results.Add($"SKIP {group.Key}: Path '{filePath}' is not authorized by WritePaths scope.");
                        continue;
                    }

                    if (!File.Exists(filePath))
                    {
                        results.Add($"SKIP {group.Key}: file not found");
                        continue;
                    }

                    var originalLines = (await File.ReadAllLinesAsync(filePath)).ToList();
                    var modifiedLines = new List<string>(originalLines);
                    var offset = 0;
                    bool contextMismatch = false;

                    foreach (var hunk in group.OrderBy(h => h.StartLine))
                    {
                        var adjustedStart = hunk.StartLine - 1 + offset;

                        var contextMatch = VerifyContext(modifiedLines, adjustedStart, hunk);
                        if (!contextMatch)
                        {
                            results.Add($"FAIL {group.Key}:{hunk.StartLine}: context mismatch");
                            contextMismatch = true;
                            break;
                        }

                        var (newLines, linesRemoved, linesAdded) = ApplyHunk(modifiedLines, adjustedStart, hunk);
                        modifiedLines = newLines;
                        offset += linesAdded - linesRemoved;
                    }

                    if (contextMismatch)
                    {
                        continue;
                    }

                    if (!dry_run)
                    {
                        await File.WriteAllLinesAsync(filePath, modifiedLines);
                    }

                    var verb = dry_run ? "Would modify" : "Modified";
                    results.Add($"OK {verb} {group.Key} ({group.Count()} hunk(s))");
                    filesModified++;
                }

                var summary = dry_run
                    ? $"Dry run: {filesModified} file(s) would be modified"
                    : $"Applied patch: {filesModified} file(s) modified";

                var finalOutput = $"{summary}\n{string.Join('\n', results)}";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error: Patch application failed: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private static List<PatchHunk> ParsePatch(string patch)
        {
            var hunks = new List<PatchHunk>();
            var lines = patch.Split('\n');
            string? currentFile = null;
            PatchHunk? currentHunk = null;
            var hunkHeaderPattern = new Regex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@");

            foreach (var line in lines)
            {
                if (line.StartsWith("+++ "))
                {
                    var path = line.Substring(4).TrimStart('b', '/').Trim();
                    currentFile = path;
                    continue;
                }

                if (line.StartsWith("--- ")) continue;

                var hunkMatch = hunkHeaderPattern.Match(line);
                if (hunkMatch.Success && currentFile is not null)
                {
                    currentHunk = new PatchHunk
                    {
                        FilePath = currentFile,
                        StartLine = int.Parse(hunkMatch.Groups[1].Value),
                    };
                    hunks.Add(currentHunk);
                    continue;
                }

                if (currentHunk is not null &&
                    (line.StartsWith('+') || line.StartsWith('-') || line.StartsWith(' ')))
                {
                    currentHunk.Lines.Add(line);
                }
            }

            return hunks;
        }

        private static bool VerifyContext(List<string> fileLines, int startIndex, PatchHunk hunk)
        {
            var fileIdx = startIndex;
            foreach (var line in hunk.Lines)
            {
                if (line.StartsWith(' ') || line.StartsWith('-'))
                {
                    if (fileIdx >= fileLines.Count) return false;
                    var expected = line.Substring(1);
                    if (fileLines[fileIdx] != expected) return false;
                    fileIdx++;
                }
            }
            return true;
        }

        private static (List<string> Result, int Removed, int Added) ApplyHunk(
            List<string> lines, int startIndex, PatchHunk hunk)
        {
            var result = new List<string>(lines.Take(startIndex));
            var removed = 0;
            var added = 0;
            var sourceIdx = startIndex;

            foreach (var line in hunk.Lines)
            {
                if (line.StartsWith(' '))
                {
                    result.Add(lines[sourceIdx]);
                    sourceIdx++;
                }
                else if (line.StartsWith('-'))
                {
                    sourceIdx++;
                    removed++;
                }
                else if (line.StartsWith('+'))
                {
                    result.Add(line.Substring(1));
                    added++;
                }
            }

            result.AddRange(lines.Skip(sourceIdx));
            return (result, removed, added);
        }

        private class PatchHunk
        {
            public string FilePath { get; set; } = string.Empty;
            public int StartLine { get; set; }
            public List<string> Lines { get; } = new List<string>();
        }
    }
}
