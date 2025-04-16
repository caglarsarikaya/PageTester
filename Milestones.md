## ✅ Project Plan for .NET 9 Web Crawler

### **Task 1: Setup the Project**
**1.1** Create a new .NET 9 Console Application  
**1.2** Add required NuGet packages:
- `System.Net.Http`
- `HtmlAgilityPack` (for HTML parsing)
- `Microsoft.Extensions.Configuration` (for `appsettings.json`)
- `Microsoft.Extensions.Logging.Console`

**1.3** Create `appsettings.json` with:
```json
{
  "CrawlSettings": {
    "Depth": 2
  }
}
```

---

### **Task 2: Configuration & DI Setup**
**2.1** Create a `CrawlSettings` class to bind config  
**2.2** Register configuration and settings in `Program.cs`  
**2.3** Setup basic logging

---

### **Task 3: Core Models & Utilities**
**3.1** Create a model `CrawlResult` with:
- URL
- Status Code
- Error Message (if any)

**3.2** Create a thread-safe `VisitedUrls` HashSet or `ConcurrentDictionary`  
**3.3** Implement a simple `Queue<UrlWithDepth>` structure to manage the crawl logic

---

### **Task 4: URL Fetcher**
**4.1** Create `IUrlFetcher` interface  
**4.2** Implement `HttpUrlFetcher` using `HttpClient`  
- Add retry logic (maybe with Polly if needed later)
- Return status code, and log non-200 responses

---

### **Task 5: HTML Parser**
**5.1** Use HtmlAgilityPack to extract links (`<a href="">`)  
**5.2** Normalize relative URLs to absolute ones  
**5.3** Filter only HTTP/HTTPS links, related with root url, I dont need to visit external links

---

### **Task 6: Crawl Logic**
**6.1** Create `WebCrawler` class:
- Accepts root URL, depth, fetcher, parser, and logger
- Adds root to queue
- While queue not empty:
  - Pop URL
  - If not visited:
    - Fetch
    - If depth > 0 → parse and enqueue children

**6.2** Maintain depth tracking correctly to avoid over-crawling  
**6.3** Track visited URLs and prevent revisiting

---

### **Task 7: Result Aggregation & Output**
**7.1** After crawling, group results:
- Group by HTTP status code
- Print success and failures clearly in console

---

### **Task 8: Nice-to-Haves / Polish**
**8.1** Add Stopwatch to show crawl duration  
**8.2** Add cancellation token support  
**8.3** Export results to a file (CSV or JSON)

---

Let me know if you want the Cursor rule file for this architecture, or sample implementations for any of these tasks.