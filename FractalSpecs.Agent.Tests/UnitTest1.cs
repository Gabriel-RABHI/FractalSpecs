using FractalSpecs.CodingAgent;
using FractalSpecs.CodingAgent.Contracts;

namespace FractalSpecs.Agent.Tests
{
    public class AgentShould
    {
        [Fact]
        public void SendLMStudioPrompt()
        {
            var client = new TestAgentListenerClient();
            var agent = new FractalSpecs.CodingAgent.Agent(new AgentOptions(), client);
        }
    }

    public class TestAgentListenerClient : IAgentListenerClient
    {
        public void HistoryUpdated(CodingAgent.Agent agent, List<IAgentHistoryPart> newHistoryParts, bool end)
        {

        }
    }
}
