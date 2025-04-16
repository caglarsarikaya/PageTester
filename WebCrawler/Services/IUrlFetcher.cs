using WebCrawler.Models;

namespace WebCrawler.Services;

/// <summary>
/// Interface for URL fetching functionality
/// </summary>
public interface IUrlFetcher
{
    /// <summary>
    /// Fetches the content from a URL
    /// </summary>
    /// <param name="url">The URL to fetch</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>CrawlResult containing status code and response time</returns>
    Task<CrawlResult> FetchAsync(string url, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches the content from a URL and returns the HTML content
    /// </summary>
    /// <param name="url">The URL to fetch</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>CrawlResult containing status code, response time, and HTML content</returns>
    Task<(CrawlResult Result, string? Content)> FetchWithContentAsync(string url, CancellationToken cancellationToken = default);
} 