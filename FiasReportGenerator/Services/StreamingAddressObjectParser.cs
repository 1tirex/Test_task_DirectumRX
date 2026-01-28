using FiasReportGenerator.Models;
using Microsoft.Extensions.Logging;

namespace FiasReportGenerator.Services;

/// <summary>
/// Streaming XML парсер адресных объектов для высокой производительности.
/// </summary>
public class StreamingAddressObjectParser : IAddressObjectParser
{
    private readonly ILogger<StreamingAddressObjectParser> _logger;

    /// <summary>
    /// Инициализирует парсер с логгером.
    /// </summary>
    /// <param name="logger">Логгер для записи событий парсинга.</param>
    public StreamingAddressObjectParser(ILogger<StreamingAddressObjectParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Парсит адресные объекты из XML файла в streaming режиме.
    /// </summary>
    /// <param name="filePath">Путь к XML файлу.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Асинхронная последовательность адресных объектов.</returns>
    public async IAsyncEnumerable<AddressObject> ParseAddressObjectsAsync(string filePath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("XML файл не найден", filePath);


        var settings = new System.Xml.XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        using var stream = File.OpenRead(filePath);
        using var reader = System.Xml.XmlReader.Create(stream, settings);

        try
        {
            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "OBJECT")
                {
                    var addressObject = await ParseAddressObjectAsync(reader, cancellationToken);
                    if (addressObject != null)
                    {
                        yield return addressObject;
                    }
                }
            }
        }
        finally { }
    }

    /// <summary>
    /// Парсит отдельный адресный объект из XML reader.
    /// </summary>
    /// <param name="reader">XML reader, позиционированный на элементе OBJECT.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объект адресного объекта или null при ошибках парсинга.</returns>
    private Task<AddressObject?> ParseAddressObjectAsync(System.Xml.XmlReader reader, CancellationToken cancellationToken)
    {
        try
        {
            // Читаем атрибуты объекта
            var id = int.Parse(reader.GetAttribute("ID") ?? "0");
            var objectGuid = reader.GetAttribute("OBJECTGUID");
            var operTypeId = int.Parse(reader.GetAttribute("OPERTYPEID") ?? "0");
            var level = int.Parse(reader.GetAttribute("LEVEL") ?? "0");
            var typeName = reader.GetAttribute("TYPENAME") ?? string.Empty;
            var name = reader.GetAttribute("NAME") ?? string.Empty;
            var parentObjId = reader.GetAttribute("PARENTOBJID");
            var isActive = reader.GetAttribute("ISACTIVE") == "1";
            var startDateStr = reader.GetAttribute("STARTDATE");
            var endDateStr = reader.GetAttribute("ENDDATE");
            var updateDateStr = reader.GetAttribute("UPDATEDATE");

            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(objectGuid) || string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Пропуск объекта с ID {Id}: отсутствуют обязательные поля", id);
                return Task.FromResult<AddressObject?>(null);
            }

            if (!Guid.TryParse(objectGuid, out var objectId))
            {
                _logger.LogWarning("Пропуск объекта с ID {Id}: некорректный формат OBJECTGUID", id);
                return Task.FromResult<AddressObject?>(null);
            }

            Guid? parentObjectId = null;
            if (!string.IsNullOrWhiteSpace(parentObjId) && Guid.TryParse(parentObjId, out var parentId))
            {
                parentObjectId = parentId;
            }

            // Парсинг дат
            if (!DateTime.TryParse(startDateStr, out var startDate))
            {
                startDate = DateTime.MinValue;
            }

            DateTime? endDate = null;
            if (!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out var endDateValue))
            {
                endDate = endDateValue;
            }

            if (!DateTime.TryParse(updateDateStr, out var updateDate))
            {
                updateDate = DateTime.MinValue;
            }

            return Task.FromResult(new AddressObject
            {
                Id = id,
                ObjectId = objectId,
                OperTypeId = operTypeId,
                ParentObjectId = parentObjectId,
                Level = level,
                ObjectTypeName = typeName,
                Name = name,
                TypeName = typeName,
                IsActive = isActive,
                StartDate = startDate,
                EndDate = endDate,
                UpdateDate = updateDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при разборе адресного объекта: {Message}", ex.Message);
            return Task.FromResult<AddressObject?>(null);
        }
    }
}