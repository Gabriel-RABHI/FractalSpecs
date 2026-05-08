using FractalSpecs.CodingAgent.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalSpecs.Agent.Outputs
{
    public class TextOutput : IAgentHistoryPart
    {
        public TextOutput(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
