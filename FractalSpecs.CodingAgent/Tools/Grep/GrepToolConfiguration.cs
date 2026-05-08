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

namespace FractalSpecs.AgentImplementation.Tools.Grep
{
    public class GrepToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "Grep";

        public string Description => "Search file contents using regex patterns. Uses native C# search.";

        public GrepToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(SearchAsync, ToolKey, Description);
        }

        private bool IsPathAllowed(string targetPath, IEnumerable<string> readPaths)
        {
            var fullTargetPath = Path.GetFullPath(targetPath);
            foreach (var rp in readPaths)
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

        [Description("Search file contents using regex patterns. Uses native C# search.")]
        public async Task<string> SearchAsync(
            [Description("Regex pattern to search for")] string pattern,
            [Description("File or directory to search in")] string path = ".",
            [Description("Glob filter for files (e.g. *.cs)")] string? glob = null,
            [Description("Case insensitive search")] bool case_insensitive = false,
            [Description("Lines of context around matches")] int context_lines = 0,
            [Description("Maximum number of results (default: 250)")] int max_results = 250)
        {
            var outputInfo = new GrepToolCallOutput(pattern, path, glob, case_insensitive, context_lines, max_results);
            var workingDirectory = _parent.ReadPaths.FirstOrDefault() ?? "c:/";

            var pathsToSearch = new List<string>();
            if (string.IsNullOrWhiteSpace(path) || path == ".")
            {
                pathsToSearch.AddRange(_parent.ReadPaths);
            }
            else
            {
                var explicitPath = Path.GetFullPath(path, workingDirectory);
                if (!IsPathAllowed(explicitPath, _parent.ReadPaths))
                {
                    outputInfo.ErrorMessage = $"Error: Path '{explicitPath}' is not authorized by ReadPaths scope.";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ErrorMessage;
                }
                pathsToSearch.Add(explicitPath);
            }

            try
            {
                var regexOptions = case_insensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
                var regex = new Regex(pattern, regexOptions);

                var filesToSearch = new List<string>();
                
                foreach (var sp in pathsToSearch)
                {
                    if (File.Exists(sp))
                    {
                        filesToSearch.Add(sp);
                    }
                    else if (Directory.Exists(sp))
                    {
                        filesToSearch.AddRange(EnumerateFilesSafe(sp, glob, workingDirectory));
                    }
                    else
                    {
                        if (pathsToSearch.Count == 1)
                        {
                            outputInfo.ErrorMessage = $"Error: Path '{sp}' does not exist.";
                            _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                            return outputInfo.ErrorMessage;
                        }
                    }
                }
                
                filesToSearch = filesToSearch.Distinct().ToList();

                var results = new List<string>();
                int matchCount = 0;
                
                foreach (var file in filesToSearch)
                {
                    if (matchCount >= max_results) break;
                    
                    if (!IsPathAllowed(file, _parent.ReadPaths)) continue; // Double check each file

                    string[] lines;
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length > 10 * 1024 * 1024) continue; // Skip files > 10MB
                        
                        lines = await File.ReadAllLinesAsync(file);
                    }
                    catch
                    {
                        continue; // Skip files that can't be read
                    }

                    var addedLines = new HashSet<int>();

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (matchCount >= max_results) break;
                        
                        if (regex.IsMatch(lines[i]))
                        {
                            matchCount++;
                            int startContext = Math.Max(0, i - context_lines);
                            int endContext = Math.Min(lines.Length - 1, i + context_lines);
                            
                            for (int j = startContext; j <= endContext; j++)
                            {
                                if (addedLines.Add(j))
                                {
                                    string separator = j == i ? ":" : "-";
                                    string resultLine = $"{file}{separator}{j + 1}{separator}{lines[j]}";
                                    results.Add(resultLine);
                                }
                            }
                        }
                    }
                }

                outputInfo.MatchCount = matchCount;

                if (matchCount == 0)
                {
                    outputInfo.ResultOutput = $"No matches for pattern '{pattern}' in {string.Join(", ", pathsToSearch)}";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ResultOutput;
                }

                var truncated = matchCount >= max_results;
                var outputStr = string.Join("\n", results);

                var header = truncated
                    ? $"Showing first {max_results} matches (truncated):"
                    : $"Found {matchCount} match(es):";

                string finalOutput = $"{header}\n{outputStr}";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error: Grep error: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private class GitIgnoreChecker
        {
            private List<Regex> _rules = new List<Regex>();
            private string _baseDir;

            public GitIgnoreChecker(string rootDir)
            {
                _baseDir = Path.GetFullPath(rootDir).Replace('\\', '/');
                if (!_baseDir.EndsWith("/")) _baseDir += "/";

                string gitignorePath = Path.Combine(rootDir, ".gitignore");
                if (File.Exists(gitignorePath))
                {
                    try
                    {
                        var lines = File.ReadAllLines(gitignorePath);
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("!")) 
                                continue; 

                            string pattern = trimmed.Replace('\\', '/');
                            bool startsWithSlash = pattern.StartsWith("/");
                            if (startsWithSlash) pattern = pattern.Substring(1);

                            pattern = Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".");

                            if (startsWithSlash)
                            {
                                pattern = "^" + pattern;
                            }
                            else
                            {
                                pattern = "(^|/)" + pattern;
                            }

                            if (!trimmed.EndsWith("/") && !trimmed.EndsWith("*"))
                            {
                                pattern = pattern + "($|/)"; 
                            }

                            _rules.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                        }
                    }
                    catch { }
                }
            }

            public bool IsIgnored(string absolutePath)
            {
                if (_rules.Count == 0) return false;
                
                string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
                string relative = normalized;
                
                if (normalized.StartsWith(_baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    relative = normalized.Substring(_baseDir.Length);
                }

                foreach (var rule in _rules)
                {
                    if (rule.IsMatch(relative)) return true;
                }
                return false;
            }
        }

        private IEnumerable<string> EnumerateFilesSafe(string rootPath, string? glob, string workingDirectory)
        {
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);
            
            Regex? globRegex = null;
            if (!string.IsNullOrWhiteSpace(glob))
            {
                string cleanGlob = glob;
                if (cleanGlob.StartsWith("**/")) cleanGlob = cleanGlob.Substring(3);
                if (cleanGlob.StartsWith("**\\")) cleanGlob = cleanGlob.Substring(3);
                
                string pattern = "^" + Regex.Escape(cleanGlob).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                globRegex = new Regex(pattern, RegexOptions.IgnoreCase);
            }

            var gitIgnoreCheckers = new List<GitIgnoreChecker>();
            if (Directory.Exists(workingDirectory))
            {
                gitIgnoreCheckers.Add(new GitIgnoreChecker(workingDirectory));
            }

            if (!Path.GetFullPath(rootPath).Equals(Path.GetFullPath(workingDirectory), StringComparison.OrdinalIgnoreCase))
            {
                gitIgnoreCheckers.Add(new GitIgnoreChecker(rootPath));
            }

            while (queue.Count > 0)
            {
                string currentDir = queue.Dequeue();
                
                string dirName = Path.GetFileName(currentDir);
                if (dirName.Equals(".git", StringComparison.OrdinalIgnoreCase) || 
                    dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || 
                    dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) || 
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals(".vs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool dirIgnored = false;
                foreach (var checker in gitIgnoreCheckers)
                {
                    if (checker.IsIgnored(currentDir + "/"))
                    {
                        dirIgnored = true;
                        break;
                    }
                }
                if (dirIgnored) continue;

                string[]? files = null;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch { }

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        bool fileIgnored = false;
                        foreach (var checker in gitIgnoreCheckers)
                        {
                            if (checker.IsIgnored(file))
                            {
                                fileIgnored = true;
                                break;
                            }
                        }
                        if (fileIgnored) continue;

                        if (globRegex == null || globRegex.IsMatch(Path.GetFileName(file)))
                        {
                            yield return file;
                        }
                    }
                }

                string[]? subDirs = null;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch { }

                if (subDirs != null)
                {
                    foreach (var subDir in subDirs)
                    {
                        queue.Enqueue(subDir);
                    }
                }
            }
        }
    }
}
