namespace FiasReportGenerator.Utils;

/// <summary>
/// Интерфейс для работы с файловой системой.
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Получает путь к корневой директории проекта.
    /// </summary>
    /// <returns>Путь к корневой директории проекта.</returns>
    string GetProjectRoot();

    /// <summary>
    /// Получает путь к директории с данными.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Путь к директории с данными.</returns>
    string GetDataDirectory(string projectRoot);

    /// <summary>
    /// Получает путь к директории для скачанных файлов.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Путь к директории для скачанных файлов.</returns>
    string GetDownloadsDirectory(string projectRoot);

    /// <summary>
    /// Получает путь к директории для распакованных файлов.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Путь к директории для распакованных файлов.</returns>
    string GetExtractedDirectory(string projectRoot);

    /// <summary>
    /// Получает путь к директории для отчетов.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Путь к директории для отчетов.</returns>
    string GetReportsDirectory(string projectRoot);

    /// <summary>
    /// Создает необходимые директории для данных.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    void EnsureDataDirectoriesCreated(string projectRoot);

    /// <summary>
    /// Распаковывает ZIP-архив в указанную директорию.
    /// </summary>
    /// <param name="zipFilePath">Путь к ZIP-файлу.</param>
    /// <param name="destinationDirectory">Директория для распаковки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task ExtractZipFileAsync(string zipFilePath, string destinationDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает дату текущей версии ФИАС из файла version.txt.
    /// </summary>
    /// <param name="projectRoot">Путь к корневой директории проекта.</param>
    /// <returns>Дата текущей версии или null, если файл не найден или некорректен.</returns>
    Task<DateTime?> GetCurrentVersionDateAsync(string projectRoot);
}