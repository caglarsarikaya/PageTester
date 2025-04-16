using System.Text;
using System.Text.Json;
using WebCrawler.Interfaces;
using WebCrawler.Models;

namespace WebCrawler.Services;

/// <summary>
/// Service for exporting crawl results to files
/// </summary>
public class ResultExporter
{
    private readonly string _resultsDirectory;

    public ResultExporter()
    {
        // Set the results directory to be in the same place as the code
        _resultsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");

        // Ensure the Results directory exists
        if (!Directory.Exists(_resultsDirectory))
        {
            Directory.CreateDirectory(_resultsDirectory);
        }
    }

    /// <summary>
    /// Exports crawl results to a JSON file
    /// </summary>
    /// <param name="results">The crawl results to export</param>
    /// <param name="rootUrl">The root URL that was crawled</param>
    /// <returns>The path to the exported file</returns>
    public string ExportToJson(IEnumerable<CrawlResult> results, string rootUrl)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeRootUrl = GetSafeFilename(rootUrl);
        var filename = $"{safeRootUrl}_{timestamp}.json";
        var filePath = Path.Combine(_resultsDirectory, filename);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(results, jsonOptions);
        File.WriteAllText(filePath, json);

        return filePath;
    }

    /// <summary>
    /// Exports crawl results to a CSV file
    /// </summary>
    /// <param name="results">The crawl results to export</param>
    /// <param name="rootUrl">The root URL that was crawled</param>
    /// <returns>The path to the exported file</returns>
    public string ExportToCsv(IEnumerable<CrawlResult> results, string rootUrl)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeRootUrl = GetSafeFilename(rootUrl);
        var filename = $"{safeRootUrl}_{timestamp}.csv";
        var filePath = Path.Combine(_resultsDirectory, filename);

        var csv = new StringBuilder();

        // Add CSV header
        csv.AppendLine("URL,StatusCode,ResponseTimeMs,ErrorMessage");

        // Add rows
        foreach (var result in results)
        {
            var url = EscapeCsvField(result.Url);
            var errorMessage = EscapeCsvField(result.ErrorMessage ?? string.Empty);
            csv.AppendLine($"{url},{result.StatusCode},{result.ResponseTimeMs},{errorMessage}");
        }

        File.WriteAllText(filePath, csv.ToString());

        return filePath;
    }

    /// <summary>
    /// Exports non-successful results to a separate file
    /// </summary>
    /// <param name="results">The crawl results to filter and export</param>
    /// <param name="rootUrl">The root URL that was crawled</param>
    /// <returns>The path to the exported file</returns>
    public string ExportNonSuccessfulResultsToCsv(IEnumerable<CrawlResult> results, string rootUrl)
    {
        var nonSuccessfulResults = results.Where(r => !r.IsSuccess).ToList();

        if (!nonSuccessfulResults.Any())
        {
            return string.Empty;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeRootUrl = GetSafeFilename(rootUrl);
        var filename = $"{safeRootUrl}_{timestamp}_errors.csv";
        var filePath = Path.Combine(_resultsDirectory, filename);

        var csv = new StringBuilder();

        // Add CSV header
        csv.AppendLine("URL,StatusCode,ResponseTimeMs,ErrorMessage");

        // Add rows
        foreach (var result in nonSuccessfulResults)
        {
            var url = EscapeCsvField(result.Url);
            var errorMessage = EscapeCsvField(result.ErrorMessage ?? string.Empty);
            csv.AppendLine($"{url},{result.StatusCode},{result.ResponseTimeMs},{errorMessage}");
        }

        File.WriteAllText(filePath, csv.ToString());

        return filePath;
    }

    /// <summary>
    /// Exports response time statistics to a CSV file
    /// </summary>
    /// <param name="stats">The crawl statistics to export</param>
    /// <param name="rootUrl">The root URL that was crawled</param>
    /// <returns>The path to the exported file</returns>
    public string ExportResponseTimeStatisticsToCsv(CrawlStatistics stats, string rootUrl)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeRootUrl = GetSafeFilename(rootUrl);
        var filename = $"{safeRootUrl}_{timestamp}_response_times.csv";
        var filePath = Path.Combine(_resultsDirectory, filename);
        
        var csv = new StringBuilder();
        
        // Add summary statistics
        csv.AppendLine("Statistic,Value,Unit");
        csv.AppendLine($"Total URLs Processed,{stats.VisitedCount},count");
        csv.AppendLine($"Minimum Response Time,{stats.MinResponseTimeMs},ms");
        csv.AppendLine($"Maximum Response Time,{stats.MaxResponseTimeMs},ms");
        csv.AppendLine($"Average Response Time,{stats.AverageResponseTimeMs:F2},ms");
        csv.AppendLine($"Median Response Time (P50),{stats.MedianResponseTimeMs},ms");
        csv.AppendLine($"90th Percentile (P90),{stats.P90ResponseTimeMs},ms");
        csv.AppendLine($"95th Percentile (P95),{stats.P95ResponseTimeMs},ms");
        csv.AppendLine($"99th Percentile (P99),{stats.P99ResponseTimeMs},ms");
        
        // Add distribution data
        csv.AppendLine();
        csv.AppendLine("Response Time Range,Count,Percentage");
        
        double percentUnder100ms = stats.VisitedCount > 0 ? (double)stats.ResponsesUnder100ms / stats.VisitedCount * 100 : 0;
        double percent100to500ms = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween100msAnd500ms / stats.VisitedCount * 100 : 0;
        double percent500to1s = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween500msAnd1s / stats.VisitedCount * 100 : 0;
        double percent1to3s = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween1sAnd3s / stats.VisitedCount * 100 : 0;
        double percentOver3s = stats.VisitedCount > 0 ? (double)stats.ResponsesOver3s / stats.VisitedCount * 100 : 0;
        
        csv.AppendLine($"Under 100ms,{stats.ResponsesUnder100ms},{percentUnder100ms:F2}%");
        csv.AppendLine($"100ms-500ms,{stats.ResponsesBetween100msAnd500ms},{percent100to500ms:F2}%");
        csv.AppendLine($"500ms-1s,{stats.ResponsesBetween500msAnd1s},{percent500to1s:F2}%");
        csv.AppendLine($"1s-3s,{stats.ResponsesBetween1sAnd3s},{percent1to3s:F2}%");
        csv.AppendLine($"Over 3s,{stats.ResponsesOver3s},{percentOver3s:F2}%");
        
        // Add raw response times
        if (stats.AllResponseTimes.Count > 0)
        {
            csv.AppendLine();
            csv.AppendLine("ResponseTime (ms)");
            
            // Sort response times from slowest to fastest for easier analysis
            foreach (var responseTime in stats.AllResponseTimes.OrderByDescending(t => t))
            {
                csv.AppendLine($"{responseTime}");
            }
        }
        
        File.WriteAllText(filePath, csv.ToString());
        
        return filePath;
    }

    /// <summary>
    /// Gets a safe filename from a URL
    /// </summary>
    /// <param name="url">The URL to convert to a safe filename</param>
    /// <returns>A safe filename derived from the URL</returns>
    private string GetSafeFilename(string url)
    {
        try
        {
            // Extract domain from URL
            var uri = new Uri(url);
            var host = uri.Host;

            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Join("_", host.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Limit the length
            if (safeName.Length > 30)
            {
                safeName = safeName.Substring(0, 30);
            }

            return safeName;
        }
        catch
        {
            // If URL parsing fails, return a generic name
            return "crawl_results";
        }
    }

    /// <summary>
    /// Escapes a field for CSV format
    /// </summary>
    /// <param name="field">The field value to escape</param>
    /// <returns>The escaped field value</returns>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        bool requiresQuoting = field.Contains(',') || field.Contains('"') || field.Contains('\r') || field.Contains('\n');

        if (requiresQuoting)
        {
            // Double up any double quotes
            field = field.Replace("\"", "\"\"");
            // Wrap in quotes
            return $"\"{field}\"";
        }

        return field;
    }
}