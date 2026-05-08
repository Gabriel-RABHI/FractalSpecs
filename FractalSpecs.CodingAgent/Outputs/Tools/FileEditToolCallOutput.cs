using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class FileEditToolCallOutput : IAgentHistoryPart
    {
        public string FilePath { get; set; }
        public string OldString { get; set; }
        public string NewString { get; set; }
        public bool ReplaceAll { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public FileEditToolCallOutput() { }

        public FileEditToolCallOutput(string filePath, string oldString, string newString, bool replaceAll)
        {
            FilePath = filePath;
            OldString = oldString;
            NewString = newString;
            ReplaceAll = replaceAll;
            ResultOutput = string.Empty;
        }
    }
}
