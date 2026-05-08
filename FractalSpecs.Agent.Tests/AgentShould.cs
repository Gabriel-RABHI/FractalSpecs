using FractalSpecs.Agent.Outputs;
using FractalSpecs.AgentImplementation;
using FractalSpecs.AgentImplementation.Configuration;
using FractalSpecs.AgentImplementation.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FractalSpecs.Agent.Tests
{
    public class AgentShould
    {
        [Fact]
        public async Task SendLMStudioPrompt()
        {
            var client = new TestAgentListenerClient();
            var options = new AgentOptions();
            options.ModelProvider = new LMStudioProviderConfiguration();
            
            var agent = new GenericAgent(options, client);
            
            await agent.RunPromptAsync("Hello, please return just the word 'Hello'");

            Assert.True(client.HistoryParts.Count > 0);
            Assert.True(client.IsEnd);
        }

        [Fact]
        public async Task SendLMStudioPromptAndGetLongResult()
        {
            var client = new TestAgentListenerClient();
            var options = new AgentOptions();
            options.ModelProvider = new LMStudioProviderConfiguration();

            var agent = new GenericAgent(options, client);

            await agent.RunPromptAsync("Write a half page about artificial intelligence and LLMs.");

            while (!client.IsEnd)
                Thread.Sleep(100);

            Assert.True(client.HistoryParts.Count > 5);
            var thinking = string.Concat(client.HistoryParts.Select(h => h is ThinkingOutput ? (((ThinkingOutput)h).Text) : ""));
            Console.WriteLine("****************************** THINKING *********************************");
            Console.WriteLine(thinking);
            var aggr = string.Concat(client.HistoryParts.Select(h => h is TextOutput ? (((TextOutput)h).Text) : ""));
            Console.WriteLine("****************************** RESPONSE *********************************");
            Console.WriteLine(aggr);
            Console.WriteLine("*************************************************************************");
            var stat = client.HistoryParts.FirstOrDefault(p => p is StatisticsOutput);
            if (stat != null)
                Console.WriteLine(stat);
            Assert.True(client.IsEnd);
        }
    }

    public class TestAgentListenerClient : IAgentListenerClient
    {
        public List<IAgentHistoryPart> HistoryParts { get; } = new List<IAgentHistoryPart>();
        public bool IsEnd { get; private set; }

        public void HistoryUpdated(AgentImplementation.GenericAgent agent, List<IAgentHistoryPart> newHistoryParts, bool end)
        {
            HistoryParts.AddRange(newHistoryParts);
            IsEnd = end;
        }
    }
}
