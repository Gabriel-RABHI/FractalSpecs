using Microsoft.Extensions.AI;

namespace FractalSpecs.AgentImplementation.Contracts
{
    public interface IToolConfiguration
    {
        string ToolKey { get; }

        string Description { get; }

        AITool CreateTool();
    }
}
