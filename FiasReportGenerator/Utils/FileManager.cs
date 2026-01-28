using FiasReportGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FiasReportGenerator.Utils;

/// <summary>
/// Сервис для работы с файлами
/// </summary>
public class FileManager : IFileManager
{
    private const string SolutionFileName = "FiasReportGenerator.sln";
    private readonly DataSettings _settings;
    private readonly ILogger<FileManager> _logger;

    public FileManager(DataSettings settings, ILogger<FileManager> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetProjectRoot()
    {
        var candidates = new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory),
        };

        foreach (var start in candidates)
        {
            var solutionDir = FindUpwards(start, SolutionFileName);
            if (solutionDir is not null)
            {
                // Сначала проверяем родительскую директорию (предпочтительно, когда .sln находится в папке проекта)
                var parentDir = solutionDir.Parent;
                if (parentDir is not null && Directory.Exists(Path.Combine(parentDir.FullName, _settings.DirectoryName)))
                {
                    _logger.LogInformation("Определен корень проекта (родительская директория решения): {ProjectRoot}", parentDir.FullName);
                    return parentDir.FullName;
                }

                // Иначе проверяем, существует ли директория Data в директории решения
                if (Directory.Exists(Path.Combine(solutionDir.FullName, _settings.DirectoryName)))
                {
                    _logger.LogInformation("Определен корень проекта (директория с решением): {ProjectRoot}", solutionDir.FullName);
                    return solutionDir.FullName;
                }

                // Откат к директории решения
                _logger.LogInformation("Используется директория с решением как корень проекта: {ProjectRoot}", solutionDir.FullName);
                return solutionDir.FullName;
            }
        }

        var errorMessage = $"Не удалось определить корень проекта. Ожидалось найти `{SolutionFileName}` в текущей или родительских директориях. " +
                          $"Текущая директория=`{Directory.GetCurrentDirectory()}`, BaseDir=`{AppContext.BaseDirectory}`";
        _logger.LogError(errorMessage);
        throw new DirectoryNotFoundException(errorMessage);
    }

    public string GetDataDirectory(string projectRoot)
        => Path.Combine(projectRoot, _settings.DirectoryName);

    public string GetDownloadsDirectory(string projectRoot)
        => Path.Combine(GetDataDirectory(projectRoot), _settings.DownloadsDirectoryName);

    public string GetExtractedDirectory(string projectRoot)
        => Path.Combine(GetDataDirectory(projectRoot), _settings.ExtractedDirectoryName);

    public string GetReportsDirectory(string projectRoot)
        => Path.Combine(GetDataDirectory(projectRoot), _settings.ReportsDirectoryName);

    public void EnsureDataDirectoriesCreated(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new ArgumentException("Корневой путь проекта пуст", nameof(projectRoot));
        }

        _logger.LogInformation("Обеспечение создания каталогов данных для корневой папки проекта: {ProjectRoot}", projectRoot);

        var downloadsDir = GetDownloadsDirectory(projectRoot);
        var extractedDir = GetExtractedDirectory(projectRoot);
        var reportsDir = GetReportsDirectory(projectRoot);

        Directory.CreateDirectory(downloadsDir);
        Directory.CreateDirectory(extractedDir);
        Directory.CreateDirectory(reportsDir);
    }

    /// <summary>
    /// Синхронно распаковывает ZIP-архив в указанную директорию.
    /// </summary>
    /// <param name="zipFilePath">Путь к ZIP</param>
    /// <param name="destinationDirectory">Директория для распаковки.</param>
    /// <exception cref="ArgumentException">При некорректных параметрах.</exception>
    /// <exception cref="FileNotFoundException">Если ZIP не найден.</exception>
    /// <exception cref="IOException">При ошибках работы с файлами.</exception>
    /// <exception cref="InvalidDataException">При ошибках формата ZIP.</exception>
    public static void ExtractZipFile(string zipFilePath, string destinationDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
                throw new ArgumentException("Путь к ZIP пустой или null.", nameof(zipFilePath));
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Каталог пустой или null.", nameof(destinationDirectory));

            if (!File.Exists(zipFilePath))
                throw new FileNotFoundException($"ZIP не найдет: {zipFilePath}", zipFilePath);

            Directory.CreateDirectory(destinationDirectory);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory, overwriteFiles: true);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not FileNotFoundException)
        {
            throw new IOException($"Не удалось извлечь ZIP. {zipFilePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Асинхронно распаковывает ZIP в указанную директорию.
    /// </summary>
    /// <param name="zipFilePath">Путь к ZIP.</param>
    /// <param name="destinationDirectory">Директория для распаковки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <exception cref="ArgumentException">При некорректных параметрах.</exception>
    /// <exception cref="FileNotFoundException">Если ZIP не найден.</exception>
    /// <exception cref="IOException">При ошибках работы с файлами.</exception>
    /// <exception cref="InvalidDataException">При ошибках формата ZIP.</exception>
    /// <exception cref="OperationCanceledException">При отмене операции.</exception>
    public async Task ExtractZipFileAsync(string zipFilePath, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zipFilePath))
            throw new ArgumentException("ZIP пустой или null", nameof(zipFilePath));
        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new ArgumentException("Директория пустая или null", nameof(destinationDirectory));

        try
        {
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException($"ZIP не найдет: {zipFilePath}", zipFilePath);
            }

            Directory.CreateDirectory(destinationDirectory);

            // Используем Task.Run для выполнения синхронной операции в фоне
            await Task.Run(() =>
                System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory, overwriteFiles: true), cancellationToken);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not FileNotFoundException && ex is not OperationCanceledException)
        {
            throw new IOException($"Не удалось извлечь ZIP {zipFilePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Рекурсивно ищет файл вверх по директории от заданной точки.
    /// </summary>
    /// <param name="start">Стартовая директория для поиска.</param>
    /// <param name="fileName">Имя файла для поиска.</param>
    /// <returns>Директория, содержащая файл, или null если файл не найден.</returns>
    private static DirectoryInfo? FindUpwards(DirectoryInfo start, string fileName)
    {
        var current = start;
        while (current.Exists)
        {
            var target = Path.Combine(current.FullName, fileName);
            if (File.Exists(target))
            {
                return current;
            }

            if (current.Parent is null)
            {
                return null;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Получает дату текущей версии ФИАС из файла version.txt.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Дата текущей версии или null, если файл не найден или некорректен.</returns>
    public async Task<DateTime?> GetCurrentVersionDateAsync(string projectRoot)
    {
        var versionFilePath = Path.Combine(GetExtractedDirectory(projectRoot), "version.txt");

        if (!File.Exists(versionFilePath))
        {
            _logger.LogDebug("Файл version.txt не найден: {Path}", versionFilePath);
            return null;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(versionFilePath);
            if (lines.Length == 0)
            {
                _logger.LogWarning("Файл version.txt пустой: {Path}", versionFilePath);
                return null;
            }

            // Парсим дату из первой строки
            var dateString = lines[0].Trim();
            if (!DateTime.TryParseExact(dateString, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                _logger.LogWarning("Не удалось распарсить дату из version.txt: {DateString}", dateString);
                return null;
            }

            _logger.LogDebug("Прочитана дата версии ФИАС: {Date}", parsedDate.ToString("yyyy-MM-dd"));
            return parsedDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при чтении файла version.txt: {Path}", versionFilePath);
            return null;
        }
    }
}