using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class FileReadToolCallOutput : IAgentHistoryPart
    {
        public string? FilePath { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public string? FromCursor { get; set; }
        public int MaxFiles { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public FileReadToolCallOutput() { }

        public FileReadToolCallOutput(string? filePath, int offset, int limit, string? fromCursor, int maxFiles)
        {
            FilePath = filePath;
            Offset = offset;
            Limit = limit;
            FromCursor = fromCursor;
            MaxFiles = maxFiles;
            ResultOutput = string.Empty;
        }
    }
}
