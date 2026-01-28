using FiasReportGenerator.Models;
using FiasReportGenerator.Services;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FiasReportGenerator.Tests.Services;

public class AddressObjectServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<IAddressObjectParser> _parserMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly ILogger<AddressObjectService> _logger;

    public AddressObjectServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "AddressObjectServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _parserMock = new Mock<IAddressObjectParser>();
        _cacheServiceMock = new Mock<ICacheService>();
        _logger = NullLogger<AddressObjectService>.Instance;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Act
        var service = new AddressObjectService(_parserMock.Object, _cacheServiceMock.Object, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAndFilterAddressObjectsAsync_WithNonExistentDirectory_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var service = new AddressObjectService(_parserMock.Object, _cacheServiceMock.Object, _logger);
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");
        var addressLevels = new Dictionary<int, AddressLevel>();

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            service.LoadAndFilterAddressObjectsAsync(nonExistentDir, addressLevels));
    }

    [Fact]
    public async Task LoadAndFilterAddressObjectsAsync_WithEmptyDirectory_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var service = new AddressObjectService(_parserMock.Object, _cacheServiceMock.Object, _logger);
        var addressLevels = new Dictionary<int, AddressLevel>();

        // Создаем пустую директорию без файлов
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.LoadAndFilterAddressObjectsAsync(emptyDir, addressLevels));
    }
}