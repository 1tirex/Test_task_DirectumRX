using FiasReportGenerator.Models;
using FiasReportGenerator.Utils;
using FiasReportGenerator.Filters;
using Microsoft.Extensions.Logging;

namespace FiasReportGenerator.Services;

/// <summary>
/// Сервис для работы с адресными объектами.
/// </summary>
public class AddressObjectService
{
    private readonly IAddressObjectParser _parser;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AddressObjectService> _logger;

    public AddressObjectService(
        IAddressObjectParser parser,
        ICacheService cacheService,
        ILogger<AddressObjectService> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Парсит все файлы AS_ADDR_OBJ_*.xml, фильтрует действующие объекты и группирует по уровням.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами ФИАС.</param>
    /// <returns>Словарь сгруппированных адресных объектов по уровням.</returns>
    /// <exception cref="DirectoryNotFoundException">Если директория не найдена.</exception>
    /// <exception cref="InvalidOperationException">При ошибках парсинга XML.</exception>

    /// <summary>
    /// Загружает и фильтрует адресные объекты из файлов изменений по аналогии с Алексеем.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами ФИАС.</param>
    /// <param name="addressLevels">Словарь уровней адресных объектов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сгруппированная коллекция отфильтрованных адресных объектов по уровням.</returns>
    public async Task<Dictionary<AddressLevel, List<AddressObject>>> LoadAndFilterAddressObjectsAsync(string extractedDirectory, Dictionary<int, AddressLevel> addressLevels, CancellationToken cancellationToken = default)
    {
        var directory = new DirectoryInfo(extractedDirectory);
        if (!directory.Exists)
            throw new DirectoryNotFoundException($"Извлеченная директория не найдена: {extractedDirectory}");

        // Ищем только файлы AS_ADDR_OBJ_*.xml без суффиксов (_TYPES, _PARAMS, _DIVISION)
        var allAddressFiles = directory.GetFiles("AS_ADDR_OBJ_*.xml", SearchOption.AllDirectories);
        var addressFiles = allAddressFiles.Where(f =>
            !f.Name.Contains("_TYPES") &&
            !f.Name.Contains("_PARAMS") &&
            !f.Name.Contains("_DIVISION")).ToArray();

        if (!addressFiles.Any())
            throw new FileNotFoundException("В извлеченной директории не найдено объектных файлов");

        // Фильтрация по аналогии с Алексеем
        var excludedLevelNames = new HashSet<string> { "Дом", "Квартира", "Земельный участок", "Машино-место" };
        var addressLevelFilter = new AddressLevelFilter(excludedLevelNames);
        var filteredLevels = addressLevelFilter.Filter(addressLevels.Values).Select(x => x.Level).ToHashSet();

        // Обрабатываем файлы параллельно для лучшей производительности
        var tasks = addressFiles.Select(async (file, index) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var objectsLoaded = 0;
            var objectsFiltered = 0;
            var localFilteredObjects = new List<AddressObject>();

            await foreach (var addressObject in _parser.ParseAddressObjectsAsync(file.FullName, cancellationToken))
            {
                objectsLoaded++;

                // Применяем фильтры на лету
                if (!filteredLevels.Contains(addressObject.Level) && addressObject.IsActive)
                {
                    localFilteredObjects.Add(addressObject);
                    objectsFiltered++;
                }
            }

            return (objectsLoaded, localFilteredObjects);
        });

        var results = await Task.WhenAll(tasks);
        var totalObjectsLoaded = results.Sum(r => r.objectsLoaded);
        var filteredObjects = results.SelectMany(r => r.localFilteredObjects).ToList();

        // Группируем по AddressLevel объектам
        var dictionary = new Dictionary<AddressLevel, List<AddressObject>>();
        foreach (var address in filteredObjects)
        {
            if (!addressLevels.TryGetValue(address.Level, out var addressLevel)) continue;
            if (!dictionary.TryGetValue(addressLevel, out var value))
            {
                value = [];
                dictionary[addressLevel] = value;
            }

            value.Add(address);
        }

        // Сортируем объекты в каждой группе
        foreach (var (key, value) in dictionary)
        {
            value.Sort();
        }

        return dictionary;
    }
}