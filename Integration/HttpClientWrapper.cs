#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;

namespace EfMigrationDiff.Integration;

/// <summary>
/// Wrapper around HttpClient providing convenient methods for common HTTP operations.
/// Handles serialization, error responses, retries, and timeout management.
/// </summary>
public class HttpClientWrapper : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public HttpClientWrapper(
        string? baseUrl = null,
        TimeSpan? timeout = null,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        _httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (timeout.HasValue)
        {
            _httpClient.Timeout = timeout.Value;
        }

        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Sends a GET request and deserializes the response to the specified type.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await SendWithRetryAsync(() => _httpClient.GetAsync(url));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {url} returned {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    /// <summary>
    /// Sends a GET request and returns the response as a string.
    /// </summary>
    public async Task<string> GetStringAsync(string url)
    {
        var response = await SendWithRetryAsync(() => _httpClient.GetAsync(url));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {url} returned {response.StatusCode}");

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Sends a POST request with JSON body.
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, object? data)
    {
        var content = data is not null
            ? new StringContent(
                JsonSerializer.Serialize(data, _jsonOptions),
                Encoding.UTF8,
                "application/json")
            : null;

        var response = await SendWithRetryAsync(() => _httpClient.PostAsync(url, content));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"POST {url} returned {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(responseContent)
            ? default
            : JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Sends a POST request and returns raw string response.
    /// </summary>
    public async Task<string> PostStringAsync(string url, string data, string contentType = "application/json")
    {
        var content = new StringContent(data, Encoding.UTF8, contentType);
        var response = await SendWithRetryAsync(() => _httpClient.PostAsync(url, content));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"POST {url} returned {response.StatusCode}");

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Sends a PUT request with JSON body.
    /// </summary>
    public async Task<T?> PutAsync<T>(string url, object? data)
    {
        var content = data is not null
            ? new StringContent(
                JsonSerializer.Serialize(data, _jsonOptions),
                Encoding.UTF8,
                "application/json")
            : null;

        var response = await SendWithRetryAsync(() => _httpClient.PutAsync(url, content));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"PUT {url} returned {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(responseContent)
            ? default
            : JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task DeleteAsync(string url)
    {
        var response = await SendWithRetryAsync(() => _httpClient.DeleteAsync(url));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"DELETE {url} returned {response.StatusCode}");
    }

    /// <summary>
    /// Sends an HTTP request with automatic retry logic on failure.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> requestFunc)
    {
        int attempts = 0;
        while (attempts < _maxRetries)
        {
            try
            {
                var response = await requestFunc();

                // Retry on transient failures (5xx errors or timeout)
                if (!response.IsSuccessStatusCode &&
                    ((int)response.StatusCode >= 500 || (int)response.StatusCode == 408))
                {
                    attempts++;
                    if (attempts < _maxRetries)
                    {
                        await Task.Delay(_retryDelay);
                        continue;
                    }
                }

                return response;
            }
            catch (HttpRequestException) when (attempts < _maxRetries - 1)
            {
                attempts++;
                await Task.Delay(_retryDelay);
            }
        }

        return await requestFunc();
    }

    /// <summary>
    /// Adds a default header to all requests.
    /// </summary>
    public void AddDefaultHeader(string name, string value)
    {
        _httpClient.DefaultRequestHeaders.Add(name, value);
    }

    /// <summary>
    /// Sets the authorization header.
    /// </summary>
    public void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
