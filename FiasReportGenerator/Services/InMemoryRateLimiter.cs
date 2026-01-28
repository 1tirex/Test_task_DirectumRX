using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FiasReportGenerator.Services;

/// <summary>
/// Простой in-memory rate limiter с использованием скользящего окна.
/// </summary>
public class InMemoryRateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryRateLimiter> _logger;
    private readonly int _maxRequestsPerMinute;
    private readonly TimeSpan _windowSize;

    /// <summary>
    /// Инициализирует rate limiter с заданными параметрами.
    /// </summary>
    /// <param name="cache">Кэш для хранения информации о запросах.</param>
    /// <param name="logger">Логгер для записи событий.</param>
    /// <param name="maxRequestsPerMinute">Максимальное количество запросов в минуту.</param>
    /// <param name="windowSize">Размер скользящего окна (по умолчанию 1 минута).</param>
    public InMemoryRateLimiter(
        IMemoryCache cache,
        ILogger<InMemoryRateLimiter> logger,
        int maxRequestsPerMinute = 60,
        TimeSpan? windowSize = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRequestsPerMinute = maxRequestsPerMinute;
        _windowSize = windowSize ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Проверяет, можно ли выполнить запрос.
    /// </summary>
    public async Task<bool> IsRequestAllowedAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"RateLimit_{key}";
        var now = DateTimeOffset.UtcNow;

        var requestTimes = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _windowSize;
            return Task.FromResult(new List<DateTimeOffset>());
        }) ?? new List<DateTimeOffset>();

        // Удаляем просроченные запросы
        requestTimes.RemoveAll(time => now - time > _windowSize);

        if (requestTimes.Count >= _maxRequestsPerMinute)
        {
            _logger.LogWarning("Превышен лимит запросов для ключа {Key}: {Count} из {Max} за интервал",
                key, requestTimes.Count, _maxRequestsPerMinute);
            return false;
        }

        // Добавляем текущий запрос
        requestTimes.Add(now);

        // Обновляем кэш
        _cache.Set(cacheKey, requestTimes, _windowSize);

        return true;
    }

    /// <summary>
    /// Получает время ожидания до следующего разрешенного запроса.
    /// </summary>
    public async Task<int> GetWaitTimeSecondsAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"RateLimit_{key}";
        var now = DateTimeOffset.UtcNow;

        var requestTimes = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _windowSize;
            return Task.FromResult(new List<DateTimeOffset>());
        }) ?? new List<DateTimeOffset>();

        // Удаляем просроченные запросы
        requestTimes.RemoveAll(time => now - time > _windowSize);

        if (requestTimes.Count < _maxRequestsPerMinute)
            return 0;

        // Вычисляем время ожидания до истечения срока самого старого запроса
        var oldestRequest = requestTimes.Min();
        var waitTime = (_windowSize - (now - oldestRequest)).TotalSeconds;

        return Math.Max(0, (int)Math.Ceiling(waitTime));
    }
}
