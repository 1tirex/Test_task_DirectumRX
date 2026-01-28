using Newtonsoft.Json;

namespace FiasReportGenerator.Models;

/// <summary>
/// Модель ответа метода GetLastDownloadFileInfo ФИАС API.
/// Содержит информацию о последнем пакете обновлений.
/// </summary>
public class DownloadFileInfo
{
    /// <summary>
    /// Идентификатор версии пакета обновлений.
    /// </summary>
    public required string VersionId { get; set; }

    /// <summary>
    /// Текстовая версия пакета обновлений.
    /// </summary>
    public string? TextVersion { get; set; }

    /// <summary>
    /// Дата выпуска пакета обновлений.
    /// </summary>
    public required DateTime Date { get; set; }

    /// <summary>
    /// URL для скачивания дельта-пакета обновлений (XML).
    /// </summary>
    public required string GarXMLDeltaURL { get; set; }

    /// <summary>
    /// URL для скачивания полного пакета (XML).
    /// </summary>
    public required string GarXMLFullURL { get; set; }

    public override string ToString()
    {
        return $"VersionId: {VersionId}, Date: {Date:yyyy-MM-dd HH:mm:ss}, DeltaURL: {GarXMLDeltaURL}";
    }
}

/// <summary>
/// Ответ API ФИАС для получения информации о пакете обновлений.
/// </summary>
public class DownloadFileInfoResponse
{
    /// <summary>
    /// Идентификатор версии.
    /// </summary>
    [JsonProperty("VersionId")]
    public string VersionId { get; set; } = null!;

    /// <summary>
    /// Текстовое описание версии.
    /// </summary>
    [JsonProperty("TextVersion")]
    public string? TextVersion { get; set; }

    /// <summary>
    /// Дата пакета обновлений в формате dd.MM.yyyy.
    /// </summary>
    [JsonProperty("Date")]
    public string Date { get; set; } = null!;

    /// <summary>
    /// URL для скачивания дельта пакета.
    /// </summary>
    [JsonProperty("GarXMLDeltaURL")]
    public string GarXMLDeltaURL { get; set; } = null!;

    /// <summary>
    /// URL для скачивания полного пакета.
    /// </summary>
    [JsonProperty("GarXMLFullURL")]
    public string GarXMLFullURL { get; set; } = null!;
}