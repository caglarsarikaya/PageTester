using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Web;
using WebCrawler.Interfaces;

namespace WebCrawler.Services;

/// <summary>
/// Implementation of IHtmlParser using HtmlAgilityPack
/// </summary>
public class HtmlAgilityPackParser : IHtmlParser
{
    private readonly ILogger<HtmlAgilityPackParser> _logger;

    public HtmlAgilityPackParser(ILogger<HtmlAgilityPackParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> ExtractLinks(string html, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            _logger.LogWarning("Empty HTML content provided for parsing");
            return Enumerable.Empty<string>();
        }

        var links = new List<string>();
        var htmlDoc = new HtmlDocument();

        try
        {
            htmlDoc.LoadHtml(html);

            // Get all anchor tags
            var anchorNodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]");

            if (anchorNodes == null)
            {
                _logger.LogDebug("No links found in the HTML content");
                return Enumerable.Empty<string>();
            }

            // Process each anchor tag to extract and normalize URLs
            foreach (var anchorNode in anchorNodes)
            {
                var href = anchorNode.GetAttributeValue("href", string.Empty);

                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                // Decode HTML entities if present
                href = HttpUtility.HtmlDecode(href);

                // Skip fragment-only URLs, javascript:, mailto:, tel:, etc.
                if (href.StartsWith("#") ||
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    // Normalize the URL (convert relative to absolute)
                    var absoluteUrl = NormalizeUrl(href, baseUrl);

                    if (!string.IsNullOrEmpty(absoluteUrl))
                    {
                        // Only add HTTP/HTTPS URLs
                        if (absoluteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            absoluteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            links.Add(absoluteUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing URL '{Href}' from base URL '{BaseUrl}'", href, baseUrl);
                }
            }

            _logger.LogDebug("Extracted {LinkCount} links from HTML content", links.Count);
            return links;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML content");
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public bool IsRelatedToRootUrl(string url, string rootUrl)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(rootUrl))
        {
            return false;
        }

        try
        {
            var urlUri = new Uri(url);
            var rootUri = new Uri(rootUrl);

            // Check if both are on the same host
            var urlHost = urlUri.Host;
            var rootHost = rootUri.Host;

            // Same exact host
            if (string.Equals(urlHost, rootHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if URL is a subdomain of root
            if (urlHost.EndsWith("." + rootHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if root is a subdomain of URL
            if (rootHost.EndsWith("." + urlHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if URL '{Url}' is related to root URL '{RootUrl}'", url, rootUrl);
            return false;
        }
    }

    /// <summary>
    /// Normalizes a URL by converting relative URLs to absolute
    /// </summary>
    /// <param name="url">The URL to normalize</param>
    /// <param name="baseUrl">The base URL to use for relative URLs</param>
    /// <returns>The normalized absolute URL</returns>
    private string NormalizeUrl(string url, string baseUrl)
    {
        try
        {
            // Try to create a URI directly (if it's already absolute)
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            // It's a relative URL, combine with base URL
            if (Uri.TryCreate(new Uri(baseUrl), url, out var combinedUri))
            {
                return combinedUri.ToString();
            }

            // If we got here, we couldn't normalize the URL
            _logger.LogWarning("Could not normalize URL '{Url}' with base '{BaseUrl}'", url, baseUrl);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizing URL '{Url}' with base '{BaseUrl}'", url, baseUrl);
            return string.Empty;
        }
    }
}