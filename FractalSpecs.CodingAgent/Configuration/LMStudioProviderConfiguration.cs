using FractalSpecs.AgentImplementation.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalSpecs.AgentImplementation.Configuration
{
    public class LMStudioProviderConfiguration : IModelProviderConfiguration
    {
        public string ProviderName => "LM Studio";

        public string ModelProviderKey { get; set; } = "LM-Studio";

        public string EndPoint { get; set; } = "http://localhost:1234/v1";
    }
}
