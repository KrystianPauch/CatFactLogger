using System.Net.Http.Json;
using System.Text.Json;
using CatFactLogger.Models;
using Microsoft.Extensions.Logging;

namespace CatFactLogger.Services;

public sealed class CatFactService : ICatFactService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatFactService> _logger;

    // Reuse JsonSerializerOptions - expensive to create each time
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatFactService(HttpClient httpClient, ILogger<CatFactService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatFact?> GetFactAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fact = await _httpClient.GetFromJsonAsync<CatFact>(
                requestUri: string.Empty,
                options: JsonOptions,
                cancellationToken: cancellationToken);

            if (fact is null)
            {
                _logger.LogWarning("API returned an empty response.");
                return null;
            }

            return fact;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Request was cancelled.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching cat fact. Status: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize API response.");
            return null;
        }
    }
}