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
        IReadOnlyList<Uri> listUrls = await LoadManifestAsync(
            ContinuumArchiveCatalog.GetManifestUrl(),
            httpClient,
            logger,
            cancellationToken).ConfigureAwait(false);

        return await LoadAllFromUrlsAsync(
            listUrls,
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
                using HttpResponseMessage response = await SendArchiveRequestAsync(
                    listUrl,
                    client,
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

    internal static async Task<IReadOnlyList<Uri>> LoadManifestAsync(
        Uri manifestUrl,
        HttpClient client,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await SendArchiveRequestAsync(
                manifestUrl,
                client,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Continuum playlist manifest at {Url} returned HTTP {StatusCode}.",
                    manifestUrl,
                    (int)response.StatusCode);
                return [];
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string[] fileNames = DeserializeManifest(json);

            List<Uri> urls = [];
            foreach (string fileName in fileNames
                         .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (fileName.Contains("/", StringComparison.Ordinal) || fileName.Contains("\\", StringComparison.Ordinal))
                {
                    logger.LogWarning(
                        "Skipping invalid Continuum manifest entry {Entry} because nested paths are not supported.",
                        fileName);
                    continue;
                }

                urls.Add(ContinuumArchiveCatalog.GetListUrl(fileName));
            }

            if (urls.Count == 0)
            {
                logger.LogWarning("Continuum playlist manifest at {Url} did not contain any valid playlist file names.", manifestUrl);
                return [];
            }

            logger.LogInformation("Loaded {Count} Continuum playlist file reference(s) from the live manifest.", urls.Count);
            return urls;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Continuum playlist manifest from {Url}.", manifestUrl);
            return [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch Continuum playlist manifest from {Url}.", manifestUrl);
            return [];
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Timed out while fetching Continuum playlist manifest from {Url}.", manifestUrl);
            return [];
        }
    }

    internal static ContinuumListDefinition? DeserializeDefinition(string json)
    {
        ContinuumListDefinition? definition = JsonSerializer.Deserialize<ContinuumListDefinition>(json, SerializerOptions);
        if (definition is null)
        {
            return null;
        }

        for (int index = 0; index < definition.Items.Count; index++)
        {
            definition.Items[index].Order = index + 1;
        }

        return definition;
    }

    internal static string[] DeserializeManifest(string json)
    {
        string[]? fileNames = JsonSerializer.Deserialize<string[]>(json, SerializerOptions);
        return fileNames ?? [];
    }

    private static async Task<HttpResponseMessage> SendArchiveRequestAsync(
        Uri url,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        Uri requestUri = CreateCacheBustedUri(url);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };
        request.Headers.Pragma.ParseAdd("no-cache");

        return await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
    }

    private static Uri CreateCacheBustedUri(Uri url)
    {
        string separator = string.IsNullOrEmpty(url.Query) ? "?" : "&";
        string cacheBust = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new Uri($"{url}{separator}continuum_cache_bust={cacheBust}", UriKind.Absolute);
    }
}
