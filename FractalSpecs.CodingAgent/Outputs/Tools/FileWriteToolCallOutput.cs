using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class FileWriteToolCallOutput : IAgentHistoryPart
    {
        public string FilePath { get; set; }
        public string Content { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public FileWriteToolCallOutput() { }

        public FileWriteToolCallOutput(string filePath, string content)
        {
            FilePath = filePath;
            Content = content;
            ResultOutput = string.Empty;
        }
    }
}
