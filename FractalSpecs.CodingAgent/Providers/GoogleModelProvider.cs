using FractalSpecs.AgentImplementation.Configuration;
using OpenAI;
using OpenAI.Chat;
using System;

namespace FractalSpecs.AgentImplementation.Providers
{
    public class GoogleModelProvider
    {
        public ChatClient GetChatClient(GoogleProviderConfiguration configuration)
        {
            string apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? "dummy-key";
            var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), new OpenAIClientOptions {
                Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
            });
            return openAIClient.GetChatClient(configuration.ModelProviderKey);
        }
    }
}
