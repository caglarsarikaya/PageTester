using WebCrawler.Models;

namespace WebCrawler.Services;

/// <summary>
/// Service for aggregating and filtering crawl results
/// </summary>
public class ResultAggregator
{
    /// <summary>
    /// Gets URLs with non-successful status codes (not 2xx)
    /// </summary>
    /// <param name="results">The collection of crawl results</param>
    /// <returns>A collection of URLs with non-successful status codes</returns>
    public IReadOnlyList<string> GetNonSuccessfulUrls(IEnumerable<CrawlResult> results)
    {
        return results
            .Where(r => !r.IsSuccess) // Filter out successful (2xx) responses
            .Select(r => r.Url)       // Select just the URL
            .OrderBy(url => url)      // Sort alphabetically for better readability
            .ToList();
    }

    /// <summary>
    /// Gets URLs with specific status code ranges
    /// </summary>
    /// <param name="results">The collection of crawl results</param>
    /// <param name="minStatusCode">The minimum status code (inclusive)</param>
    /// <param name="maxStatusCode">The maximum status code (exclusive)</param>
    /// <returns>A collection of results with status codes in the specified range</returns>
    public IReadOnlyList<CrawlResult> GetUrlsByStatusCodeRange(
        IEnumerable<CrawlResult> results,
        int minStatusCode,
        int maxStatusCode)
    {
        return results
            .Where(r => r.StatusCode >= minStatusCode && r.StatusCode < maxStatusCode)
            .OrderBy(r => r.StatusCode)
            .ThenBy(r => r.Url)
            .ToList();
    }

    /// <summary>
    /// Groups results by status code
    /// </summary>
    /// <param name="results">The collection of crawl results</param>
    /// <returns>A dictionary mapping status codes to collections of URLs</returns>
    public IReadOnlyDictionary<int, List<string>> GroupByStatusCode(IEnumerable<CrawlResult> results)
    {
        return results
            .GroupBy(r => r.StatusCode)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Url).ToList()
            );
    }
}