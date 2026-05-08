namespace FractalSpecs.AgentImplementation.Contracts
{
    public interface IModelProviderConfiguration
    {
        string ProviderName { get; }

        string ModelProviderKey { get; set; }
    }
}
