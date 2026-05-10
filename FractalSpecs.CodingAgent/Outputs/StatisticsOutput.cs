using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs
{
    public class StatisticsOutput : IAgentHistoryPart
    {
        public long PromptTokens { get; }
        public long CompletionTokens { get; }
        public long TotalTokens { get; }
        public long ReasoningTokens { get; }

        public StatisticsOutput(long promptTokens, long completionTokens, long totalTokens, long reasoningTokens)
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
