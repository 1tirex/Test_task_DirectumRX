using FiasReportGenerator.Models;
using FiasReportGenerator.Reports;
using FluentAssertions;
using Xunit;

namespace FiasReportGenerator.Tests.Reports;

public class CsvReportGeneratorTests : IDisposable
{
    private readonly string _testDirectory;

    public CsvReportGeneratorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsvReportGeneratorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GenerateReport_WithValidData_ShouldCreateCsvFile()
    {
        // Arrange
        var heading = "Test Report";
        var generator = new CsvReportGenerator();

        var addressLevel = new AddressLevel { Level = 7, Name = "Street", ShortName = "ул" };
        var addresses = new List<AddressObject>
        {
            new() { ObjectId = Guid.NewGuid(), ObjectTypeName = "улица", Name = "Ленина", TypeName = "ул", Level = 7, IsActive = true },
            new() { ObjectId = Guid.NewGuid(), ObjectTypeName = "улица", Name = "Пушкина", TypeName = "ул", Level = 7, IsActive = true }
        };

        var data = new Dictionary<AddressLevel, List<AddressObject>>
        {
            { addressLevel, addresses }
        };

        // Act
        var resultPath = generator.GenerateReport(data, _testDirectory, heading);

        // Assert
        File.Exists(resultPath).Should().BeTrue();
        resultPath.Should().EndWith("Отчет.csv");
    }

    [Fact]
    public void GenerateReport_ShouldGenerateCorrectCsvContent()
    {
        // Arrange
        var heading = "Test Report";
        var generator = new CsvReportGenerator();

        var addressLevel = new AddressLevel { Level = 7, Name = "Street", ShortName = "ул" };
        var addresses = new List<AddressObject>
        {
            new() { ObjectId = Guid.NewGuid(), ObjectTypeName = "улица", Name = "Ленина", TypeName = "ул", Level = 7, IsActive = true },
            new() { ObjectId = Guid.NewGuid(), ObjectTypeName = "улица", Name = "Пушкина", TypeName = "ул", Level = 7, IsActive = true }
        };

        var data = new Dictionary<AddressLevel, List<AddressObject>>
        {
            { addressLevel, addresses }
        };

        // Act
        var resultPath = generator.GenerateReport(data, _testDirectory, heading);
        var content = File.ReadAllText(resultPath);

        // Assert
        content.Should().Contain($"\"{heading}\"");
        content.Should().Contain("\"Таблица 7: Street\"");
        content.Should().Contain("\"Количество объектов: 2\"");
        content.Should().Contain("\"Тип объекта\";\"Наименование объекта\"");
        content.Should().Contain("\"ул\";\"Ленина\"");
        content.Should().Contain("\"ул\";\"Пушкина\"");
    }

    [Fact]
    public void GenerateReport_WithSpecialCharacters_ShouldEscapeQuotes()
    {
        // Arrange
        var heading = "Test Report";
        var generator = new CsvReportGenerator();

        var addressLevel = new AddressLevel { Level = 7, Name = "Street", ShortName = "ул" };
        var addresses = new List<AddressObject>
        {
            new() { ObjectId = Guid.NewGuid(), ObjectTypeName = "улица", Name = "Улица \"Ленина\"", TypeName = "ул", Level = 7, IsActive = true }
        };

        var data = new Dictionary<AddressLevel, List<AddressObject>>
        {
            { addressLevel, addresses }
        };

        // Act
        var resultPath = generator.GenerateReport(data, _testDirectory, heading);
        var content = File.ReadAllText(resultPath);

        // Assert
        content.Should().Contain("\"ул\";\"Улица \"\"Ленина\"\"\"");
    }

    [Fact]
    public void GenerateReport_WithInvalidData_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new CsvReportGenerator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => generator.GenerateReport("invalid data", _testDirectory, "Test"));
    }

    [Fact]
    public void GenerateReport_WithEmptyDictionary_ShouldCreateValidCsv()
    {
        // Arrange
        var generator = new CsvReportGenerator();
        var data = new Dictionary<AddressLevel, List<AddressObject>>();

        // Act
        var resultPath = generator.GenerateReport(data, _testDirectory, "Test");

        // Assert
        File.Exists(resultPath).Should().BeTrue();
        var content = File.ReadAllText(resultPath);
        content.Should().Contain("\"Test\"");
    }
}
