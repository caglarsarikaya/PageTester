using System.Collections.Concurrent;
using WebCrawler.Models;

namespace WebCrawler.Utilities;

/// <summary>
/// Thread-safe collection to track visited URLs
/// </summary>
public class VisitedUrls
{
    private readonly ConcurrentDictionary<string, bool> _urls = new();

    /// <summary>
    /// Tries to add a URL to the visited collection
    /// </summary>
    /// <param name="url">The URL to add</param>
    /// <returns>True if the URL was added, false if it was already in the collection</returns>
    public bool TryAdd(string url)
    {
        return _urls.TryAdd(url, true);
    }

    /// <summary>
    /// Checks if a URL has been visited
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>True if the URL has been visited</returns>
    public bool Contains(string url)
    {
        return _urls.ContainsKey(url);
    }

    /// <summary>
    /// Gets the count of visited URLs
    /// </summary>
    public int Count => _urls.Count;
}

/// <summary>
/// Manages the queue of URLs to crawl
/// </summary>
public class CrawlQueue
{
    private readonly Queue<UrlWithDepth> _queue = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Adds a URL to the crawl queue
    /// </summary>
    /// <param name="url">The URL to crawl</param>
    /// <param name="depth">The depth of the URL from the root</param>
    public void Enqueue(string url, int depth)
    {
        lock (_lockObject)
        {
            _queue.Enqueue(new UrlWithDepth(url, depth));
        }
    }

    /// <summary>
    /// Adds a URL with depth to the crawl queue
    /// </summary>
    /// <param name="urlWithDepth">The URL with depth to crawl</param>
    public void Enqueue(UrlWithDepth urlWithDepth)
    {
        lock (_lockObject)
        {
            _queue.Enqueue(urlWithDepth);
        }
    }

    /// <summary>
    /// Gets the next URL to crawl, or null if the queue is empty
    /// </summary>
    /// <returns>The next URL with depth to crawl, or null if the queue is empty</returns>
    public UrlWithDepth? Dequeue()
    {
        lock (_lockObject)
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }
    }

    /// <summary>
    /// Gets the number of URLs in the queue
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _queue.Count;
            }
        }
    }

    /// <summary>
    /// Checks if the queue is empty
    /// </summary>
    public bool IsEmpty => Count == 0;
}