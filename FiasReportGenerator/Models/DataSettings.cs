namespace FiasReportGenerator.Models;

/// <summary>
/// Настройки для работы с файловой системой.
/// </summary>
public class DataSettings
{
    /// <summary>
    /// Имя корневой директории для данных.
    /// </summary>
    public required string DirectoryName { get; set; }

    /// <summary>
    /// Имя директории для скачанных файлов.
    /// </summary>
    public required string DownloadsDirectoryName { get; set; }

    /// <summary>
    /// Имя директории для распакованных файлов.
    /// </summary>
    public required string ExtractedDirectoryName { get; set; }

    /// <summary>
    /// Имя директории для сгенерированных отчетов.
    /// </summary>
    public required string ReportsDirectoryName { get; set; }
}