namespace FiasReportGenerator.Services;

/// <summary>
/// Интерфейс для ограничения частоты запросов.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Проверяет, можно ли выполнить запрос.
    /// </summary>
    /// <param name="key">Ключ для идентификации источника запросов.</param>
    /// <returns>true если запрос разрешен, false если превышен лимит.</returns>
    Task<bool> IsRequestAllowedAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает время ожидания до следующего разрешенного запроса.
    /// </summary>
    /// <param name="key">Ключ для идентификации источника запросов.</param>
    /// <returns>Время ожидания в секундах.</returns>
    Task<int> GetWaitTimeSecondsAsync(string key, CancellationToken cancellationToken = default);
}
