using FractalSpecs.CodingAgent.Contracts;

namespace FractalSpecs.CodingAgent.Configuration
{
    public class GoogleProviderConfiguration : IModelProviderConfiguration
    {
        public string ProviderName => "Google";

        public string ModelProviderKey { get; set; } = "Gemini-Pro";
    }
}
