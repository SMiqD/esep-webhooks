using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    public async Task<string> FunctionHandler(JsonElement input, ILambdaContext context)
    {
        if (input.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Expected a GitHub webhook JSON object.");
        }

        if (!input.TryGetProperty("issue", out var issue))
        {
            context.Logger.LogLine("Ignored an event without an issue, such as a GitHub ping.");
            return "ignored";
        }

        if (issue.ValueKind != JsonValueKind.Object ||
            !issue.TryGetProperty("html_url", out var url) ||
            url.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(url.GetString()))
        {
            throw new ArgumentException("The webhook is missing issue.html_url.");
        }

        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL")
            ?? throw new InvalidOperationException("The SLACK_URL environment variable is not set.");

        if (string.IsNullOrWhiteSpace(slackUrl))
        {
            throw new InvalidOperationException("The SLACK_URL environment variable is empty.");
        }

        string payload = JsonSerializer.Serialize(new
        {
            text = $"Issue Created: {url.GetString()}"
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        HttpResponseMessage response;

        try
        {
            response = await Client.PostAsync(slackUrl, content);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("Could not reach Slack.");
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException("The Slack request timed out.");
        }

        using (response)
        {
            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.Trim() != "ok")
            {
                throw new InvalidOperationException(
                    $"Slack did not accept the message (HTTP {(int)response.StatusCode}).");
            }

            context.Logger.LogLine("Posted the issue link to Slack.");
            return "ok";
        }
    }
}
