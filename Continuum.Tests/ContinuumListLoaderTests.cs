using Continuum.Models;
using Continuum.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http;

namespace Continuum.Tests;

public class ContinuumListLoaderTests
{
    [Fact]
    public void DeserializeDefinition_ParsesJsonAndUsesArrayOrder()
    {
        const string json = """
            {
              // Array order is canonical.
              "name": "Sample",
              "slug": "sample",
              "enabled": true,
              "items": [
                { "type": "movie", "title": "Later" },
                { "type": "episode", "title": "Sooner" }
              ]
            }
            """;

        ContinuumListDefinition? definition = ContinuumListLoader.DeserializeDefinition(json);

        Assert.NotNull(definition);
        Assert.Equal("Sample", definition!.Name);
        Assert.Equal("sample", definition.Slug);
        Assert.Equal("Later", definition.Items[0].Title);
        Assert.Equal("Sooner", definition.Items[1].Title);
        Assert.Equal(1, definition.Items[0].Order);
        Assert.Equal(2, definition.Items[1].Order);
    }

    [Fact]
    public void DeserializeDefinition_ThrowsForInvalidJson()
    {
        const string json = "{ this is not valid json";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ContinuumListLoader.DeserializeDefinition(json));
    }

    [Fact]
    public void DeserializeManifest_ParsesJsonManifest()
    {
        const string json = """
            [
              "first.jsonc",
              "second.jsonc"
            ]
            """;

        string[] fileNames = ContinuumListLoader.DeserializeManifest(json);

        Assert.Equal(["first.jsonc", "second.jsonc"], fileNames);
    }

    [Fact]
    public async Task LoadManifestAsync_ReadsLiveManifestAndBuildsUrls()
    {
        Uri manifestUrl = new Uri("https://example.invalid/playlist-manifest.json");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Assert.NotNull(request.RequestUri);
            Assert.StartsWith(manifestUrl.ToString(), request.RequestUri!.ToString(), StringComparison.Ordinal);
            Assert.Contains("continuum_cache_bust=", request.RequestUri.ToString(), StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      "sample.jsonc",
                      "sample.jsonc",
                      "second.jsonc"
                    ]
                    """)
            };
        }));

        IReadOnlyList<Uri> urls = await ContinuumListLoader.LoadManifestAsync(
            manifestUrl,
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Equal("https://raw.githubusercontent.com/CursedCodeStudios/Continuum/main/Playlist-Data/sample.jsonc", urls[0].ToString());
        Assert.Equal("https://raw.githubusercontent.com/CursedCodeStudios/Continuum/main/Playlist-Data/second.jsonc", urls[1].ToString());
    }

    [Fact]
    public async Task LoadManifestAsync_SkipsInvalidManifestEntries()
    {
        Uri manifestUrl = new Uri("https://example.invalid/playlist-manifest.json");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      "valid.jsonc",
                      "nested/path.jsonc",
                      "",
                      "other\\path.jsonc"
                    ]
                    """)
            }));

        IReadOnlyList<Uri> urls = await ContinuumListLoader.LoadManifestAsync(
            manifestUrl,
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Single(urls);
        Assert.Equal("https://raw.githubusercontent.com/CursedCodeStudios/Continuum/main/Playlist-Data/valid.jsonc", urls[0].ToString());
    }

    [Fact]
    public async Task LoadAllFromUrlsAsync_ReadsRemoteArchiveFiles()
    {
        Uri listUrl = new Uri("https://example.invalid/sample.jsonc");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Assert.NotNull(request.RequestUri);
            Assert.StartsWith(listUrl.ToString(), request.RequestUri!.ToString(), StringComparison.Ordinal);
            Assert.Contains("continuum_cache_bust=", request.RequestUri.ToString(), StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "name": "Sample",
                      "slug": "sample",
                      "enabled": true,
                      "items": [
                        { "type": "movie", "title": "Second" },
                        { "type": "movie", "title": "First" }
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
        Assert.Equal("Second", definitions[0].Items[0].Title);
        Assert.Equal("First", definitions[0].Items[1].Title);
        Assert.Equal(1, definitions[0].Items[0].Order);
        Assert.Equal(2, definitions[0].Items[1].Order);
    }

    [Fact]
    public void DeserializeDefinition_IgnoresExplicitOrderValuesAndUsesArrayPosition()
    {
        const string json = """
            {
              "name": "Sample",
              "slug": "sample",
              "enabled": true,
              "items": [
                { "order": 99, "type": "movie", "title": "First In Array" },
                { "order": 1, "type": "movie", "title": "Second In Array" }
              ]
            }
            """;

        ContinuumListDefinition? definition = ContinuumListLoader.DeserializeDefinition(json);

        Assert.NotNull(definition);
        Assert.Equal("First In Array", definition!.Items[0].Title);
        Assert.Equal("Second In Array", definition.Items[1].Title);
        Assert.Equal(1, definition.Items[0].Order);
        Assert.Equal(2, definition.Items[1].Order);
    }

    [Fact]
    public async Task LoadAllFromUrlsAsync_SkipsFailedUrlsAndKeepsValidOnes()
    {
        Uri missingUrl = new Uri("https://example.invalid/missing.jsonc");
        Uri validUrl = new Uri("https://example.invalid/valid.jsonc");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Assert.NotNull(request.RequestUri);
            string requestPath = request.RequestUri!.AbsolutePath;

            if (string.Equals(requestPath, missingUrl.AbsolutePath, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            Assert.Equal(validUrl.AbsolutePath, requestPath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "name": "Valid",
                      "slug": "valid",
                      "enabled": true,
                      "items": [
                        { "type": "episode", "title": "Episode" }
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

    [Fact]
    public async Task LoadManifestAsync_ReturnsEmptyWhenManifestIsMissing()
    {
        Uri manifestUrl = new Uri("https://example.invalid/playlist-manifest.json");
        HttpClient client = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        IReadOnlyList<Uri> urls = await ContinuumListLoader.LoadManifestAsync(
            manifestUrl,
            client,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Empty(urls);
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
