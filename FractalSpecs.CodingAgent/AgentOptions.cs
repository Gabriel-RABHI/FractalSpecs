using FractalSpecs.CodingAgent.Configuration;
using FractalSpecs.CodingAgent.Contracts;

namespace FractalSpecs.CodingAgent
{
    public class AgentOptions
    {
        public IModelProviderConfiguration ModelProvider { get; set; } = new LMStudioProviderConfiguration();

        public List<string> ReadPaths { get; set; } = new List<string> { "c:/" };

        public List<string> WritePaths { get; set; } = new List<string> { "c:/" };

        public List<IToolConfiguration> Tools { get; set; } = new List<IToolConfiguration>();

        public string SystemPrompt { get; set; } = "You are a professional developper.";
    }
}
