using FiasReportGenerator.Models;
using FiasReportGenerator.Services;
using FiasReportGenerator.Utils;
using FiasReportGenerator.Reports;
using FiasReportGenerator.Extensions;
using FiasReportGenerator.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Xunit;
using System.Net.Http;

namespace FiasReportGenerator.Tests.Integration;

/// <summary>
/// Integration tests for complete FIAS report generation workflow.
/// </summary>
public class FiasReportGeneratorIntegrationTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private IHost? _host;
    private IServiceProvider? _services;

    public FiasReportGeneratorIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FiasReportGeneratorIntegrationTests", Guid.NewGuid().ToString());
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
                services.AddValidatorsFromAssemblyContaining<DownloadFileInfoValidator>();
                services.AddScoped<IValidator<string>, UrlValidator>();
                services.AddScoped<IValidator<string>, FilePathValidator>(_ => new FilePathValidator());

                // Register HttpClient with test configuration
                services.AddHttpClient<IFiasApiClient, FiasApiClient>((serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FiasSettings>>().Value;
                    client.BaseAddress = new Uri(settings.ApiBaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(settings.HttpTimeoutMinutes);
                });

                // Register rate limiter
                services.AddMemoryCache();
                services.AddSingleton<IRateLimiter>(sp =>
                {
                    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                    var logger = sp.GetRequiredService<ILogger<InMemoryRateLimiter>>();
                    return new InMemoryRateLimiter(cache, logger, maxRequestsPerMinute: 30);
                });

                // Register services
                services.AddSingleton<IFileManager, FileManager>();
                services.AddScoped<AddressLevelService>();
                services.AddScoped<IAddressObjectParser, StreamingAddressObjectParser>();
                services.AddScoped<ICacheService, MemoryCacheService>();
                services.AddScoped<AddressObjectService>();

                // Register report generators
                services.AddScoped<TxtReportGenerator>(sp =>
                {
                    var heading = $"Test Report {DateTime.Now:dd.MM.yyyy}";
                    return new TxtReportGenerator(heading);
                });

                services.AddScoped<CsvReportGenerator>(_ =>
                {
                    var heading = $"Test Report {DateTime.Now:dd.MM.yyyy}";
                    return new CsvReportGenerator(heading);
                });
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
    public async Task GenerateReport_EndToEnd_ShouldCreateReports()
    {
        // Arrange
        var fileManager = _services!.GetRequiredService<IFileManager>();
        var fiasClient = _services!.GetRequiredService<IFiasApiClient>();
        var addressLevelService = _services!.GetRequiredService<AddressLevelService>();
        var addressObjectService = _services!.GetRequiredService<AddressObjectService>();
        var txtGenerator = _services!.GetRequiredService<TxtReportGenerator>();
        var csvGenerator = _services!.GetRequiredService<CsvReportGenerator>();

        var projectRoot = _testDirectory;

        // Act & Assert - Step 1: Get FIAS download info
        var downloadInfo = await fiasClient.GetLastDownloadFileInfoAsync();
        downloadInfo.Should().NotBeNull();
        downloadInfo.VersionId.Should().NotBeNullOrEmpty();
        downloadInfo.GarXMLDeltaURL.Should().NotBeNullOrEmpty();

        // Act & Assert - Step 2: Check/create directories
        fileManager.EnsureDataDirectoriesCreated(projectRoot);

        var extractedDir = fileManager.GetExtractedDirectory(projectRoot);
        Directory.Exists(extractedDir).Should().BeTrue();

        // Act & Assert - Step 3: Load address levels (using existing data)
        var addressLevels = addressLevelService.LoadAddressLevels(extractedDir);
        addressLevels.Should().NotBeNull();
        addressLevels.Count.Should().BeGreaterThan(0);

        // Act & Assert - Step 4: Load and filter address objects
        var filteredObjects = await addressObjectService.LoadAndFilterAddressObjectsAsync(extractedDir, addressLevels);
        filteredObjects.Should().NotBeNull();

        // Act & Assert - Step 5: Generate reports
        var reportsDir = fileManager.GetReportsDirectory(projectRoot);

        var txtReportPath = txtGenerator.GenerateReport(filteredObjects, reportsDir);
        var csvReportPath = csvGenerator.GenerateReport(filteredObjects, reportsDir);

        // Assert - Verify reports were created
        File.Exists(txtReportPath).Should().BeTrue();
        File.Exists(csvReportPath).Should().BeTrue();

        // Assert - Verify report content
        var txtContent = await File.ReadAllTextAsync(txtReportPath);
        var csvContent = await File.ReadAllTextAsync(csvReportPath);

        txtContent.Should().NotBeNullOrEmpty();
        csvContent.Should().NotBeNullOrEmpty();

        // TXT report should contain level information
        txtContent.Should().Contain("Address Level");

        // CSV report should contain CSV headers
        csvContent.Should().Contain("Тип объекта");
        csvContent.Should().Contain("Наименование объекта");
    }

    [Fact]
    public void FileManager_DirectoryOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var fileManager = _services!.GetRequiredService<IFileManager>();

        // Act
        var projectRoot = fileManager.GetProjectRoot();
        fileManager.EnsureDataDirectoriesCreated(projectRoot);

        // Assert
        Directory.Exists(fileManager.GetDataDirectory(projectRoot)).Should().BeTrue();
        Directory.Exists(fileManager.GetDownloadsDirectory(projectRoot)).Should().BeTrue();
        Directory.Exists(fileManager.GetExtractedDirectory(projectRoot)).Should().BeTrue();
        Directory.Exists(fileManager.GetReportsDirectory(projectRoot)).Should().BeTrue();
    }

    [Fact]
    public async Task XmlParsing_ShouldHandleExistingData()
    {
        // Arrange
        var addressObjectService = _services!.GetRequiredService<AddressObjectService>();
        var fileManager = _services!.GetRequiredService<IFileManager>();
        var addressLevelService = _services!.GetRequiredService<AddressLevelService>();

        var extractedDir = fileManager.GetExtractedDirectory(_testDirectory);

        // Act - Load address levels
        var addressLevels = addressLevelService.LoadAddressLevels(extractedDir);

        // Act - Load address objects
        var addressObjects = await addressObjectService.LoadAndFilterAddressObjectsAsync(extractedDir, addressLevels);

        // Assert
        addressLevels.Should().NotBeNull();
        addressObjects.Should().NotBeNull();

        // Should have some data if XML files exist
        if (Directory.Exists(extractedDir) &&
            Directory.GetFiles(extractedDir, "*.XML", SearchOption.AllDirectories).Any())
        {
            addressLevels.Count.Should().BeGreaterThan(0);
        }
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
