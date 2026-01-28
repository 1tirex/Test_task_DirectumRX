namespace FiasReportGenerator.Models;

/// <summary>
/// Настройки для работы с ФИАС API.
/// </summary>
public class FiasSettings
{
    /// <summary>
    /// Базовый URL API ФИАС.
    /// </summary>
    public required string ApiBaseUrl { get; set; }

    /// <summary>
    /// Путь для получения информации о последнем пакете обновлений.
    /// </summary>
    public required string LastDownloadFileInfoPath { get; set; }

    /// <summary>
    /// Таймаут HTTP запросов в минутах.
    /// </summary>
    public int HttpTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Максимальное количество попыток повторных запросов.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Базовая задержка между повторными попытками в секундах.
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;
}

