using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs
{
    public class StatisticsOutput : IAgentHistoryPart
    {
        public int PromptTokens { get; }
        public int CompletionTokens { get; }
        public int TotalTokens { get; }
        public int ReasoningTokens { get; }

        public StatisticsOutput(int promptTokens, int completionTokens, int totalTokens, int reasoningTokens)
        {
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            TotalTokens = totalTokens;
            ReasoningTokens = reasoningTokens;
        }

        public override string ToString()
        {
            if (ReasoningTokens > 0)
            {
                return $"Tokens Usage: {PromptTokens} prompt + {CompletionTokens} completion ({ReasoningTokens} reasoning) = {TotalTokens} total";
            }
            return $"Tokens Usage: {PromptTokens} prompt + {CompletionTokens} completion = {TotalTokens} total";
        }
    }
}
