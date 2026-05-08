using FractalSpecs.AgentImplementation.Contracts;

namespace FractalSpecs.Agent.Outputs.Tools
{
    public class ApplyPatchToolCallOutput : IAgentHistoryPart
    {
        public string Patch { get; set; }
        public bool DryRun { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string ResultOutput { get; set; }

        public ApplyPatchToolCallOutput() { }

        public ApplyPatchToolCallOutput(string patch, bool dryRun)
        {
            Patch = patch;
            DryRun = dryRun;
            ResultOutput = string.Empty;
        }
    }
}
