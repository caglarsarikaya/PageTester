using System.Text;
using System.Text.Json;
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