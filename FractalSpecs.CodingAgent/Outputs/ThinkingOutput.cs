using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs
{
    public class ThinkingOutput : IAgentHistoryPart
    {
        public ThinkingOutput(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
