using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class WebSearchToolCallOutput : IAgentHistoryPart
    {
        public string Query { get; set; }
        public int MaxResults { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public WebSearchToolCallOutput() { }

        public WebSearchToolCallOutput(string query, int maxResults)
        {
            Query = query;
            MaxResults = maxResults;
            ResultOutput = string.Empty;
        }
    }
}
