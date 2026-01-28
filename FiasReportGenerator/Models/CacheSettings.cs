namespace FiasReportGenerator.Models;

/// <summary>
/// Настройки кэширования.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Время жизни кэша уровней адресных объектов в часах.
    /// </summary>
    public int AddressLevelsExpirationHours { get; set; } = 1;

    /// <summary>
    /// Максимальный размер кэша в памяти в мегабайтах.
    /// </summary>
    public long MemoryCacheSizeLimit { get; set; } = 1024;
}