using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class RoslynToolCallOutput : IAgentHistoryPart
    {
        public string Action { get; set; }
        public string Target { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public RoslynToolCallOutput() { }

        public RoslynToolCallOutput(string action, string target)
        {
            Action = action;
            Target = target;
            ResultOutput = string.Empty;
        }
    }
}
