namespace WebCrawler.Models;

/// <summary>
/// Represents the result of crawling a URL
/// </summary>
public class CrawlResult
{
    /// <summary>
    /// The URL that was crawled
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP status code received from the server
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Error message if an exception occurred during crawling
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Time taken to fetch the URL in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Whether the crawl was successful (status code 200-299)
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
} 