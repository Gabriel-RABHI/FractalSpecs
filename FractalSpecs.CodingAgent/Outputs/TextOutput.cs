using FractalSpecs.AgentImplementation.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

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

    public class TextOutput : IAgentHistoryPart
    {
        public TextOutput(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
