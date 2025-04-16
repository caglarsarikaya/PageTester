using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
logger.LogInformation($"Configured crawl depth: {crawlSettings.Depth}");

// Get the web crawler and result aggregator
var webCrawler = serviceProvider.GetRequiredService<IWebCrawler>();
var resultAggregator = serviceProvider.GetRequiredService<ResultAggregator>();

// Prompt for URL
Console.WriteLine("Please enter the URL to crawl:");
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
    logger.LogInformation("Added https:// prefix to URL: {Url}", rootUrl);
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

try
{
    // Start crawling
    var results = await webCrawler.CrawlAsync(rootUrl, cts.Token);
    
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
    Console.WriteLine($"Average response time: {stats.AverageResponseTimeMs:F2} ms");
    Console.WriteLine($"Total crawl time: {stats.TotalTimeMs / 1000.0:F2} seconds");
    
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
}
catch (Exception ex)
{
    logger.LogError(ex, "Error during crawl");
}

// Keep the console window open
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
