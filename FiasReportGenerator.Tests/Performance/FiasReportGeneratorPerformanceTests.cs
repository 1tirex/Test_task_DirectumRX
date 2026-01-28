using FiasReportGenerator.Models;
using FiasReportGenerator.Services;
using FiasReportGenerator.Utils;
using FiasReportGenerator.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Xunit;
using System.Diagnostics;

namespace FiasReportGenerator.Tests.Performance;

/// <summary>
/// Performance tests for memory usage and execution time.
/// </summary>
public class FiasReportGeneratorPerformanceTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private IHost? _host;
    private IServiceProvider? _services;

    public FiasReportGeneratorPerformanceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FiasReportGeneratorPerfTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public async Task InitializeAsync()
    {
        // Create test appsettings.json
        var appsettingsPath = Path.Combine(_testDirectory, "appsettings.json");
        await File.WriteAllTextAsync(appsettingsPath, GetTestAppSettings());

        // Build host for testing
        _host = Host.CreateDefaultBuilder()
            .UseContentRoot(_testDirectory)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<FiasSettings>(context.Configuration.GetSection("Fias"));
                services.Configure<DataSettings>(context.Configuration.GetSection("Data"));
                services.Configure<CacheSettings>(context.Configuration.GetSection("Cache"));

                // Register options as services
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FiasSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>().Value);

                // Register validators
                services.AddValidatorsFromAssemblyContaining<FiasReportGenerator.Validators.DownloadFileInfoValidator>();

                // Register additional validators
                services.AddScoped<FiasReportGenerator.Validators.IValidator<string>, FiasReportGenerator.Validators.UrlValidator>();
                services.AddScoped<FiasReportGenerator.Validators.IValidator<string>, FiasReportGenerator.Validators.FilePathValidator>(sp =>
                    new FiasReportGenerator.Validators.FilePathValidator());

                // Register HttpClient with test configuration
                services.AddHttpClient<IFiasApiClient, FiasApiClient>((serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FiasSettings>>().Value;
                    client.BaseAddress = new Uri(settings.ApiBaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(settings.HttpTimeoutMinutes);
                });

                // Register services
                services.AddSingleton<IFileManager, FileManager>();
                services.AddScoped<AddressLevelService>();
                services.AddScoped<IAddressObjectParser, StreamingAddressObjectParser>();
                services.AddScoped<ICacheService, MemoryCacheService>();
                services.AddScoped<AddressObjectService>();

                // Register rate limiter
                services.AddSingleton<IRateLimiter>(sp =>
                {
                    var cache = sp.GetRequiredService<IMemoryCache>();
                    var logger = sp.GetRequiredService<ILogger<InMemoryRateLimiter>>();
                    return new InMemoryRateLimiter(cache, logger, maxRequestsPerMinute: 30);
                });

                // Register report generators
                services.AddScoped<TxtReportGenerator>(sp =>
                {
                    var heading = $"Performance Test Report {DateTime.Now:dd.MM.yyyy}";
                    return new TxtReportGenerator(heading);
                });

                services.AddScoped<CsvReportGenerator>(sp =>
                {
                    var heading = $"Performance Test Report {DateTime.Now:dd.MM.yyyy}";
                    return new CsvReportGenerator(heading);
                });

                // Register caching
                services.AddMemoryCache();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
            })
            .Build();

        _services = _host.Services;
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task XmlParsing_Performance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var parser = _services!.GetRequiredService<IAddressObjectParser>();
        var fileManager = _services!.GetRequiredService<IFileManager>();
        var extractedDir = fileManager.GetExtractedDirectory(_testDirectory);

        // Find XML files to test
        var xmlFiles = Directory.GetFiles(extractedDir, "*.XML", SearchOption.AllDirectories);
        xmlFiles.Should().NotBeEmpty("Test requires XML files in extracted directory");

        var testFile = xmlFiles.First();

        // Act - Measure performance
        var stopwatch = Stopwatch.StartNew();
        var count = 0;

        await foreach (var addressObject in parser.ParseAddressObjectsAsync(testFile))
        {
            count++;
        }

        stopwatch.Stop();

        // Assert - Performance requirements
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "XML parsing should complete within 30 seconds");
        count.Should().BeGreaterThan(0, "Should parse at least one object");

        var objectsPerSecond = count / (stopwatch.ElapsedMilliseconds / 1000.0);
        objectsPerSecond.Should().BeGreaterThan(10, "Should parse at least 10 objects per second");

        // Log performance metrics
        Console.WriteLine($"Performance Test Results:");
        Console.WriteLine($"File: {Path.GetFileName(testFile)}");
        Console.WriteLine($"Objects parsed: {count}");
        Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Objects/second: {objectsPerSecond:F2}");
    }

    [Fact]
    public async Task MemoryCache_Performance_ShouldBeFasterOnSecondCall()
    {
        // Arrange
        var cacheService = _services!.GetRequiredService<ICacheService>();
        var fileManager = _services!.GetRequiredService<IFileManager>();
        var extractedDir = fileManager.GetExtractedDirectory(_testDirectory);

        // Act - First call (cache miss)
        var stopwatch1 = Stopwatch.StartNew();
        var addressLevels1 = await cacheService.GetAddressLevelsAsync(extractedDir);
        stopwatch1.Stop();

        // Act - Second call (cache hit)
        var stopwatch2 = Stopwatch.StartNew();
        var addressLevels2 = await cacheService.GetAddressLevelsAsync(extractedDir);
        stopwatch2.Stop();

        // Assert
        addressLevels1.Should().NotBeNull();
        addressLevels2.Should().NotBeNull();
        addressLevels1.Count.Should().Be(addressLevels2.Count);

        // Cache hit should be significantly faster
        stopwatch2.ElapsedMilliseconds.Should().BeLessThan(stopwatch1.ElapsedMilliseconds / 2,
            "Cache hit should be at least 2x faster than cache miss");

        // Log performance comparison
        Console.WriteLine($"Cache Performance Test:");
        Console.WriteLine($"First call (cache miss): {stopwatch1.ElapsedMilliseconds}ms");
        Console.WriteLine($"Second call (cache hit): {stopwatch2.ElapsedMilliseconds}ms");
        Console.WriteLine($"Speedup: {stopwatch1.ElapsedMilliseconds / (double)stopwatch2.ElapsedMilliseconds:F2}x");
    }

    [Fact]
    public async Task ReportGeneration_MemoryUsage_ShouldNotExceedLimit()
    {
        // Arrange
        var addressObjectService = _services!.GetRequiredService<AddressObjectService>();
        var cacheService = _services!.GetRequiredService<ICacheService>();
        var txtGenerator = _services!.GetRequiredService<TxtReportGenerator>();
        var csvGenerator = _services!.GetRequiredService<CsvReportGenerator>();
        var fileManager = _services!.GetRequiredService<IFileManager>();

        var extractedDir = fileManager.GetExtractedDirectory(_testDirectory);
        var reportsDir = fileManager.GetReportsDirectory(_testDirectory);

        // Act - Load data
        var addressLevels = await cacheService.GetAddressLevelsAsync(extractedDir);
        var filteredObjects = await addressObjectService.LoadAndFilterAddressObjectsAsync(extractedDir, addressLevels);

        // Measure memory before report generation
        var memoryBefore = GC.GetTotalMemory(true);

        // Act - Generate reports
        var txtReportPath = txtGenerator.GenerateReport(filteredObjects, reportsDir);
        var csvReportPath = csvGenerator.GenerateReport(filteredObjects, reportsDir);

        // Force GC to get accurate memory measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryAfter = GC.GetTotalMemory(true);

        var memoryUsed = memoryAfter - memoryBefore;

        // Assert - Memory usage should be reasonable
        memoryUsed.Should().BeLessThan(50 * 1024 * 1024, "Report generation should use less than 50MB of additional memory");

        // Assert - Files should be created
        File.Exists(txtReportPath).Should().BeTrue();
        File.Exists(csvReportPath).Should().BeTrue();

        // Log memory usage
        Console.WriteLine($"Memory Usage Test:");
        Console.WriteLine($"Memory before: {memoryBefore / 1024 / 1024:F2}MB");
        Console.WriteLine($"Memory after: {memoryAfter / 1024 / 1024:F2}MB");
        Console.WriteLine($"Memory used: {memoryUsed / 1024 / 1024:F2}MB");
        Console.WriteLine($"TXT report size: {new FileInfo(txtReportPath).Length / 1024:F2}KB");
        Console.WriteLine($"CSV report size: {new FileInfo(csvReportPath).Length / 1024:F2}KB");
    }

    [Fact]
    public async Task ConcurrentRequests_RateLimiting_ShouldWork()
    {
        // Arrange
        var rateLimiter = _services!.GetRequiredService<IRateLimiter>();
        const string testKey = "test_concurrent";
        const int requestCount = 35; // More than the 30 per minute limit

        // Act - Make concurrent requests
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => rateLimiter.IsRequestAllowedAsync(testKey))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allowedRequests = results.Count(r => r);
        var blockedRequests = results.Count(r => !r);

        // Assert - Should allow up to limit and block excess
        allowedRequests.Should().BeLessThanOrEqualTo(30, "Should not allow more than 30 requests per minute");
        blockedRequests.Should().BeGreaterThan(0, "Should block some requests when limit exceeded");

        // Log results
        Console.WriteLine($"Rate Limiting Test:");
        Console.WriteLine($"Total requests: {requestCount}");
        Console.WriteLine($"Allowed: {allowedRequests}");
        Console.WriteLine($"Blocked: {blockedRequests}");
    }

    private static string GetTestAppSettings() => """
    {
      "Fias": {
        "ApiBaseUrl": "https://fias.nalog.ru/WebServices/Public",
        "LastDownloadFileInfoPath": "/GetLastDownloadFileInfo",
        "HttpTimeoutMinutes": 10,
        "MaxRetryAttempts": 3,
        "RetryBaseDelaySeconds": 2
      },
      "Data": {
        "DirectoryName": "Data",
        "DownloadsDirectoryName": "downloads",
        "ExtractedDirectoryName": "extracted",
        "ReportsDirectoryName": "reports"
      },
      "Cache": {
        "AddressLevelsExpirationHours": 1,
        "MemoryCacheSizeLimit": 1024
      },
      "Logging": {
        "LogLevel": {
          "Default": "Warning",
          "Microsoft": "Warning"
        }
      }
    }
    """;
}

