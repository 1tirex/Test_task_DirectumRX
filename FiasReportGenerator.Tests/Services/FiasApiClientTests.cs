using FiasReportGenerator.Models;
using FiasReportGenerator.Services;
using FiasReportGenerator.Validators;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiasReportGenerator.Tests.Services;

public class FiasApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly FiasSettings _settings;
    private readonly ILogger<FiasApiClient> _logger;
    private readonly Mock<IValidator<DownloadFileInfo>> _downloadFileInfoValidatorMock;
    private readonly Mock<IValidator<string>> _urlValidatorMock;
    private readonly Mock<IValidator<string>> _filePathValidatorMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly FiasApiClient _apiClient;

    public FiasApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _settings = new FiasSettings
        {
            ApiBaseUrl = "https://test.fias.api",
            LastDownloadFileInfoPath = "/GetLastDownloadFileInfo",
            HttpTimeoutMinutes = 5
        };

        _logger = NullLogger<FiasApiClient>.Instance;

        _downloadFileInfoValidatorMock = new Mock<IValidator<DownloadFileInfo>>();
        _downloadFileInfoValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _urlValidatorMock = new Mock<IValidator<string>>();
        _urlValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _filePathValidatorMock = new Mock<IValidator<string>>();
        _filePathValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _rateLimiterMock = new Mock<IRateLimiter>();
        _rateLimiterMock
            .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _apiClient = new FiasApiClient(
            _httpClient,
            _settings,
            _logger,
            _downloadFileInfoValidatorMock.Object,
            _urlValidatorMock.Object,
            _filePathValidatorMock.Object,
            _rateLimiterMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Assert
        _apiClient.Should().NotBeNull();
    }

    [Fact]
    public async Task DownloadFileAsync_WithInvalidUrl_ShouldThrowArgumentException()
    {
        // Arrange
        _urlValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("url", "Invalid URL")
            }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _apiClient.DownloadFileAsync("", "destination"));
    }

    [Fact]
    public async Task DownloadFileAsync_WithInvalidDestination_ShouldThrowArgumentException()
    {
        // Arrange
        _filePathValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("destination", "Invalid path")
            }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _apiClient.DownloadFileAsync("http://example.com", ""));
    }

    [Fact]
    public async Task GetLastDownloadFileInfoAsync_WithValidResponse_ShouldParseCorrectly()
    {
        // Arrange
        var jsonResponse = """
        {
          "VersionId": "20260127",
          "TextVersion": "Test Version",
          "Date": "27.01.2026",
          "GarXMLDeltaURL": "https://example.com/delta.zip",
          "GarXMLFullURL": "https://example.com/full.zip"
        }
        """;

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/GetLastDownloadFileInfo")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _apiClient.GetLastDownloadFileInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.VersionId.Should().Be("20260127");
        result.TextVersion.Should().Be("Test Version");
        result.Date.Should().Be(new DateTime(2026, 1, 27));
        result.GarXMLDeltaURL.Should().Be("https://example.com/delta.zip");
        result.GarXMLFullURL.Should().Be("https://example.com/full.zip");
    }

    [Fact]
    public async Task GetLastDownloadFileInfoAsync_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJson)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _apiClient.GetLastDownloadFileInfoAsync());
    }

    [Fact]
    public async Task GetLastDownloadFileInfoAsync_WithHttpError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _apiClient.GetLastDownloadFileInfoAsync());
    }
}
