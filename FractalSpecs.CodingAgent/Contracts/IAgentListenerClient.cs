using System.Collections.Concurrent;

namespace FractalSpecs.CodingAgent.Contracts
{
    public interface IAgentListenerClient
    {
        void HistoryUpdated(Agent agent, List<IAgentHistoryPart> newHistoryParts, bool end);
    }
}
