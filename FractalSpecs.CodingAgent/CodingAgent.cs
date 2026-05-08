using Microsoft.Agents.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace FractalSpecs.CodingAgent
{
    public class CodingAgent : IDisposable
    {
        private AIAgent _agent;

        public CodingAgent(CodingAgentOptions options)
        {

        }

        public async IAsyncEnumerable<AgentResponseUpdate> PromptStreamingAsync(string prompt)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
