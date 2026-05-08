using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class WebFetchToolCallOutput : IAgentHistoryPart
    {
        public string Url { get; set; }
        public int MaxLength { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public WebFetchToolCallOutput() { }

        public WebFetchToolCallOutput(string url, int maxLength)
        {
            Url = url;
            MaxLength = maxLength;
            ResultOutput = string.Empty;
        }
    }
}
