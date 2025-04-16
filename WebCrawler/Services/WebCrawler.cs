using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WebCrawler.Models;
using WebCrawler.Utilities;

namespace WebCrawler.Services;

/// <summary>
/// Implementation of the web crawler
/// </summary>
public class WebCrawler : IWebCrawler
{
    private readonly IUrlFetcher _urlFetcher;
    private readonly IHtmlParser _htmlParser;
    private readonly ILogger<WebCrawler> _logger;
    private readonly CrawlSettings _settings;
    private readonly VisitedUrls _visitedUrls;
    private readonly CrawlQueue _crawlQueue;
    private readonly ConcurrentBag<CrawlResult> _results = new();
    private readonly CrawlStatistics _statistics = new();
    private readonly Stopwatch _crawlStopwatch = new();
    
    public WebCrawler(
        IUrlFetcher urlFetcher,
        IHtmlParser htmlParser,
        ILogger<WebCrawler> logger,
        CrawlSettings settings,
        VisitedUrls visitedUrls,
        CrawlQueue crawlQueue)
    {
        _urlFetcher = urlFetcher;
        _htmlParser = htmlParser;
        _logger = logger;
        _settings = settings;
        _visitedUrls = visitedUrls;
        _crawlQueue = crawlQueue;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CrawlResult>> CrawlAsync(string rootUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting crawl from root URL: {RootUrl} with max depth: {MaxDepth}", rootUrl, _settings.Depth);
        
        // Clear previous results if any
        _results.Clear();
        ResetStatistics();
        
        // Add the root URL to the queue
        _crawlQueue.Enqueue(rootUrl, 0);
        
        _crawlStopwatch.Start();
        
        try
        {
            // Process the queue until it's empty
            while (!_crawlQueue.IsEmpty && !cancellationToken.IsCancellationRequested)
            {
                // Get next URL from queue
                var urlWithDepth = _crawlQueue.Dequeue();
                
                if (urlWithDepth == null)
                {
                    continue;
                }
                
                // Skip if already visited
                if (_visitedUrls.Contains(urlWithDepth.Url))
                {
                    _logger.LogDebug("Skipping already visited URL: {Url}", urlWithDepth.Url);
                    continue;
                }
                
                // Mark as visited
                _visitedUrls.TryAdd(urlWithDepth.Url);
                
                // Fetch the URL
                var result = await ProcessUrlAsync(urlWithDepth, cancellationToken);
                
                // Add to results
                _results.Add(result);
                
                _logger.LogInformation("Processed URL {CurrentCount}: {Url}, Status: {StatusCode}, Depth: {Depth}",
                    _results.Count, result.Url, result.StatusCode, urlWithDepth.Depth);
                
                // Update statistics
                UpdateStatistics(result);
                
                // Log progress periodically
                if (_results.Count % 10 == 0)
                {
                    LogProgress();
                }
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Crawl was cancelled after processing {Count} URLs", _results.Count);
            }
            else
            {
                _logger.LogInformation("Crawl completed, processed {Count} URLs", _results.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during crawl");
        }
        finally
        {
            _crawlStopwatch.Stop();
            _statistics.TotalTimeMs = _crawlStopwatch.ElapsedMilliseconds;
            
            LogProgress();
        }
        
        return _results.ToArray();
    }
    
    /// <inheritdoc />
    public CrawlStatistics GetStatistics()
    {
        _statistics.TotalTimeMs = _crawlStopwatch.IsRunning ? _crawlStopwatch.ElapsedMilliseconds : _statistics.TotalTimeMs;
        return _statistics;
    }
    
    /// <summary>
    /// Processes a URL: fetches it and, if successful and within depth limit, extracts and enqueues links
    /// </summary>
    private async Task<CrawlResult> ProcessUrlAsync(UrlWithDepth urlWithDepth, CancellationToken cancellationToken)
    {
        CrawlResult result;
        
        try
        {
            // If we're at max depth, just fetch the headers to check status
            if (urlWithDepth.Depth >= _settings.Depth)
            {
                result = await _urlFetcher.FetchAsync(urlWithDepth.Url, cancellationToken);
                return result;
            }
            
            // Otherwise fetch the content too
            var (fetchResult, content) = await _urlFetcher.FetchWithContentAsync(urlWithDepth.Url, cancellationToken);
            result = fetchResult;
            
            // If successful and we have content, extract links
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(content))
            {
                await EnqueueLinksAsync(content, urlWithDepth, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing URL: {Url}", urlWithDepth.Url);
            result = new CrawlResult
            {
                Url = urlWithDepth.Url,
                StatusCode = 0,
                ErrorMessage = ex.Message
            };
        }
        
        return result;
    }
    
    /// <summary>
    /// Extracts links from content and enqueues them if they're related to the root URL
    /// </summary>
    private Task EnqueueLinksAsync(string content, UrlWithDepth parentUrlWithDepth, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content) || parentUrlWithDepth.Depth >= _settings.Depth)
        {
            return Task.CompletedTask;
        }
        
        try
        {
            var links = _htmlParser.ExtractLinks(content, parentUrlWithDepth.Url);
            
            foreach (var link in links)
            {
                // Skip if already visited or queued
                if (_visitedUrls.Contains(link))
                {
                    continue;
                }
                
                // Check if the link is related to the root URL
                if (!_htmlParser.IsRelatedToRootUrl(link, parentUrlWithDepth.Url))
                {
                    _logger.LogDebug("Skipping external URL: {Url}", link);
                    continue;
                }
                
                // Enqueue the link with incremented depth
                _crawlQueue.Enqueue(link, parentUrlWithDepth.Depth + 1);
                _logger.LogDebug("Enqueued URL: {Url} at depth {Depth}", link, parentUrlWithDepth.Depth + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting links from URL: {Url}", parentUrlWithDepth.Url);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Updates statistics based on a crawl result
    /// </summary>
    private void UpdateStatistics(CrawlResult result)
    {
        _statistics.VisitedCount++;
        _statistics.TotalResponseTimeMs += result.ResponseTimeMs;
        
        if (result.StatusCode >= 200 && result.StatusCode < 300)
        {
            _statistics.SuccessCount++;
        }
        else if (result.StatusCode >= 300 && result.StatusCode < 400)
        {
            _statistics.RedirectCount++;
        }
        else if (result.StatusCode >= 400 && result.StatusCode < 500)
        {
            _statistics.ClientErrorCount++;
        }
        else if (result.StatusCode >= 500 && result.StatusCode < 600)
        {
            _statistics.ServerErrorCount++;
        }
        else
        {
            _statistics.OtherErrorCount++;
        }
    }
    
    /// <summary>
    /// Resets the statistics to their default values
    /// </summary>
    private void ResetStatistics()
    {
        _statistics.VisitedCount = 0;
        _statistics.SuccessCount = 0;
        _statistics.RedirectCount = 0;
        _statistics.ClientErrorCount = 0;
        _statistics.ServerErrorCount = 0;
        _statistics.OtherErrorCount = 0;
        _statistics.TotalTimeMs = 0;
        _statistics.TotalResponseTimeMs = 0;
    }
    
    /// <summary>
    /// Logs the current progress of the crawl
    /// </summary>
    private void LogProgress()
    {
        var currentStats = GetStatistics();
        
        _logger.LogInformation(
            "Crawl progress: Visited={VisitedCount}, Success={SuccessCount}, Redirect={RedirectCount}, " +
            "ClientError={ClientErrorCount}, ServerError={ServerErrorCount}, OtherError={OtherErrorCount}, " +
            "AvgResponseTime={AvgResponseTime:F2}ms, TotalTime={TotalTime:F2}s",
            currentStats.VisitedCount,
            currentStats.SuccessCount,
            currentStats.RedirectCount,
            currentStats.ClientErrorCount,
            currentStats.ServerErrorCount,
            currentStats.OtherErrorCount,
            currentStats.AverageResponseTimeMs,
            currentStats.TotalTimeMs / 1000.0);
    }
} 