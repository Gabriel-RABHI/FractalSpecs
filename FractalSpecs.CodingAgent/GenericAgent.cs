using FractalSpecs.Agent.Constants;
using FractalSpecs.Agent.Outputs;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using OpenAI.Chat;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FractalSpecs.AgentImplementation
{
    public class GenericAgent : IDisposable
    {
        private AIAgent _agent;
        private IAgentListenerClient _client;
        private ConcurrentQueue<IAgentHistoryPart> _outputs = new ConcurrentQueue<IAgentHistoryPart>();
        private AgentStatus _status = AgentStatus.Idle;
        private AgentOptions _options;

        public GenericAgent(AgentOptions options, IAgentListenerClient client)
        {
            _options = options;
            _client = client;

            ChatClient chatClient;
            if (options.ModelProvider is Configuration.LMStudioProviderConfiguration lmConfig)
            {
                chatClient = new Providers.LMStudioModelProvider().GetChatClient(lmConfig);
            }
            else if (options.ModelProvider is Configuration.GoogleProviderConfiguration googleConfig)
            {
                chatClient = new Providers.GoogleModelProvider().GetChatClient(googleConfig);
            }
            else
            {
                throw new NotSupportedException($"Model provider type {options.ModelProvider.GetType().Name} is not supported.");
            }

            _agent = chatClient.AsAIAgent(
                name: "CodingAgent",
                instructions: options.SystemPrompt
            );
        }

        public IEnumerable<IAgentHistoryPart> History => _outputs;

        public AgentStatus Status => _status;

        public async Task RunPromptAsync(string prompt)
        {
            _status = AgentStatus.Generating;
            try
            {
                PublishHistoryPart(new ClientPromptOutput(prompt, _options.ModelProvider.ProviderName, _options.ModelProvider.ModelProviderKey));
                await foreach (var chunk in _agent.RunStreamingAsync(prompt))
                {
                    var chunkText = chunk.ToString();
                    if (!string.IsNullOrEmpty(chunkText))
                    {
                        var output = new FractalSpecs.Agent.Outputs.TextOutput(chunkText);
                        PublishHistoryPart(output);
                    }
                }
                
                _client.HistoryUpdated(this, new List<IAgentHistoryPart>(), true);
            }
            finally
            {
                _status = AgentStatus.Idle;
            }
        }

        private void PublishHistoryPart(IAgentHistoryPart output)
        {
            _outputs.Enqueue(output);
            _client.HistoryUpdated(this, new List<IAgentHistoryPart> { output }, false);
        }

        public void Dispose()
        {
        }
    }
}
