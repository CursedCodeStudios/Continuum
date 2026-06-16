using Continuum.Models;
using Continuum.Services;

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
}
