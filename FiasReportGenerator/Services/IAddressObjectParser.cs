using FiasReportGenerator.Models;

namespace FiasReportGenerator.Services;

/// <summary>
/// Интерфейс для парсинга адресных объектов из XML.
/// </summary>
public interface IAddressObjectParser
{
    /// <summary>
    /// Парсит адресные объекты из XML файла.
    /// </summary>
    /// <param name="filePath">Путь к XML файлу.</param>
    /// <returns>Коллекция адресных объектов.</returns>
    IAsyncEnumerable<AddressObject> ParseAddressObjectsAsync(string filePath, CancellationToken cancellationToken = default);
}
