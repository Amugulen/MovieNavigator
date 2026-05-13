using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Ai;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Infrastructure.Ai;

public sealed class OpenAiCompatibleClassificationClient : IAiClassificationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public OpenAiCompatibleClassificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiClassificationSuggestion> SuggestAsync(
        AiSettings settings,
        AiClassificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!settings.IsEnabled)
        {
            throw new InvalidOperationException("AI is disabled.");
        }

        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            throw new InvalidOperationException("AI model is required.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildCompletionsUri(settings.BaseUrl));
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }

        httpRequest.Content = JsonContent.Create(CreateChatRequest(settings.Model, request), options: JsonOptions);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("AI response was empty.");
        var content = chatResponse.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI response did not include a suggestion.");
        }

        return ParseSuggestion(content);
    }

    private static Uri BuildCompletionsUri(string baseUrl)
    {
        var normalized = baseUrl.TrimEnd('/');
        if (!Uri.TryCreate($"{normalized}/chat/completions", UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("AI Base URL is invalid.");
        }

        return uri;
    }

    private static ChatCompletionRequest CreateChatRequest(string model, AiClassificationRequest request)
    {
        return new ChatCompletionRequest(
            model,
            [
                new ChatMessage(
                    "system",
                    "You classify local movie files from text only. Do not ask for or infer from screenshots, thumbnails, audio, or video bytes. Return only JSON matching: {\"title\":\"string\",\"year\":1970,\"summary\":\"string\",\"tags\":[\"country.soviet_union\"],\"confidence\":0.72,\"notes\":\"string\"}."),
                new ChatMessage("user", JsonSerializer.Serialize(request.ToTextOnlyPayload(), JsonOptions))
            ],
            0.2);
    }

    private static AiClassificationSuggestion ParseSuggestion(string content)
    {
        AiSuggestionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AiSuggestionDto>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("AI returned invalid JSON.", ex);
        }

        if (dto is null)
        {
            throw new InvalidOperationException("AI returned invalid JSON.");
        }

        var tags = new List<TagKey>();
        foreach (var tag in dto.Tags ?? [])
        {
            try
            {
                tags.Add(TagKey.Parse(tag));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"AI returned invalid tag: {tag}", ex);
            }
        }

        return new AiClassificationSuggestion(
            dto.Title,
            dto.Year,
            dto.Summary,
            tags,
            dto.Confidence,
            dto.Notes);
    }

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessageResponse Message);

    private sealed record ChatMessageResponse(string Content);

    private sealed record AiSuggestionDto(
        string? Title,
        int? Year,
        string? Summary,
        IReadOnlyList<string>? Tags,
        double Confidence,
        string? Notes);
}
