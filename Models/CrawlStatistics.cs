/// <summary>
/// Statistics about a crawl operation
/// </summary>
public class CrawlStatistics
{
    /// <summary>
    /// The number of URLs visited
    /// </summary>
    public int VisitedCount { get; set; }

    /// <summary>
    /// The number of successful requests (2xx status codes)
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// The number of redirects (3xx status codes)
    /// </summary>
    public int RedirectCount { get; set; }

    /// <summary>
    /// The number of client errors (4xx status codes)
    /// </summary>
    public int ClientErrorCount { get; set; }

    /// <summary>
    /// The number of server errors (5xx status codes)
    /// </summary>
    public int ServerErrorCount { get; set; }

    /// <summary>
    /// The number of other errors (non-HTTP errors)
    /// </summary>
    public int OtherErrorCount { get; set; }

    /// <summary>
    /// The total time spent crawling (ms)
    /// </summary>
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// The average response time (ms)
    /// </summary>
    public double AverageResponseTimeMs => VisitedCount > 0 ? (double)TotalResponseTimeMs / VisitedCount : 0;

    /// <summary>
    /// The total response time (ms) of all requests
    /// </summary>
    public long TotalResponseTimeMs { get; set; }
    
    /// <summary>
    /// The minimum response time observed (ms)
    /// </summary>
    public long MinResponseTimeMs { get; set; } = long.MaxValue;
    
    /// <summary>
    /// The maximum response time observed (ms)
    /// </summary>
    public long MaxResponseTimeMs { get; set; }
    
    /// <summary>
    /// The median response time (ms)
    /// </summary>
    public long MedianResponseTimeMs { get; set; }
    
    /// <summary>
    /// The 90th percentile response time (ms)
    /// </summary>
    public long P90ResponseTimeMs { get; set; }
    
    /// <summary>
    /// The 95th percentile response time (ms)
    /// </summary>
    public long P95ResponseTimeMs { get; set; }
    
    /// <summary>
    /// The 99th percentile response time (ms)
    /// </summary>
    public long P99ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Number of responses faster than 100ms
    /// </summary>
    public int ResponsesUnder100ms { get; set; }
    
    /// <summary>
    /// Number of responses between 100ms and 500ms
    /// </summary>
    public int ResponsesBetween100msAnd500ms { get; set; }
    
    /// <summary>
    /// Number of responses between 500ms and 1s
    /// </summary>
    public int ResponsesBetween500msAnd1s { get; set; }
    
    /// <summary>
    /// Number of responses between 1s and 3s
    /// </summary>
    public int ResponsesBetween1sAnd3s { get; set; }
    
    /// <summary>
    /// Number of responses slower than 3s
    /// </summary>
    public int ResponsesOver3s { get; set; }
    
    /// <summary>
    /// All response times collected during the crawl
    /// </summary>
    public List<long> AllResponseTimes { get; } = new List<long>();
}