using FractalSpecs.Agent.Constants;
using FractalSpecs.Agent.Outputs;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
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

            _options.OwnerAgent = this;

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

            var tools = options.Tools?.Select(t => t.CreateTool()).ToList();

            _agent = chatClient.AsAIAgent(
                name: "CodingAgent",
                instructions: options.SystemPrompt,
                tools: tools
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
                    if (chunk.Contents != null)
                    {
                        foreach (var item in chunk.Contents)
                        {
                            switch (item)
                            {
                                case TextReasoningContent reasoning:
                                    if (!string.IsNullOrEmpty(reasoning.Text))
                                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(reasoning.Text));
                                    break;

                                case UsageContent usage:
                                    if (usage.Details != null)
                                    {
                                        long promptTokens = usage.Details.InputTokenCount ?? 0;
                                        long completionTokens = usage.Details.OutputTokenCount ?? 0;
                                        long totalTokens = usage.Details.TotalTokenCount ?? 0;
                                        long reasoningTokens = 0;

                                        // Fallback checks for reasoning tokens in usage
                                        if (usage.AdditionalProperties != null)
                                        {
                                            if (usage.AdditionalProperties.TryGetValue("ReasoningTokenCount", out object rObj) && rObj is IConvertible rVal)
                                                reasoningTokens = rVal.ToInt64(null);
                                            else if (usage.AdditionalProperties.TryGetValue("OutputTokenDetails", out object outDetails) && outDetails != null)
                                            {
                                                var rProp = outDetails.GetType().GetProperty("ReasoningTokenCount");
                                                if (rProp != null)
                                                {
                                                    var val = rProp.GetValue(outDetails);
                                                    if (val is IConvertible vConv) reasoningTokens = vConv.ToInt64(null);
                                                }
                                            }
                                        }
                                        
                                        // Some Microsoft.Extensions.AI preview versions store it in UsageDetails.AdditionalCounts
                                        if (reasoningTokens == 0)
                                        {
                                            var addCountsProp = usage.Details.GetType().GetProperty("AdditionalCounts");
                                            if (addCountsProp != null)
                                            {
                                                var dict = addCountsProp.GetValue(usage.Details) as System.Collections.IDictionary;
                                                if (dict != null && dict.Contains("ReasoningTokenCount"))
                                                {
                                                    var val = dict["ReasoningTokenCount"];
                                                    if (val is IConvertible vConv) reasoningTokens = vConv.ToInt64(null);
                                                }
                                            }
                                        }

                                        if (totalTokens > 0)
                                            PublishHistoryPart(new FractalSpecs.Agent.Outputs.StatisticsOutput(promptTokens, completionTokens, totalTokens, reasoningTokens));
                                    }
                                    break;

                                case FunctionCallContent functionCall:
                                    // TODO: Implement Function Call extraction
                                    break;

                                case McpServerToolCallContent mcpServerToolCall:
                                    // TODO: Implement MCP Server Tool Call extraction
                                    break;

                                case ToolCallContent toolCall:
                                    // TODO: Implement Tool Call extraction
                                    break;

                                case ErrorContent errorContent:
                                    // Handle AI generation errors
                                    PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput($"[Error] Received error from model: {errorContent}"));
                                    break;

                                case DataContent dataContent:
                                    // TODO: Handle binary data or base64 streams
                                    break;

                                case UriContent uriContent:
                                    // TODO: Handle URI references
                                    break;

                                case HostedFileContent hostedFileContent:
                                    // TODO: Handle hosted file references
                                    break;

                                case HostedVectorStoreContent vectorStoreContent:
                                    // TODO: Handle vector store content
                                    break;

                                case FunctionResultContent functionResult:
                                    // TODO: Handle function execution results
                                    break;

                                case McpServerToolResultContent mcpServerToolResult:
                                    // TODO: Handle MCP server tool results
                                    break;

                                case ToolResultContent toolResult:
                                    // TODO: Handle tool execution results
                                    break;

                                case ToolApprovalRequestContent toolApprovalRequest:
                                    // TODO: Handle requests for manual tool approval
                                    break;

                                case ToolApprovalResponseContent toolApprovalResponse:
                                    // TODO: Handle tool approval decisions
                                    break;

                                case InputRequestContent inputRequest:
                                    // TODO: Handle agent asking for input
                                    break;

                                case InputResponseContent inputResponse:
                                    // TODO: Handle user providing input
                                    break;

                                case TextContent textContent:
                                    string chunkText = textContent.Text;
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
                                    break;
                            }
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(tagBuffer))
                {
                    if (isThinkingMode)
                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.ThinkingOutput(tagBuffer));
                    else
                        PublishHistoryPart(new FractalSpecs.Agent.Outputs.TextOutput(tagBuffer));
                }

                _client.HistoryUpdated(this, new List<IAgentHistoryPart>(), true);
            }
            finally
            {
                _status = AgentStatus.Idle;
            }
        }

        internal void PublishHistoryPart(IAgentHistoryPart output)
        {
            _outputs.Enqueue(output);
            _client.HistoryUpdated(this, new List<IAgentHistoryPart> { output }, false);
        }

        public void Dispose()
        {
        }
    }
}
