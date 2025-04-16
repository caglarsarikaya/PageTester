using WebCrawler.Models;

namespace WebCrawler.Services;

/// <summary>
/// Interface for the web crawler functionality
/// </summary>
public interface IWebCrawler
{
    /// <summary>
    /// Starts the crawler from a root URL
    /// </summary>
    /// <param name="rootUrl">The starting URL to crawl</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A collection of crawl results</returns>
    Task<IReadOnlyCollection<CrawlResult>> CrawlAsync(string rootUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current statistics of the crawl
    /// </summary>
    /// <returns>Statistics about the crawl</returns>
    CrawlStatistics GetStatistics();
}

/// <summary>
/// Statistics about a crawl operation
/// </summary>
public class CrawlStatistics
{
    /// <summary>
    /// The number of URLs visited
    /// </summary>
    public int VisitedCount { get; set; }
    
    /// <summary>
    /// The number of successful requests (2xx status codes)
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// The number of redirects (3xx status codes)
    /// </summary>
    public int RedirectCount { get; set; }
    
    /// <summary>
    /// The number of client errors (4xx status codes)
    /// </summary>
    public int ClientErrorCount { get; set; }
    
    /// <summary>
    /// The number of server errors (5xx status codes)
    /// </summary>
    public int ServerErrorCount { get; set; }
    
    /// <summary>
    /// The number of other errors (non-HTTP errors)
    /// </summary>
    public int OtherErrorCount { get; set; }
    
    /// <summary>
    /// The total time spent crawling (ms)
    /// </summary>
    public long TotalTimeMs { get; set; }
    
    /// <summary>
    /// The average response time (ms)
    /// </summary>
    public double AverageResponseTimeMs => VisitedCount > 0 ? (double)TotalResponseTimeMs / VisitedCount : 0;
    
    /// <summary>
    /// The total response time (ms) of all requests
    /// </summary>
    public long TotalResponseTimeMs { get; set; }
} 