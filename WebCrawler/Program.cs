using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WebCrawler.Interfaces;
using WebCrawler.Models;
using WebCrawler.Services;
using WebCrawler.Utilities;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Create service collection
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});

// Add configuration
services.AddSingleton<IConfiguration>(configuration);

// Bind CrawlSettings from configuration
var crawlSettings = new CrawlSettings();
configuration.GetSection("CrawlSettings").Bind(crawlSettings);
services.AddSingleton(crawlSettings);

// Register core utilities
services.AddSingleton<VisitedUrls>();
services.AddSingleton<CrawlQueue>();

// Register services
services.AddSingleton<IUrlFetcher, HttpUrlFetcher>();
services.AddSingleton<IHtmlParser, HtmlAgilityPackParser>();
services.AddSingleton<IWebCrawler, WebCrawler.Services.WebCrawler>();
services.AddSingleton<ResultAggregator>();
services.AddSingleton<ResultExporter>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
logger.LogDebug($"Configured crawl depth: {crawlSettings.Depth}");

// Get the web crawler and result services
var webCrawler = serviceProvider.GetRequiredService<IWebCrawler>();
var resultAggregator = serviceProvider.GetRequiredService<ResultAggregator>();
var resultExporter = serviceProvider.GetRequiredService<ResultExporter>();

// Prompt for URL
Console.Write("Please enter the URL to crawl:");
var rootUrl = Console.ReadLine();

if (string.IsNullOrWhiteSpace(rootUrl))
{
    logger.LogError("No URL provided. Exiting.");
    return;
}

// Make sure URL has a scheme
if (!rootUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
    !rootUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    rootUrl = "https://" + rootUrl;
    logger.LogDebug("Added https:// prefix to URL: {Url}", rootUrl);
}

// Create cancellation token source
using var cts = new CancellationTokenSource();

// Handle cancellation
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; // Prevent the process from terminating
    logger.LogWarning("Cancellation requested. Stopping crawler...");
    cts.Cancel();
};

logger.LogInformation("Starting crawl of {Url}. Press Ctrl+C to cancel.", rootUrl);
Console.WriteLine();

// Start a stopwatch to measure total execution time
var stopwatch = Stopwatch.StartNew();

try
{
    // Start crawling
    var results = await webCrawler.CrawlAsync(rootUrl, cts.Token);

    // Stop the stopwatch
    stopwatch.Stop();

    // Get statistics
    var stats = webCrawler.GetStatistics();

    // Display results
    Console.WriteLine();
    Console.WriteLine("Crawl completed!");
    Console.WriteLine($"Total URLs processed: {stats.VisitedCount}");
    Console.WriteLine($"Successful (2xx): {stats.SuccessCount}");
    Console.WriteLine($"Redirects (3xx): {stats.RedirectCount}");
    Console.WriteLine($"Client errors (4xx): {stats.ClientErrorCount}");
    Console.WriteLine($"Server errors (5xx): {stats.ServerErrorCount}");
    Console.WriteLine($"Other errors: {stats.OtherErrorCount}");
    
    // Display detailed response time statistics
    Console.WriteLine();
    Console.WriteLine("Response Time Statistics:");
    Console.WriteLine($"  Minimum: {stats.MinResponseTimeMs} ms");
    Console.WriteLine($"  Maximum: {stats.MaxResponseTimeMs} ms");
    Console.WriteLine($"  Average: {stats.AverageResponseTimeMs:F2} ms");
    Console.WriteLine($"  Median (P50): {stats.MedianResponseTimeMs} ms");
    Console.WriteLine($"  90th Percentile (P90): {stats.P90ResponseTimeMs} ms");
    Console.WriteLine($"  95th Percentile (P95): {stats.P95ResponseTimeMs} ms");
    Console.WriteLine($"  99th Percentile (P99): {stats.P99ResponseTimeMs} ms");
    
    Console.WriteLine();
    Console.WriteLine("Response Time Distribution:");
    Console.WriteLine($"  Under 100ms: {stats.ResponsesUnder100ms} requests ({CalculatePercentage(stats.ResponsesUnder100ms, stats.VisitedCount):F1}%)");
    Console.WriteLine($"  100ms-500ms: {stats.ResponsesBetween100msAnd500ms} requests ({CalculatePercentage(stats.ResponsesBetween100msAnd500ms, stats.VisitedCount):F1}%)");
    Console.WriteLine($"  500ms-1s: {stats.ResponsesBetween500msAnd1s} requests ({CalculatePercentage(stats.ResponsesBetween500msAnd1s, stats.VisitedCount):F1}%)");
    Console.WriteLine($"  1s-3s: {stats.ResponsesBetween1sAnd3s} requests ({CalculatePercentage(stats.ResponsesBetween1sAnd3s, stats.VisitedCount):F1}%)");
    Console.WriteLine($"  Over 3s: {stats.ResponsesOver3s} requests ({CalculatePercentage(stats.ResponsesOver3s, stats.VisitedCount):F1}%)");
    
    Console.WriteLine($"Total crawl time: {stopwatch.ElapsedMilliseconds / 1000.0:F2} seconds");

    // Get and display non-successful URLs
    var nonSuccessfulUrls = resultAggregator.GetNonSuccessfulUrls(results);

    if (nonSuccessfulUrls.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Non-successful URLs (not 2xx):");

        foreach (var url in nonSuccessfulUrls)
        {
            Console.WriteLine(url);
        }
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("All URLs returned successful (2xx) responses.");
    }

    // Export results to files
    Console.WriteLine();
    Console.WriteLine("Exporting results to files...");
    
    // Export all results to JSON
    var jsonPath = resultExporter.ExportToJson(results, rootUrl);
    Console.WriteLine($"All results exported to JSON: {jsonPath}");
    
    // Export all results to CSV
    var csvPath = resultExporter.ExportToCsv(results, rootUrl);
    Console.WriteLine($"All results exported to CSV: {csvPath}");
    
    // Export response time statistics
    var responseTimeStatsPath = resultExporter.ExportResponseTimeStatisticsToCsv(stats, rootUrl);
    Console.WriteLine($"Response time statistics exported to CSV: {responseTimeStatsPath}");
    
    // Export non-successful results to a separate CSV file
    if (nonSuccessfulUrls.Count > 0)
    {
        var errorsCsvPath = resultExporter.ExportNonSuccessfulResultsToCsv(results, rootUrl);
        Console.WriteLine($"Error results exported to CSV: {errorsCsvPath}");
    }
}
catch (Exception ex)
{
    stopwatch.Stop();
    logger.LogError(ex, "Error during crawl");
    Console.WriteLine($"Error during crawl: {ex.Message}");
}

// Keep the console window open
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// Helper method to calculate percentage
static double CalculatePercentage(int count, int total)
{
    if (total == 0)
    {
        return 0;
    }
    
    return (double)count / total * 100;
}
