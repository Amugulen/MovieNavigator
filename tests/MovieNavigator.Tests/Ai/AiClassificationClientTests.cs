using System.Net;
using System.Text.Json;
using FluentAssertions;
using MovieNavigator.Core.Ai;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Ai;

namespace MovieNavigator.Tests.Ai;

public sealed class AiClassificationClientTests
{
    [Fact]
    public async Task Suggest_tags_sends_text_only_clues_to_chat_completions()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"title\":\"Come and See\",\"year\":1985,\"summary\":\"Anti-war film\",\"tags\":[\"country.soviet_union\",\"genre.war\"],\"confidence\":0.72,\"notes\":\"filename and tags matched\"}"
                  }
                }
              ]
            }
            """)
        });
        using var httpClient = new HttpClient(handler);
        var client = new OpenAiCompatibleClassificationClient(httpClient);
        var settings = new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "movie-classifier", true, "secret-key");
        var request = CreateRequest();

        var suggestion = await client.SuggestAsync(settings, request, CancellationToken.None);

        handler.RequestUri.Should().Be("https://api.example.test/v1/chat/completions");
        handler.AuthorizationHeader.Should().Be("Bearer secret-key");
        handler.RequestJson.Should().NotBeNull();
        handler.RequestJson!.RootElement.GetProperty("model").GetString().Should().Be("movie-classifier");
        var userPayloadText = handler.RequestJson.RootElement
            .GetProperty("messages")[1]
            .GetProperty("content")
            .GetString();
        userPayloadText.Should().NotBeNull();
        using var userPayload = JsonDocument.Parse(userPayloadText!);
        userPayload.RootElement.GetProperty("fileName").GetString().Should().Be("Come.and.See.1985.mkv");
        userPayload.RootElement.GetProperty("folderPath").GetString().Should().Be(@"D:\Movies\Soviet");
        userPayload.RootElement.GetProperty("manualIdentifier").GetString().Should().Be("tt0091251");
        userPayload.RootElement.GetProperty("manualUrl").GetString().Should().Be("https://example.test/movie");
        userPayload.RootElement.GetProperty("existingTags")[0].GetString().Should().Be("country.soviet_union");
        userPayload.RootElement.GetProperty("duration").GetString().Should().Be("02:22:00");
        userPayload.RootElement.GetProperty("resolution").GetString().Should().Be("1920x1080");
        userPayload.RootElement.GetProperty("libraryType").GetString().Should().Be("Normal");
        userPayloadText.Should().NotContain("thumbnail");
        userPayloadText.Should().NotContain("screenshot");
        userPayloadText.Should().NotContain("audio");
        userPayloadText.Should().NotContain("video bytes");
        suggestion.Tags.Select(tag => tag.Value).Should().Equal("country.soviet_union", "genre.war");
        suggestion.Title.Should().Be("Come and See");
        suggestion.Confidence.Should().Be(0.72);
    }

    [Fact]
    public async Task Suggest_tags_rejects_invalid_json_without_returning_partial_suggestion()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
            {
              "choices": [
                {
                  "message": {
                    "content": "not json"
                  }
                }
              ]
            }
            """)
        });
        using var httpClient = new HttpClient(handler);
        var client = new OpenAiCompatibleClassificationClient(httpClient);

        var act = async () => await client.SuggestAsync(
            new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "movie-classifier", true, "secret-key"),
            CreateRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*invalid JSON*");
    }

    [Fact]
    public async Task Suggest_tags_rejects_invalid_tag_key_without_returning_partial_suggestion()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"title\":\"Unknown\",\"tags\":[\"bad tag\"],\"confidence\":0.1}"
                  }
                }
              ]
            }
            """)
        });
        using var httpClient = new HttpClient(handler);
        var client = new OpenAiCompatibleClassificationClient(httpClient);

        var act = async () => await client.SuggestAsync(
            new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "movie-classifier", true, "secret-key"),
            CreateRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*invalid tag*");
    }

    [Fact]
    public async Task Suggest_tags_reports_invalid_base_url_as_configuration_error()
    {
        using var httpClient = new HttpClient(new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new OpenAiCompatibleClassificationClient(httpClient);

        var act = async () => await client.SuggestAsync(
            new AiSettings("OpenAI-compatible", "not a url", "movie-classifier", true, "secret-key"),
            CreateRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Base URL*");
    }

    [Fact]
    public void Confirmation_preview_lists_exactly_the_text_fields_sent_to_ai()
    {
        var request = CreateRequest();

        request.ToConfirmationLines().Should().Equal(
            "文件名: Come.and.See.1985.mkv",
            @"文件夹路径: D:\Movies\Soviet",
            "手动标题: Come and See",
            "手动标识/番号: tt0091251",
            "手动网址: https://example.test/movie",
            "已有TAG: country.soviet_union",
            "时长: 02:22:00",
            "分辨率: 1920x1080",
            "库类型: Normal");
    }

    private static AiClassificationRequest CreateRequest()
    {
        return new AiClassificationRequest(
            FileName: "Come.and.See.1985.mkv",
            FolderPath: @"D:\Movies\Soviet",
            ManualTitle: "Come and See",
            ManualIdentifier: "tt0091251",
            ManualUrl: "https://example.test/movie",
            ExistingTags: [TagKey.Parse("country.soviet_union")],
            Duration: TimeSpan.FromMinutes(142),
            Width: 1920,
            Height: 1080,
            LibraryType: MediaLibraryType.Normal);
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CapturingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public string? RequestUri { get; private set; }
        public string? AuthorizationHeader { get; private set; }
        public JsonDocument? RequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            AuthorizationHeader = request.Headers.Authorization?.ToString();
            RequestJson = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            return _response;
        }
    }
}
