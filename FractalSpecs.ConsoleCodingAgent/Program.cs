using System;
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI; // Use the OpenAI connector for Microsoft Agent Framework
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "http://localhost:1234/v1";
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "lm-studio";

// Because LM Studio exposes a standard OpenAI-compatible API (not Azure API) and uses HTTP, 
// we MUST use OpenAIClient instead of Azure's AIProjectClient. AIProjectClient enforces HTTPS 
// for TokenCredentials and formats requests specifically for Azure Foundry endpoints.
var openAIClient = new OpenAIClient(new ApiKeyCredential("lm-studio-local"), new OpenAIClientOptions {
    Endpoint = new Uri(endpoint)
});
var chatClient = openAIClient.GetChatClient(deploymentName);

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location) => $"The weather in {location} is cloudy with a high of 15°C.";

// The core Agent Framework abstraction works identical to the tutorial using .AsAIAgent()
AIAgent agent = chatClient.AsAIAgent(
    name: "LocalBot",
    instructions: "You are a friendly, helpful AI assistant running locally.",
    tools: [AIFunctionFactory.Create(GetWeather)]
);

Console.WriteLine("LM Studio Console Chat App using Microsoft Agent Framework initialized.");
Console.WriteLine("Type your message and press Enter. Type 'exit' to quit.\n");

while (true)
{
    Console.Write("User: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    Console.Write("Agent: ");
    try
    {
        var response = await agent.RunAsync(input);
        Console.WriteLine(response);
    } catch (Exception ex)
    {
        Console.WriteLine($"\n[Error]: {ex.Message}");
    }
}
