using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using WebCrawler.Models;

namespace WebCrawler.Services;

/// <summary>
/// Implementation of IUrlFetcher using HttpClient
/// </summary>
public class HttpUrlFetcher : IUrlFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpUrlFetcher> _logger;
    
    public HttpUrlFetcher(ILogger<HttpUrlFetcher> logger)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Set up a user agent to avoid being blocked by servers
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 WebCrawler/1.0");
        
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<CrawlResult> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CrawlResult { Url = url };
        
        try
        {
            _logger.LogInformation("Fetching URL: {Url}", url);
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            stopwatch.Stop();
            
            result.StatusCode = (int)response.StatusCode;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogInformation("Fetched {Url} - Status: {StatusCode}, Time: {ResponseTime}ms", 
                url, result.StatusCode, result.ResponseTimeMs);
                
            return result;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            result.StatusCode = 0;
            result.ErrorMessage = "Request timed out";
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning("Timeout fetching {Url} after {ResponseTime}ms: {Message}", 
                url, result.ResponseTimeMs, result.ErrorMessage);
                
            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.StatusCode = GetStatusCodeFromException(ex);
            result.ErrorMessage = ex.Message;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning("Error fetching {Url} - Status: {StatusCode}, Time: {ResponseTime}ms, Error: {Message}", 
                url, result.StatusCode, result.ResponseTimeMs, result.ErrorMessage);
                
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.StatusCode = 0;
            result.ErrorMessage = ex.Message;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Unexpected error fetching {Url} after {ResponseTime}ms", 
                url, result.ResponseTimeMs);
                
            return result;
        }
    }
    
    /// <inheritdoc />
    public async Task<(CrawlResult Result, string? Content)> FetchWithContentAsync(string url, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CrawlResult { Url = url };
        string? content = null;
        
        try
        {
            _logger.LogInformation("Fetching URL with content: {Url}", url);
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
            
            result.StatusCode = (int)response.StatusCode;
            
            // Only read content if status code is success
            if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogInformation("Fetched {Url} with content - Status: {StatusCode}, Size: {ContentSize} bytes, Time: {ResponseTime}ms", 
                url, result.StatusCode, content?.Length ?? 0, result.ResponseTimeMs);
                
            return (result, content);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            result.StatusCode = 0;
            result.ErrorMessage = "Request timed out";
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning("Timeout fetching {Url} with content after {ResponseTime}ms: {Message}", 
                url, result.ResponseTimeMs, result.ErrorMessage);
                
            return (result, null);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.StatusCode = GetStatusCodeFromException(ex);
            result.ErrorMessage = ex.Message;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning("Error fetching {Url} with content - Status: {StatusCode}, Time: {ResponseTime}ms, Error: {Message}", 
                url, result.StatusCode, result.ResponseTimeMs, result.ErrorMessage);
                
            return (result, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.StatusCode = 0;
            result.ErrorMessage = ex.Message;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Unexpected error fetching {Url} with content after {ResponseTime}ms", 
                url, result.ResponseTimeMs);
                
            return (result, null);
        }
    }
    
    /// <summary>
    /// Tries to extract a status code from an HttpRequestException
    /// </summary>
    private static int GetStatusCodeFromException(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue)
        {
            return (int)ex.StatusCode.Value;
        }
        
        return 0; // Unknown status code
    }
} 