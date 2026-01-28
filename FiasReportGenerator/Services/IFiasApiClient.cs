using FiasReportGenerator.Models;

namespace FiasReportGenerator.Services;

/// <summary>
/// Интерфейс для взаимодействия с ФИАС API.
/// </summary>
public interface IFiasApiClient
{
    /// <summary>
    /// Получает информацию о последнем пакете обновлений ФИАС.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Информация о последнем пакете обновлений.</returns>
    Task<DownloadFileInfo> GetLastDownloadFileInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Скачивает файл по указанному URL и сохраняет в заданную директорию.
    /// </summary>
    /// <param name="url">URL файла для скачивания.</param>
    /// <param name="destinationDirectory">Директория для сохранения файла.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Полный путь к скачанному файлу.</returns>
    Task<string> DownloadFileAsync(string url, string destinationDirectory, CancellationToken cancellationToken = default);
}

