using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.AgentImplementation.Configuration
{
    public class GoogleProviderConfiguration : IModelProviderConfiguration
    {
        public string ProviderName => "Google";

        public string ModelProviderKey { get; set; } = "Gemini-Pro";
    }
}
