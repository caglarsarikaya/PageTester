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

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
logger.LogInformation($"Configured crawl depth: {crawlSettings.Depth}");

// Get utilities to demonstrate they're registered
var visitedUrls = serviceProvider.GetRequiredService<VisitedUrls>();
var crawlQueue = serviceProvider.GetRequiredService<CrawlQueue>();
logger.LogInformation($"Created VisitedUrls tracker with {visitedUrls.Count} entries");
logger.LogInformation($"Created CrawlQueue with {crawlQueue.Count} entries");

// Test URL fetcher
var urlFetcher = serviceProvider.GetRequiredService<IUrlFetcher>();
logger.LogInformation("URL fetcher service registered successfully");

// Keep the console window open
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
