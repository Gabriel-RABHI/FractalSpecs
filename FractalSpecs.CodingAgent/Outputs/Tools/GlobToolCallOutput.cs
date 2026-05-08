using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class GlobToolCallOutput : IAgentHistoryPart
    {
        public string Pattern { get; set; }
        public string Path { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public GlobToolCallOutput() { }

        public GlobToolCallOutput(string pattern, string path)
        {
            Pattern = pattern;
            Path = path;
            ResultOutput = string.Empty;
        }
    }
}
