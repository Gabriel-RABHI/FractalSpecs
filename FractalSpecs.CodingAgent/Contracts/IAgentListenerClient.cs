using System.Collections.Concurrent;

namespace FractalSpecs.AgentImplementation.Contracts
{
    public interface IAgentListenerClient
    {
        void HistoryUpdated(GenericAgent agent, List<IAgentHistoryPart> newHistoryParts, bool end);
    }
}
