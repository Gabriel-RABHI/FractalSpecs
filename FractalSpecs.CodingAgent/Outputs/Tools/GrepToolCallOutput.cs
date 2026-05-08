using FractalSpecs.AgentImplementation.Contracts;
using System;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class GrepToolCallOutput : IAgentHistoryPart
    {
        public string Pattern { get; set; }
        public string Path { get; set; }
        public string? Glob { get; set; }
        public bool CaseInsensitive { get; set; }
        public int ContextLines { get; set; }
        public int MaxResults { get; set; }

        public string? ErrorMessage { get; set; }
        public int MatchCount { get; set; }
        public string ResultOutput { get; set; }

        public GrepToolCallOutput() { }

        public GrepToolCallOutput(string pattern, string path, string? glob, bool caseInsensitive, int contextLines, int maxResults)
        {
            Pattern = pattern;
            Path = path;
            Glob = glob;
            CaseInsensitive = caseInsensitive;
            ContextLines = contextLines;
            MaxResults = maxResults;
            ResultOutput = string.Empty;
        }
    }
}
