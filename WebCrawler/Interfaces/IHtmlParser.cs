namespace WebCrawler.Interfaces;

/// <summary>
/// Interface for HTML parsing functionality
/// </summary>
public interface IHtmlParser
{
    /// <summary>
    /// Extracts all links from HTML content
    /// </summary>
    /// <param name="html">The HTML content to parse</param>
    /// <param name="baseUrl">The base URL used to resolve relative URLs</param>
    /// <returns>A collection of absolute URLs extracted from the HTML</returns>
    IEnumerable<string> ExtractLinks(string html, string baseUrl);

    /// <summary>
    /// Determines if a URL is related to the root URL (same domain or subdomain)
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="rootUrl">The root URL of the crawl</param>
    /// <returns>True if the URL is related to the root URL</returns>
    bool IsRelatedToRootUrl(string url, string rootUrl);
}