using System.Text;
using WebCrawler.Interfaces;
using WebCrawler.Models;
using ClosedXML.Excel;

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
    /// Exports all crawl data to a single Excel file with multiple worksheets
    /// </summary>
    /// <param name="results">The crawl results to export</param>
    /// <param name="stats">The crawl statistics to export</param>
    /// <param name="rootUrl">The root URL that was crawled</param>
    /// <returns>The path to the exported file</returns>
    public string ExportToExcel(IEnumerable<CrawlResult> results, CrawlStatistics stats, string rootUrl)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeRootUrl = GetSafeFilename(rootUrl);
        var filename = $"{safeRootUrl}_{timestamp}.xlsx";
        var filePath = Path.Combine(_resultsDirectory, filename);
        
        using var workbook = new XLWorkbook();
        
        // Create statistics worksheet
        var statsSheet = workbook.Worksheets.Add("Statistics");
        
        // Add summary statistics
        statsSheet.Cell("A1").Value = "Statistic";
        statsSheet.Cell("B1").Value = "Value";
        statsSheet.Cell("C1").Value = "Unit";
        
        // Format headers
        var headerRange = statsSheet.Range("A1:C1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        
        int row = 2;
        
        // Add visit count statistics
        statsSheet.Cell(row, 1).Value = "Total URLs Processed";
        statsSheet.Cell(row, 2).Value = stats.VisitedCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Successful Requests (2xx)";
        statsSheet.Cell(row, 2).Value = stats.SuccessCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Redirects (3xx)";
        statsSheet.Cell(row, 2).Value = stats.RedirectCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Client Errors (4xx)";
        statsSheet.Cell(row, 2).Value = stats.ClientErrorCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Server Errors (5xx)";
        statsSheet.Cell(row, 2).Value = stats.ServerErrorCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Other Errors";
        statsSheet.Cell(row, 2).Value = stats.OtherErrorCount;
        statsSheet.Cell(row, 3).Value = "count";
        row++;
        
        // Add response time statistics
        row++; // Add blank row for separation
        statsSheet.Cell(row, 1).Value = "Response Time Statistics";
        statsSheet.Range(row, 1, row, 3).Merge();
        statsSheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        statsSheet.Cell(row, 1).Value = "Minimum Response Time";
        statsSheet.Cell(row, 2).Value = stats.MinResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Maximum Response Time";
        statsSheet.Cell(row, 2).Value = stats.MaxResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Average Response Time";
        statsSheet.Cell(row, 2).Value = stats.AverageResponseTimeMs;
        statsSheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Median Response Time (P50)";
        statsSheet.Cell(row, 2).Value = stats.MedianResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "90th Percentile (P90)";
        statsSheet.Cell(row, 2).Value = stats.P90ResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "95th Percentile (P95)";
        statsSheet.Cell(row, 2).Value = stats.P95ResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        statsSheet.Cell(row, 1).Value = "99th Percentile (P99)";
        statsSheet.Cell(row, 2).Value = stats.P99ResponseTimeMs;
        statsSheet.Cell(row, 3).Value = "ms";
        row++;
        
        // Add distribution data
        row++; // Add blank row for separation
        statsSheet.Cell(row, 1).Value = "Response Time Distribution";
        statsSheet.Range(row, 1, row, 3).Merge();
        statsSheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        statsSheet.Cell(row, 1).Value = "Response Time Range";
        statsSheet.Cell(row, 2).Value = "Count";
        statsSheet.Cell(row, 3).Value = "Percentage";
        statsSheet.Range(row, 1, row, 3).Style.Font.Bold = true;
        row++;
        
        double percentUnder100ms = stats.VisitedCount > 0 ? (double)stats.ResponsesUnder100ms / stats.VisitedCount * 100 : 0;
        double percent100to500ms = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween100msAnd500ms / stats.VisitedCount * 100 : 0;
        double percent500to1s = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween500msAnd1s / stats.VisitedCount * 100 : 0;
        double percent1to3s = stats.VisitedCount > 0 ? (double)stats.ResponsesBetween1sAnd3s / stats.VisitedCount * 100 : 0;
        double percentOver3s = stats.VisitedCount > 0 ? (double)stats.ResponsesOver3s / stats.VisitedCount * 100 : 0;
        
        statsSheet.Cell(row, 1).Value = "Under 100ms";
        statsSheet.Cell(row, 2).Value = stats.ResponsesUnder100ms;
        statsSheet.Cell(row, 3).Value = percentUnder100ms / 100; // Excel percentage format
        statsSheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        row++;
        
        statsSheet.Cell(row, 1).Value = "100ms-500ms";
        statsSheet.Cell(row, 2).Value = stats.ResponsesBetween100msAnd500ms;
        statsSheet.Cell(row, 3).Value = percent100to500ms / 100;
        statsSheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        row++;
        
        statsSheet.Cell(row, 1).Value = "500ms-1s";
        statsSheet.Cell(row, 2).Value = stats.ResponsesBetween500msAnd1s;
        statsSheet.Cell(row, 3).Value = percent500to1s / 100;
        statsSheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        row++;
        
        statsSheet.Cell(row, 1).Value = "1s-3s";
        statsSheet.Cell(row, 2).Value = stats.ResponsesBetween1sAnd3s;
        statsSheet.Cell(row, 3).Value = percent1to3s / 100;
        statsSheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        row++;
        
        statsSheet.Cell(row, 1).Value = "Over 3s";
        statsSheet.Cell(row, 2).Value = stats.ResponsesOver3s;
        statsSheet.Cell(row, 3).Value = percentOver3s / 100;
        statsSheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        row++;
        
        // Auto-fit columns
        statsSheet.Columns().AdjustToContents();
        
        // Create crawl results worksheet
        var resultsSheet = workbook.Worksheets.Add("Crawl Results");
        
        // Add headers for crawl results
        resultsSheet.Cell("A1").Value = "URL";
        resultsSheet.Cell("B1").Value = "Status Code";
        resultsSheet.Cell("C1").Value = "Response Time (ms)";
        resultsSheet.Cell("D1").Value = "Success";
        resultsSheet.Cell("E1").Value = "Error Message";
        
        // Format headers
        resultsSheet.Range("A1:E1").Style.Font.Bold = true;
        resultsSheet.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Add rows
        row = 2;
        foreach (var result in results)
        {
            resultsSheet.Cell(row, 1).Value = result.Url;
            resultsSheet.Cell(row, 2).Value = result.StatusCode;
            resultsSheet.Cell(row, 3).Value = result.ResponseTimeMs;
            resultsSheet.Cell(row, 4).Value = result.IsSuccess;
            resultsSheet.Cell(row, 5).Value = result.ErrorMessage ?? string.Empty;
            row++;
        }
        
        // Auto-fit columns
        resultsSheet.Columns().AdjustToContents();
        
        // Create errors worksheet if there are any non-successful results
        var nonSuccessfulResults = results.Where(r => !r.IsSuccess).ToList();
        if (nonSuccessfulResults.Any())
        {
            var errorsSheet = workbook.Worksheets.Add("Errors");
            
            // Add headers
            errorsSheet.Cell("A1").Value = "URL";
            errorsSheet.Cell("B1").Value = "Status Code";
            errorsSheet.Cell("C1").Value = "Response Time (ms)";
            errorsSheet.Cell("D1").Value = "Error Message";
            
            // Format headers
            errorsSheet.Range("A1:D1").Style.Font.Bold = true;
            errorsSheet.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightGray;
            
            // Add rows
            row = 2;
            foreach (var result in nonSuccessfulResults)
            {
                errorsSheet.Cell(row, 1).Value = result.Url;
                errorsSheet.Cell(row, 2).Value = result.StatusCode;
                errorsSheet.Cell(row, 3).Value = result.ResponseTimeMs;
                errorsSheet.Cell(row, 4).Value = result.ErrorMessage ?? string.Empty;
                row++;
            }
            
            // Auto-fit columns
            errorsSheet.Columns().AdjustToContents();
        }
        
        workbook.SaveAs(filePath);
        
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
}