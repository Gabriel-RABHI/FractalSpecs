using FractalSpecs.CodingAgent.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalSpecs.CodingAgent.Tools.Grep
{
    public class GrepToolConfiguration : IToolConfiguration
    {
        private CodingAgentOptions _parent;

        public string ToolKey => "Grep";

        public string Description => "Search file contents using regex patterns. Uses ripgrep for fast, recursive search.";

        public GrepToolConfiguration(CodingAgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

    }
}
