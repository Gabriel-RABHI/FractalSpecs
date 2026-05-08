using FractalSpecs.AgentImplementation.Configuration;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalSpecs.AgentImplementation.Providers
{
    public class LMStudioModelProvider
    {
        public ChatClient GetChatClient(LMStudioProviderConfiguration configuration)
        {
            var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("lm-studio-local"), new OpenAIClientOptions {
                Endpoint = new Uri(configuration.EndPoint)
            });
            return openAIClient.GetChatClient(configuration.ModelProviderKey);
        }
    }
}
