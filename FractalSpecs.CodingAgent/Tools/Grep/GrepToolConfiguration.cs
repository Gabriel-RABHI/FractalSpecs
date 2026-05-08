using FractalSpecs.AgentImplementation.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalSpecs.AgentImplementation.Tools.Grep
{
    public class GrepToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;

        public string ToolKey => "Grep";

        public string Description => "Search file contents using regex patterns. Uses ripgrep for fast, recursive search.";

        public GrepToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

    }
}
