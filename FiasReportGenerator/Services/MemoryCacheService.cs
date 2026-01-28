using FiasReportGenerator.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FiasReportGenerator.Services;

/// <summary>
/// Сервис кэширования с использованием памяти.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly AddressLevelService _addressLevelService;
    private readonly CacheSettings _cacheSettings;

    private const string AddressLevelsCacheKey = "AddressLevels";

    /// <summary>
    /// Инициализирует сервис кэширования с необходимыми зависимостями.
    /// </summary>
    /// <param name="memoryCache">Экземпляр кэша памяти.</param>
    /// <param name="logger">Логгер для записи событий.</param>
    /// <param name="addressLevelService">Сервис для загрузки уровней адресных объектов.</param>
    /// <param name="cacheSettings">Настройки кэширования.</param>
    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        AddressLevelService addressLevelService,
        CacheSettings cacheSettings)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _addressLevelService = addressLevelService ?? throw new ArgumentNullException(nameof(addressLevelService));
        _cacheSettings = cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));
    }

    /// <summary>
    /// Получает уровни адресных объектов из кэша или загружает их.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Словарь уровней адресных объектов.</returns>
    public async Task<Dictionary<int, AddressLevel>> GetAddressLevelsAsync(string extractedDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extractedDirectory))
            throw new ArgumentException("Извлеченная директория не может быть пустой или содержать пустое значение", nameof(extractedDirectory));

        var cacheKey = $"{AddressLevelsCacheKey}_{extractedDirectory.GetHashCode()}";

        return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _logger.LogInformation("Кэш уровней адресных объектов не найден, загрузка с диска");

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_cacheSettings.AddressLevelsExpirationHours);

            await Task.Yield();

            var addressLevels = _addressLevelService.LoadAddressLevels(extractedDirectory);

            _logger.LogInformation("В кэш загружено уровней адресных объектов: {Count}", addressLevels.Count);

            return addressLevels;
        }) ?? throw new InvalidOperationException("Failed to load address levels");
    }

    /// <summary>
    /// Очищает кэш уровней адресных объектов.
    /// </summary>
    public void ClearAddressLevelsCache()
    {
        // Очищаем все записи, содержащие AddressLevels в ключе
        var cacheKeys = _memoryCache.GetType()
            .GetProperty("Keys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(_memoryCache) as IEnumerable<object>;

        if (cacheKeys != null)
        {
            foreach (var key in cacheKeys)
            {
                if (key.ToString()?.Contains(AddressLevelsCacheKey) == true)
                {
                    _memoryCache.Remove(key);
                }
            }
        }

        _logger.LogInformation("Кэш уровней адресных объектов очищен");
    }
}
