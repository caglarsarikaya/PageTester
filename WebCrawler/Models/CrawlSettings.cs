namespace WebCrawler.Models;

/// <summary>
/// Configuration settings for the web crawler
/// </summary>
public class CrawlSettings
{
    /// <summary>
    /// Maximum depth to crawl from the root URL
    /// </summary>
    public int Depth { get; set; } = 1;
} 