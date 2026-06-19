using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UserDomain;

public class AnthropicService : IAnthropicService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AnthropicService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Anthropic:ApiKey"] ?? throw new ArgumentNullException("Anthropic:ApiKey");
        _model = config["Anthropic:Model"] ?? "claude-sonnet-4-6";

        _http.BaseAddress ??= new Uri("https://api.anthropic.com/");
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> SendContextAndGetRawResponseAsync(IList<MessageDto> context, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(context);

        var payload = new
        {
            model = _model,
            max_tokens = 1024,
            messages
        };

        var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");
         var resp = await _http.PostAsync("v1/messages", content, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Claude API [{(int)resp.StatusCode}]: {raw}");

        resp.EnsureSuccessStatusCode();
        

        return raw;
    }

    private static List<object> BuildMessages(IList<MessageDto> context)
    {
        var messages = new List<object>();

        foreach (var m in context)
        {
            if (string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                continue; // handled separately if needed via system param

            messages.Add(new
            {
                role = m.Role.ToLower(), // "user" or "assistant"
                content = m.Content
            });
        }

        return messages;
    }
}