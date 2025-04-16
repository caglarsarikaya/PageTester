# Page Tester

A powerful and efficient web page testing tool that analyzes website response times and detects errors. This tool helps website administrators and developers identify performance bottlenecks and problematic URLs across their websites.

## Features

- **Configurable Test Depth**: Control how deep the tester should navigate from the starting URL
- **Comprehensive Statistics**: Get detailed response time statistics including min, max, average, median, and percentiles
- **Response Time Distribution**: See the distribution of response times across different buckets
- **Error Detection**: Automatically identifies and reports any non-successful HTTP responses
- **Excel Export**: Exports all results to Excel with multiple sheets for easy analysis
- **Interactive Console UI**: Simple console interface for initiating tests and viewing results

## Requirements

- .NET 7.0 or higher
- Windows, macOS, or Linux operating system

## Installation

1. Clone this repository:
   ```
   git clone https://github.com/yourusername/ErrorReporter.git
   ```

2. Navigate to the project directory:
   ```
   cd ErrorReporter
   ```

3. Build the application:
   ```
   dotnet build
   ```

## Usage

1. Run the application:
   ```
   dotnet run --project PageTester/PageTester.csproj
   ```

2. When prompted, enter the URL you want to test:
   ```
   Please enter the URL to test: https://example.com
   ```

3. The tester will start processing the website and display progress in the console.

4. Once completed, you'll see detailed statistics about the operation.

5. Results are automatically exported to an Excel file in the output directory.

## Configuration

You can modify test settings in the `appsettings.json` file:

```json
{
  "TestSettings": {
    "Depth": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

- `Depth`: Control how many levels deep the tester will navigate from the starting URL

## Sample Output

Running the tester on a sample website produces output like this:

```
Test completed!
Total URLs processed: 291
Successful (2xx): 291
Redirects (3xx): 0
Client errors (4xx): 0
Server errors (5xx): 0
Other errors: 0

Response Time Statistics:
  Minimum: 205 ms
  Maximum: 3041 ms
  Average: 828.46 ms
  Median (P50): 1044 ms
  90th Percentile (P90): 1119 ms
  95th Percentile (P95): 1258 ms
  99th Percentile (P99): 1820 ms

Response Time Distribution:
  Under 100ms: 0 requests (0.0%)
  100ms-500ms: 90 requests (30.9%)
  500ms-1s: 8 requests (2.7%)
  1s-3s: 192 requests (66.0%)
  Over 3s: 1 requests (0.3%)
Total test time: 241.36 seconds

All URLs returned successful (2xx) responses.
```

## Analyzing Results

The tester generates statistics that can help you identify:

1. **Performance Issues**: Look for URLs with high response times (especially over 1-3 seconds)
2. **Error Pages**: Any URLs resulting in 4xx or 5xx status codes
3. **Overall Site Health**: Distribution of response times across your site

## Excel Export

All test results are automatically exported to Excel with the following sheets:
- Summary: Overview of the test operation
- All URLs: Complete list of all tested URLs with status codes and response times
- Errors: Any URLs that returned non-2xx status codes
- Performance: URLs sorted by response time (slowest first)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

https://www.youtube.com/shorts/lmRb-xfYtc8
https://www.youtube.com/watch?v=2WWE4o4kJLM
