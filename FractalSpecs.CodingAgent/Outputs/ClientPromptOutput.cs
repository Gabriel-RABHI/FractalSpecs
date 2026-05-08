using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs
{
    public class ClientPromptOutput : IAgentHistoryPart
    {
        public ClientPromptOutput(string text, string providerName, string modelProviderKey)
        {
            Text = text;
            ProviderName = providerName;
            ModelProviderKey = modelProviderKey;
        }

        public string ProviderName { get; }

        public string ModelProviderKey { get; }

        public string Text { get; }
    }
}
