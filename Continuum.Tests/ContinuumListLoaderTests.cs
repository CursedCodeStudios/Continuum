using Continuum.Models;
using Continuum.Services;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void DeserializeDefinition_ReturnsNullForInvalidJson()
    {
        const string json = "{ this is not valid json";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ContinuumListLoader.DeserializeDefinition(json));
    }

    [Fact]
    public async Task LoadAllFromDirectoryAsync_ReadsBundledArchiveFiles()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"continuum-loader-{Guid.NewGuid():N}");
        string archiveDirectory = Path.Combine(tempDirectory, "Playlist-Data");
        Directory.CreateDirectory(archiveDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(archiveDirectory, "sample.json"),
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
                """);

            IReadOnlyList<ContinuumListDefinition> definitions = await ContinuumListLoader.LoadAllFromDirectoryAsync(
                archiveDirectory,
                NullLogger.Instance,
                CancellationToken.None);

            Assert.Single(definitions);
            Assert.Equal("sample", definitions[0].Slug);
            Assert.Equal(1, definitions[0].Items[0].Order);
            Assert.Equal(2, definitions[0].Items[1].Order);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task LoadAllFromDirectoryAsync_ReturnsEmptyWhenArchiveIsMissing()
    {
        string archiveDirectory = Path.Combine(Path.GetTempPath(), $"continuum-missing-{Guid.NewGuid():N}");

        IReadOnlyList<ContinuumListDefinition> definitions = await ContinuumListLoader.LoadAllFromDirectoryAsync(
            archiveDirectory,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Empty(definitions);
    }
}
