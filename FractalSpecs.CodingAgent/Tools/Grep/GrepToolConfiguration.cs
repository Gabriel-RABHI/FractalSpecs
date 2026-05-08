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

        [Description("Search file contents using regex patterns. Uses native C# search.")]
        public async Task<string> SearchAsync(
            [Description("Regex pattern to search for")] string pattern,
            [Description("File or directory to search in")] string path = ".",
            [Description("Glob filter for files (e.g. *.cs)")] string? glob = null,
            [Description("Case insensitive search")] bool case_insensitive = false,
            [Description("Lines of context around matches")] int context_lines = 0,
            [Description("Maximum number of results (default: 250)")] int max_results = 250)
        {
            var workingDirectory = _parent.ReadPaths.FirstOrDefault() ?? "c:/";
            var searchPath = path == "." ? workingDirectory : Path.GetFullPath(path, workingDirectory);

            try
            {
                var regexOptions = case_insensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
                var regex = new Regex(pattern, regexOptions);

                var filesToSearch = new List<string>();
                if (File.Exists(searchPath))
                {
                    filesToSearch.Add(searchPath);
                }
                else if (Directory.Exists(searchPath))
                {
                    filesToSearch.AddRange(EnumerateFilesSafe(searchPath, glob));
                }
                else
                {
                    return $"Error: Path '{searchPath}' does not exist.";
                }

                var results = new List<string>();
                int matchCount = 0;
                
                foreach (var file in filesToSearch)
                {
                    if (matchCount >= max_results) break;
                    
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

                if (matchCount == 0)
                    return $"No matches for pattern '{pattern}' in {searchPath}";

                var truncated = matchCount >= max_results;
                var output = string.Join("\n", results);

                var header = truncated
                    ? $"Showing first {max_results} matches (truncated):"
                    : $"Found {matchCount} match(es):";

                return $"{header}\n{output}";
            }
            catch (Exception ex)
            {
                return $"Error: Grep error: {ex.Message}";
            }
        }

        private IEnumerable<string> EnumerateFilesSafe(string rootPath, string? glob)
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
