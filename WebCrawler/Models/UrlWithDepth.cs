namespace WebCrawler.Models;

/// <summary>
/// Represents a URL with its crawl depth from the original URL
/// </summary>
public class UrlWithDepth
{
    /// <summary>
    /// The URL to crawl
    /// </summary>
    public string Url { get; }
    
    /// <summary>
    /// The depth of this URL from the root URL (0 = root URL)
    /// </summary>
    public int Depth { get; }
    
    public UrlWithDepth(string url, int depth)
    {
        Url = url;
        Depth = depth;
    }
} 