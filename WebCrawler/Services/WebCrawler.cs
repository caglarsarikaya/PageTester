using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using WebCrawler.Interfaces;
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
            _logger.LogInformation("Queue status: {QueueCount} URLs queued, {VisitedCount} URLs processed", 
                _crawlQueue.Count, _visitedUrls.Count);

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

                // Log URL details at DEBUG level instead of INFO
                _logger.LogDebug("Processed URL {CurrentCount}: {Url}, Status: {StatusCode}, Depth: {Depth}, ResponseTime: {ResponseTime}ms",
                    _results.Count, result.Url, result.StatusCode, urlWithDepth.Depth, result.ResponseTimeMs);

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

            // Calculate percentile statistics at the end of the crawl
            CalculatePercentileStatistics();
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
            int linksAddedCount = 0;
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
                linksAddedCount++;
                _logger.LogDebug("Enqueued URL: {Url} at depth {Depth}", link, parentUrlWithDepth.Depth + 1);
            }

            if (linksAddedCount > 0)
            {
                _logger.LogDebug("Added {Count} new URLs to the queue from {ParentUrl}", 
                    linksAddedCount, parentUrlWithDepth.Url);
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

        // Update min/max response times
        if (result.ResponseTimeMs < _statistics.MinResponseTimeMs)
        {
            _statistics.MinResponseTimeMs = result.ResponseTimeMs;
        }

        if (result.ResponseTimeMs > _statistics.MaxResponseTimeMs)
        {
            _statistics.MaxResponseTimeMs = result.ResponseTimeMs;
        }

        // Add to response time list for percentile calculations
        _statistics.AllResponseTimes.Add(result.ResponseTimeMs);

        // Update response time distribution
        if (result.ResponseTimeMs < 100)
        {
            _statistics.ResponsesUnder100ms++;
        }
        else if (result.ResponseTimeMs < 500)
        {
            _statistics.ResponsesBetween100msAnd500ms++;
        }
        else if (result.ResponseTimeMs < 1000)
        {
            _statistics.ResponsesBetween500msAnd1s++;
        }
        else if (result.ResponseTimeMs < 3000)
        {
            _statistics.ResponsesBetween1sAnd3s++;
        }
        else
        {
            _statistics.ResponsesOver3s++;
        }

        // Update status code counts
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
    /// Calculates percentile statistics from the collected response times
    /// </summary>
    private void CalculatePercentileStatistics()
    {
        if (_statistics.AllResponseTimes.Count == 0)
        {
            return;
        }

        // Sort the response times for percentile calculations
        var sortedResponseTimes = _statistics.AllResponseTimes.OrderBy(t => t).ToList();

        // Calculate median (50th percentile)
        _statistics.MedianResponseTimeMs = CalculatePercentile(sortedResponseTimes, 50);

        // Calculate 90th percentile
        _statistics.P90ResponseTimeMs = CalculatePercentile(sortedResponseTimes, 90);

        // Calculate 95th percentile
        _statistics.P95ResponseTimeMs = CalculatePercentile(sortedResponseTimes, 95);

        // Calculate 99th percentile
        _statistics.P99ResponseTimeMs = CalculatePercentile(sortedResponseTimes, 99);
    }

    /// <summary>
    /// Calculates a percentile value from a sorted list
    /// </summary>
    /// <param name="sortedValues">A sorted list of values</param>
    /// <param name="percentile">The percentile to calculate (0-100)</param>
    /// <returns>The percentile value</returns>
    private long CalculatePercentile(List<long> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var rank = (percentile / 100.0) * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var weight = rank - lowerIndex;
        return (long)(sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight);
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
        _statistics.MinResponseTimeMs = long.MaxValue;
        _statistics.MaxResponseTimeMs = 0;
        _statistics.MedianResponseTimeMs = 0;
        _statistics.P90ResponseTimeMs = 0;
        _statistics.P95ResponseTimeMs = 0;
        _statistics.P99ResponseTimeMs = 0;
        _statistics.ResponsesUnder100ms = 0;
        _statistics.ResponsesBetween100msAnd500ms = 0;
        _statistics.ResponsesBetween500msAnd1s = 0;
        _statistics.ResponsesBetween1sAnd3s = 0;
        _statistics.ResponsesOver3s = 0;
        _statistics.AllResponseTimes.Clear();
    }

    /// <summary>
    /// Logs the current progress of the crawl
    /// </summary>
    private void LogProgress()
    {
        var currentStats = GetStatistics();
        var elapsedSeconds = currentStats.TotalTimeMs / 1000.0;
        var urlsPerSecond = elapsedSeconds > 0 ? currentStats.VisitedCount / elapsedSeconds : 0;
        
        // Calculate estimated time to completion
        var remainingUrls = _crawlQueue.Count;
        var estimatedSecondsRemaining = urlsPerSecond > 0 ? remainingUrls / urlsPerSecond : 0;
        var estimatedTimeMessage = urlsPerSecond > 0 
            ? $", Est. completion in: {FormatTimeSpan(TimeSpan.FromSeconds(estimatedSecondsRemaining))}"
            : "";
        
        _logger.LogInformation(
            "Crawl progress: Processed={VisitedCount} URLs, Queue={QueueCount} URLs, " +
            "Rate={Rate:F2} URLs/sec, Success={SuccessCount}, Non-Success={NonSuccessCount}, " +
            "AvgResponseTime={AvgResponseTime:F2}ms, TotalTime={TotalTime:F2}s{EstTimeRemaining}",
            currentStats.VisitedCount,
            remainingUrls,
            urlsPerSecond,
            currentStats.SuccessCount,
            currentStats.VisitedCount - currentStats.SuccessCount,
            currentStats.AverageResponseTimeMs,
            elapsedSeconds,
            estimatedTimeMessage);
    }
    
    /// <summary>
    /// Formats a TimeSpan into a readable string
    /// </summary>
    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.TotalHours:F1} hours";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.TotalMinutes:F1} minutes";
        }
        else
        {
            return $"{timeSpan.TotalSeconds:F0} seconds";
        }
    }
}