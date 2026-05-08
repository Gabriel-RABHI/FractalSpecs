using FractalSpecs.Agent.Constants;
using FractalSpecs.Agent.Outputs;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using OpenAI.Chat;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FractalSpecs.AgentImplementation
{
    public class GenericAgent : IDisposable
    {
        private AIAgent _agent;
        private IAgentListenerClient _client;
        private ConcurrentQueue<IAgentHistoryPart> _outputs = new ConcurrentQueue<IAgentHistoryPart>();
        private AgentStatus _status = AgentStatus.Idle;
        private AgentOptions _options;

        public GenericAgent(AgentOptions options, IAgentListenerClient client)
        {
            _options = options;
            _client = client;

            ChatClient chatClient;
            if (options.ModelProvider is Configuration.LMStudioProviderConfiguration lmConfig)
            {
                chatClient = new Providers.LMStudioModelProvider().GetChatClient(lmConfig);
            }
            else if (options.ModelProvider is Configuration.GoogleProviderConfiguration googleConfig)
            {
                chatClient = new Providers.GoogleModelProvider().GetChatClient(googleConfig);
            }
            else
            {
                throw new NotSupportedException($"Model provider type {options.ModelProvider.GetType().Name} is not supported.");
            }

            _agent = chatClient.AsAIAgent(
                name: "CodingAgent",
                instructions: options.SystemPrompt
            );
        }

        public IEnumerable<IAgentHistoryPart> History => _outputs;

        public AgentStatus Status => _status;

        public async Task RunPromptAsync(string prompt)
        {
            _status = AgentStatus.Generating;
            try
            {
                PublishHistoryPart(new ClientPromptOutput(prompt, _options.ModelProvider.ProviderName, _options.ModelProvider.ModelProviderKey));
                
                bool isThinkingMode = false;
                string tagBuffer = "";

                await foreach (var chunk in _agent.RunStreamingAsync(prompt))
                {
                    // 1. Extract reasoning / thinking content from the delta
                    string reasoningPart = "";

                    // Helper to extract reasoning from an enumerable of contents
                    void ExtractReasoning(System.Collections.IEnumerable contents)
                    {
                        if (contents == null) return;
                        foreach (var item in contents)
                        {
                            if (item == null) continue;
                            var itemType = item.GetType();
                            
                            // If type name contains Reasoning
                            if (itemType.Name.Contains("Reasoning"))
                            {
                                var textProp = itemType.GetProperty("Text") ?? itemType.GetProperty("Reasoning") ?? itemType.GetProperty("Content");
                                if (textProp != null)
                                    reasoningPart += textProp.GetValue(item)?.ToString();
                            }
                            // Or if the item itself has a property named Reasoning
                            else
                            {
                                var rProp = itemType.GetProperty("Reasoning");
                                if (rProp != null)
                                    reasoningPart += rProp.GetValue(item)?.ToString();
                            }
                        }
                    }

                    if (chunk.Contents != null)
                    {
                        ExtractReasoning(chunk.Contents);
                    }
                    
                    if (string.IsNullOrEmpty(reasoningPart) && chunk.RawRepresentation != null)
                    {
                        var contentsProp = chunk.RawRepresentation.GetType().GetProperty("Contents");
                        if (contentsProp != null)
                        {
                            ExtractReasoning(contentsProp.GetValue(chunk.RawRepresentation) as System.Collections.IEnumerable);
                        }
                    }

                    if (!string.IsNullOrEmpty(reasoningPart))
                    {
                        var reasoningOutput = new FractalSpecs.Agent.Outputs.ThinkingOutput(reasoningPart);
                        PublishHistoryPart(reasoningOutput);
                    }

                    // 2. Extract regular text and parse inline legacy tags
                    var chunkText = chunk.Text;
                    if (!string.IsNullOrEmpty(chunkText))
                    {
                        tagBuffer += chunkText;

                        while (tagBuffer.Length > 0)
                        {
                            if (!isThinkingMode)
                            {
                                int thinkIdx = tagBuffer.IndexOf("<think>");
                                int reasonIdx = tagBuffer.IndexOf("<reasoning>");
                                int minIdx = -1;
                                string tag = "";

                                if (thinkIdx != -1) { minIdx = thinkIdx; tag = "<think>"; }
                                if (reasonIdx != -1 && (minIdx == -1 || reasonIdx < minIdx)) { minIdx = reasonIdx; tag = "<reasoning>"; }

                                if (minIdx != -1)
                                {
                                    string textToPublish = tagBuffer.Substring(0, minIdx);
                                    if (!string.IsNullOrEmpty(textToPublish))
                                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput(textToPublish));
                                    
                                    isThinkingMode = true;
                                    tagBuffer = tagBuffer.Substring(minIdx + tag.Length);
                                }
                                else
                                {
                                    int partialIdx = tagBuffer.LastIndexOf('<');
                                    if (partialIdx != -1 && ("<reasoning>".StartsWith(tagBuffer.Substring(partialIdx)) || "<think>".StartsWith(tagBuffer.Substring(partialIdx))))
                                    {
                                        string textToPublish = tagBuffer.Substring(0, partialIdx);
                                        if (!string.IsNullOrEmpty(textToPublish))
                                            PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput(textToPublish));
                                        tagBuffer = tagBuffer.Substring(partialIdx);
                                        break;
                                    }
                                    else
                                    {
                                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput(tagBuffer));
                                        tagBuffer = "";
                                    }
                                }
                            }
                            else
                            {
                                int thinkIdx = tagBuffer.IndexOf("</think>");
                                int reasonIdx = tagBuffer.IndexOf("</reasoning>");
                                int minIdx = -1;
                                string tag = "";

                                if (thinkIdx != -1) { minIdx = thinkIdx; tag = "</think>"; }
                                if (reasonIdx != -1 && (minIdx == -1 || reasonIdx < minIdx)) { minIdx = reasonIdx; tag = "</reasoning>"; }

                                if (minIdx != -1)
                                {
                                    string textToPublish = tagBuffer.Substring(0, minIdx);
                                    if (!string.IsNullOrEmpty(textToPublish))
                                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(textToPublish));
                                    
                                    isThinkingMode = false;
                                    tagBuffer = tagBuffer.Substring(minIdx + tag.Length);
                                }
                                else
                                {
                                    int partialIdx = tagBuffer.LastIndexOf('<');
                                    if (partialIdx != -1 && ("</reasoning>".StartsWith(tagBuffer.Substring(partialIdx)) || "</think>".StartsWith(tagBuffer.Substring(partialIdx))))
                                    {
                                        string textToPublish = tagBuffer.Substring(0, partialIdx);
                                        if (!string.IsNullOrEmpty(textToPublish))
                                            PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(textToPublish));
                                        tagBuffer = tagBuffer.Substring(partialIdx);
                                        break;
                                    }
                                    else
                                    {
                                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(tagBuffer));
                                        tagBuffer = "";
                                    }
                                }
                            }
                        }
                    }

                    // 3. Extract usage statistics if present
                    if (chunk.RawRepresentation != null)
                    {
                        var rawType = chunk.RawRepresentation.GetType();
                        var usageProp = rawType.GetProperty("Usage");
                        if (usageProp != null)
                        {
                            var usageObj = usageProp.GetValue(chunk.RawRepresentation);
                            if (usageObj != null)
                            {
                                var usageType = usageObj.GetType();
                                var promptProp = usageType.GetProperty("InputTokenCount") ?? usageType.GetProperty("PromptTokens");
                                var completionProp = usageType.GetProperty("OutputTokenCount") ?? usageType.GetProperty("CompletionTokens");
                                var totalProp = usageType.GetProperty("TotalTokenCount") ?? usageType.GetProperty("TotalTokens");
                                
                                int promptTokens = (int?)promptProp?.GetValue(usageObj) ?? 0;
                                int completionTokens = (int?)completionProp?.GetValue(usageObj) ?? 0;
                                int totalTokens = (int?)totalProp?.GetValue(usageObj) ?? 0;
                                
                                int reasoningTokens = 0;
                                var detailsProp = usageType.GetProperty("OutputTokenDetails");
                                if (detailsProp != null)
                                {
                                    var detailsObj = detailsProp.GetValue(usageObj);
                                    if (detailsObj != null)
                                    {
                                        var rProp = detailsObj.GetType().GetProperty("ReasoningTokenCount");
                                        reasoningTokens = (int?)rProp?.GetValue(detailsObj) ?? 0;
                                    }
                                }

                                if (totalTokens > 0)
                                {
                                    PublishHistoryPart(new FractalSpecs.Agent.Outputs.StatisticsOutput(promptTokens, completionTokens, totalTokens, reasoningTokens));
                                }
                            }
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(tagBuffer))
                {
                    if (isThinkingMode) PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(tagBuffer));
                    else PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput(tagBuffer));
                }

                _client.HistoryUpdated(this, new List<IAgentHistoryPart>(), true);
            }
            finally
            {
                _status = AgentStatus.Idle;
            }
        }

        private void PublishHistoryPart(IAgentHistoryPart output)
        {
            _outputs.Enqueue(output);
            _client.HistoryUpdated(this, new List<IAgentHistoryPart> { output }, false);
        }

        public void Dispose()
        {
        }
    }
}
