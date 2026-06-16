using Continuum.Models;
using Continuum.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http;

namespace Continuum.Tests;

public class ContinuumListLoaderTests
{
    [Fact]
    public void DeserializeDefinition_ParsesJsonAndPreservesSortedOrder()
    {
        const string json = """
            {
              "name": "Sample",
              "slug": "sample",
              "enabled": true,
              "items": [
                { "order": 10, "type": "movie", "title": "Later" },
                { "order": 2, "type": "episode", "title": "Sooner" }
              ]
            }
            """;

        ContinuumListDefinition? definition = ContinuumListLoader.DeserializeDefinition(json);

        Assert.NotNull(definition);
        Assert.Equal("Sample", definition!.Name);
        Assert.Equal("sample", definition.Slug);
        Assert.Equal(2, definition.Items[0].Order);
        Assert.Equal(10, definition.Items[1].Order);
    }

    [Fact]
    public void DeserializeDefinition_ThrowsForInvalidJson()
    {
        const string json = "{ this is not valid json";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ContinuumListLoader.DeserializeDefinition(json));
    }

    [Fact]
    public async Task LoadAllFromUrlsAsync_ReadsRemoteArchiveFiles()
    {
        Uri listUrl = new Uri("https://example.invalid/sample.json");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Assert.Equal(listUrl, request.RequestUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "name": "Sample",
                      "slug": "sample",
                      "enabled": true,
                      "items": [
                        { "order": 2, "type": "movie", "title": "Second" },
                        { "order": 1, "type": "movie", "title": "First" }
                      ]
                    }
                    """)
            };
        }));

        IReadOnlyList<ContinuumListDefinition> definitions = await ContinuumListLoader.LoadAllFromUrlsAsync(
            [listUrl],
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Single(definitions);
        Assert.Equal("sample", definitions[0].Slug);
        Assert.Equal(1, definitions[0].Items[0].Order);
        Assert.Equal(2, definitions[0].Items[1].Order);
    }

    [Fact]
    public async Task LoadAllFromUrlsAsync_SkipsFailedUrlsAndKeepsValidOnes()
    {
        Uri missingUrl = new Uri("https://example.invalid/missing.json");
        Uri validUrl = new Uri("https://example.invalid/valid.json");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri == missingUrl)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "name": "Valid",
                      "slug": "valid",
                      "enabled": true,
                      "items": [
                        { "order": 1, "type": "episode", "title": "Episode" }
                      ]
                    }
                    """)
            };
        }));

        IReadOnlyList<ContinuumListDefinition> definitions = await ContinuumListLoader.LoadAllFromUrlsAsync(
            [missingUrl, validUrl],
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Single(definitions);
        Assert.Equal("valid", definitions[0].Slug);
    }

    [Fact]
    public async Task LoadAllFromUrlsAsync_ReturnsEmptyWhenNoArchiveUrlsAreConfigured()
    {
        HttpClient client = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        IReadOnlyList<ContinuumListDefinition> definitions = await ContinuumListLoader.LoadAllFromUrlsAsync(
            [],
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Empty(definitions);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = responseFactory(request);
            return Task.FromResult(response);
        }
    }
}
