using FiasReportGenerator.Utils;
using FiasReportGenerator.Models;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiasReportGenerator.Tests.Utils;

public class FileManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileManager _fileManager;
    private readonly DataSettings _settings;

    public FileManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FiasReportGeneratorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _settings = new DataSettings
        {
            DirectoryName = "Data",
            DownloadsDirectoryName = "downloads",
            ExtractedDirectoryName = "extracted",
            ReportsDirectoryName = "reports"
        };

        _fileManager = new FileManager(_settings, NullLogger<FileManager>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetProjectRoot_ShouldReturnValidPath()
    {
        // Act
        var result = _fileManager.GetProjectRoot();

        // Assert
        result.Should().NotBeNullOrEmpty();
        Directory.Exists(result).Should().BeTrue();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetDataDirectory_ShouldReturnDataSubdirectory()
    {
        // Arrange
        var projectRoot = _testDirectory;

        // Act
        var result = _fileManager.GetDataDirectory(projectRoot);

        // Assert
        result.Should().Be(Path.Combine(projectRoot, _settings.DirectoryName));
    }

    [Fact]
    public void GetDownloadsDirectory_ShouldReturnDownloadsSubdirectory()
    {
        // Arrange
        var projectRoot = _testDirectory;

        // Act
        var result = _fileManager.GetDownloadsDirectory(projectRoot);

        // Assert
        result.Should().Be(Path.Combine(projectRoot, _settings.DirectoryName, _settings.DownloadsDirectoryName));
    }

    [Fact]
    public void GetExtractedDirectory_ShouldReturnExtractedSubdirectory()
    {
        // Arrange
        var projectRoot = _testDirectory;

        // Act
        var result = _fileManager.GetExtractedDirectory(projectRoot);

        // Assert
        result.Should().Be(Path.Combine(projectRoot, _settings.DirectoryName, _settings.ExtractedDirectoryName));
    }

    [Fact]
    public void GetReportsDirectory_ShouldReturnReportsSubdirectory()
    {
        // Arrange
        var projectRoot = _testDirectory;

        // Act
        var result = _fileManager.GetReportsDirectory(projectRoot);

        // Assert
        result.Should().Be(Path.Combine(projectRoot, _settings.DirectoryName, _settings.ReportsDirectoryName));
    }

    [Fact]
    public void EnsureDataDirectoriesCreated_ShouldCreateAllDirectories()
    {
        // Act
        _fileManager.EnsureDataDirectoriesCreated(_testDirectory);

        // Assert
        Directory.Exists(_fileManager.GetDownloadsDirectory(_testDirectory)).Should().BeTrue();
        Directory.Exists(_fileManager.GetExtractedDirectory(_testDirectory)).Should().BeTrue();
        Directory.Exists(_fileManager.GetReportsDirectory(_testDirectory)).Should().BeTrue();
    }

    [Fact]
    public void EnsureDataDirectoriesCreated_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileManager.EnsureDataDirectoriesCreated(""));
    }

    [Fact]
    public void EnsureDataDirectoriesCreated_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileManager.EnsureDataDirectoriesCreated(null!));
    }

    [Fact]
    public async Task ExtractZipFileAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentZip = Path.Combine(_testDirectory, "nonexistent.zip");
        var destinationDir = Path.Combine(_testDirectory, "extracted");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileManager.ExtractZipFileAsync(nonExistentZip, destinationDir));
    }

    [Fact]
    public async Task ExtractZipFileAsync_WithEmptyZipPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _fileManager.ExtractZipFileAsync("", "destination"));
    }

    [Fact]
    public async Task ExtractZipFileAsync_WithEmptyDestination_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _fileManager.ExtractZipFileAsync("zipfile.zip", ""));
    }
}

