using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.WebSearch
{
    public class WebSearchToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;
        
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders =
            {
                { "User-Agent", "FractalSpecs.Agent/0.1 (coding-agent)" },
            }
        };

        public string ToolKey => "WebSearch";

        public string Description => "Search the web using DuckDuckGo. Returns titles, URLs, and snippets for the top results.";

        public WebSearchToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(WebSearchAsync, ToolKey, Description);
        }

        [Description("Search the web using DuckDuckGo. Returns titles, URLs, and snippets for the top results.")]
        public async Task<string> WebSearchAsync(
            [Description("The search query")] string query,
            [Description("Maximum number of results (default: 8, max: 20)")] int max_results = 8)
        {
            var outputInfo = new WebSearchToolCallOutput(query, max_results);
            max_results = Math.Min(max_results, 20);

            try
            {
                var encoded = Uri.EscapeDataString(query);
                var url = $"https://html.duckduckgo.com/html/?q={encoded}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "text/html");

                using var response = await Http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();

                var results = ParseResults(html, max_results);

                if (results.Count == 0)
                {
                    outputInfo.ResultOutput = $"No results found for: {query}";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ResultOutput;
                }

                var output = new System.Text.StringBuilder();
                output.AppendLine($"Search results for: {query}\n");

                for (var i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    output.AppendLine($"{i + 1}. {r.Title}");
                    output.AppendLine($"   {r.Url}");
                    if (!string.IsNullOrEmpty(r.Snippet))
                        output.AppendLine($"   {r.Snippet}");
                    output.AppendLine();
                }

                var finalOutput = output.ToString().TrimEnd();
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (TaskCanceledException)
            {
                outputInfo.ErrorMessage = $"Error: Search timed out (15s): {query}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (HttpRequestException ex)
            {
                outputInfo.ErrorMessage = $"Error: Search failed: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error: Unexpected search error: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private static List<SearchResult> ParseResults(string html, int maxResults)
        {
            var results = new List<SearchResult>();

            var linkMatches = ResultLinkPattern().Matches(html);

            foreach (Match match in linkMatches)
            {
                if (results.Count >= maxResults) break;

                var href = WebUtility.HtmlDecode(match.Groups[1].Value);
                var title = WebUtility.HtmlDecode(StripTags().Replace(match.Groups[2].Value, "")).Trim();

                if (string.IsNullOrEmpty(title) || href.Contains("duckduckgo.com")) continue;

                if (href.StartsWith("//duckduckgo.com/l/?uddg="))
                {
                    var uddg = Uri.UnescapeDataString(href.Split(new[] { "uddg=" }, StringSplitOptions.None)[^1].Split('&')[0]);
                    href = uddg;
                }

                results.Add(new SearchResult { Title = title, Url = href });
            }

            var snippetMatches = SnippetPattern().Matches(html);
            for (var i = 0; i < Math.Min(snippetMatches.Count, results.Count); i++)
            {
                var snippet = WebUtility.HtmlDecode(
                    StripTags().Replace(snippetMatches[i].Groups[1].Value, "")).Trim();
                results[i].Snippet = snippet;
            }

            return results;
        }

        private static Regex ResultLinkPattern() => new Regex(@"<a[^>]*class=""result__a""[^>]*href=""([^""]+)""[^>]*>(.*?)</a>", RegexOptions.Singleline);
        private static Regex SnippetPattern() => new Regex(@"<a[^>]*class=""result__snippet""[^>]*>(.*?)</a>", RegexOptions.Singleline);
        private static Regex StripTags() => new Regex(@"<[^>]+>");

        private class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string? Snippet { get; set; }
        }
    }
}
