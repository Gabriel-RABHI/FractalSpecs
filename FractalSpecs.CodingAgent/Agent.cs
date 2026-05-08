using FractalSpecs.Agent.Constants;
using FractalSpecs.CodingAgent.Contracts;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FractalSpecs.CodingAgent
{
    public class Agent : IDisposable
    {
        private AIAgent _agent;
        private IAgentListenerClient _client;
        private ConcurrentQueue<IAgentHistoryPart> _outputs = new ConcurrentQueue<IAgentHistoryPart>();

        public Agent(AgentOptions options, IAgentListenerClient client)
        {
            _client = client;
        }

        public IEnumerable<IAgentHistoryPart> History => _outputs;

        public AgentStatus Status { get; }

        public void RunPrompt(string prompt)
        {
        }

        public void Dispose()
        {
        }
    }
}
