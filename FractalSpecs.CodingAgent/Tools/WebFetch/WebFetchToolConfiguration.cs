using FractalSpecs.Agent.Outputs.Tools;
using FractalSpecs.AgentImplementation.Contracts;
using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FractalSpecs.AgentImplementation.Tools.WebFetch
{
    public class WebFetchToolConfiguration : IToolConfiguration
    {
        private AgentOptions _parent;
        
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestHeaders =
            {
                { "User-Agent", "FractalSpecs.Agent/0.1 (coding-agent)" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.7" },
            }
        };

        public string ToolKey => "WebFetch";

        public string Description => "Fetch a web page and extract its text content. Returns the page text with HTML tags stripped.";

        public WebFetchToolConfiguration(AgentOptions parentOptions)
        {
            _parent = parentOptions;
        }

        public AITool CreateTool()
        {
            return AIFunctionFactory.Create(WebFetchAsync, ToolKey, Description);
        }

        [Description("Fetch a web page and extract its text content. Returns the page text with HTML tags stripped.")]
        public async Task<string> WebFetchAsync(
            [Description("The URL to fetch")] string url,
            [Description("Maximum characters to return (default: 20000)")] int max_length = 20000)
        {
            var outputInfo = new WebFetchToolCallOutput(url, max_length);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                outputInfo.ErrorMessage = $"Error: Invalid URL: {url}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await Http.SendAsync(request);
                var statusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    outputInfo.ErrorMessage = $"Error: HTTP {statusCode} {response.StatusCode} for {url}";
                    _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                    return outputInfo.ErrorMessage;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                var body = await response.Content.ReadAsStringAsync();

                string text;
                if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    text = ExtractTextFromHtml(body);
                else
                    text = body;

                if (text.Length > max_length)
                    text = text.Substring(0, max_length) + $"\n\n... (truncated at {max_length} chars, total: {text.Length})";

                var finalOutput = $"[{statusCode}] {url} ({text.Length} chars)\n\n{text}";
                outputInfo.ResultOutput = finalOutput;
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return finalOutput;
            }
            catch (TaskCanceledException)
            {
                outputInfo.ErrorMessage = $"Error: Request timed out (30s): {url}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (HttpRequestException ex)
            {
                outputInfo.ErrorMessage = $"Error: HTTP error fetching {url}: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
            catch (Exception ex)
            {
                outputInfo.ErrorMessage = $"Error: Unexpected error fetching {url}: {ex.Message}";
                _parent.OwnerAgent?.PublishHistoryPart(outputInfo);
                return outputInfo.ErrorMessage;
            }
        }

        private static string ExtractTextFromHtml(string html)
        {
            var cleaned = ScriptPattern().Replace(html, " ");
            cleaned = StylePattern().Replace(cleaned, " ");
            cleaned = TagPattern().Replace(cleaned, " ");
            cleaned = WebUtility.HtmlDecode(cleaned);
            cleaned = WhitespacePattern().Replace(cleaned, " ");

            var lines = cleaned.Split('\n', StringSplitOptions.TrimEntries)
                .Where(l => l.Length > 0);

            return string.Join('\n', lines).Trim();
        }

        private static Regex ScriptPattern() => new Regex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase);
        private static Regex StylePattern() => new Regex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase);
        private static Regex TagPattern() => new Regex(@"<[^>]+>");
        private static Regex WhitespacePattern() => new Regex(@"[ \t]{2,}");
    }
}
