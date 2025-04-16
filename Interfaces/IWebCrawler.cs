using WebCrawler.Models;

namespace WebCrawler.Interfaces;

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

