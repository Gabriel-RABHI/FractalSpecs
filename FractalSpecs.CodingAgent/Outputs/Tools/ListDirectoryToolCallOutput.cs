using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class ListDirectoryToolCallOutput : IAgentHistoryPart
    {
        public string Path { get; set; }
        public bool Recursive { get; set; }
        public int MaxEntries { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public ListDirectoryToolCallOutput() { }

        public ListDirectoryToolCallOutput(string path, bool recursive, int maxEntries)
        {
            Path = path;
            Recursive = recursive;
            MaxEntries = maxEntries;
            ResultOutput = string.Empty;
        }
    }
}
