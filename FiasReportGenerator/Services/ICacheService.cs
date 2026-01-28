using FiasReportGenerator.Models;

namespace FiasReportGenerator.Services;

/// <summary>
/// Интерфейс для кэширования данных.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает уровни адресных объектов из кэша или загружает их.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Словарь уровней адресных объектов.</returns>
    Task<Dictionary<int, AddressLevel>> GetAddressLevelsAsync(string extractedDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Очищает кэш уровней адресных объектов.
    /// </summary>
    void ClearAddressLevelsCache();
}
