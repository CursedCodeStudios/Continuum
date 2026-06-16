using Continuum.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace Continuum.Services;

/// <summary>
/// Loads Continuum manual list files from the live GitHub community archive.
/// </summary>
public sealed class ContinuumListLoader(HttpClient httpClient, ILogger<ContinuumListLoader> logger) : IContinuumListLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContinuumListDefinition>> LoadAllAsync(CancellationToken cancellationToken)
    {
        return await LoadAllFromUrlsAsync(
            ContinuumArchiveCatalog.GetListUrls(),
            httpClient,
            logger,
            cancellationToken).ConfigureAwait(false);
    }

    internal static void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Continuum/0.1");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    internal static async Task<IReadOnlyList<ContinuumListDefinition>> LoadAllFromUrlsAsync(
        IReadOnlyList<Uri> listUrls,
        HttpClient client,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (listUrls.Count == 0)
        {
            logger.LogWarning("Continuum does not have any live GitHub playlist archive URLs configured.");
            return [];
        }

        List<ContinuumListDefinition> results = new List<ContinuumListDefinition>(listUrls.Count);

        foreach (Uri listUrl in listUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, listUrl);
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };

                using HttpResponseMessage response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Skipping Continuum archive URL {Url} because GitHub returned HTTP {StatusCode}.",
                        listUrl,
                        (int)response.StatusCode);
                    continue;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                ContinuumListDefinition? definition = DeserializeDefinition(json);

                if (definition is null || string.IsNullOrWhiteSpace(definition.Name) || string.IsNullOrWhiteSpace(definition.Slug))
                {
                    logger.LogWarning("Skipping invalid Continuum list payload from {Url}.", listUrl);
                    continue;
                }

                results.Add(definition);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse Continuum list payload from {Url}.", listUrl);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Failed to fetch Continuum list payload from {Url}.", listUrl);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Timed out while fetching Continuum list payload from {Url}.", listUrl);
            }
        }

        logger.LogInformation("Loaded {Count} Continuum list definition(s) from the live GitHub archive.", results.Count);
        return results;
    }

    internal static ContinuumListDefinition? DeserializeDefinition(string json)
    {
        ContinuumListDefinition? definition = JsonSerializer.Deserialize<ContinuumListDefinition>(json, SerializerOptions);
        if (definition is null)
        {
            return null;
        }

        definition.Items = definition.Items
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return definition;
    }
}
