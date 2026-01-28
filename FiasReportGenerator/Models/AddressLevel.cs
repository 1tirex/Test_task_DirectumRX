namespace FiasReportGenerator.Models;

/// <summary>
/// Модель уровня адресного объекта из AS_OBJECT_LEVELS.
/// </summary>
public class AddressLevel
{
    /// <summary>
    /// Уникальный идентификатор уровня.
    /// </summary>
    public required int Level { get; set; }

    /// <summary>
    /// Наименование уровня (например, "Регион", "Город", "Улица").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Краткое наименование уровня.
    /// </summary>
    public required string ShortName { get; set; }

    /// <summary>
    /// Дата начала действия записи.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Дата окончания действия записи.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Дата обновления записи.
    /// </summary>
    public DateTime UpdateDate { get; set; }

    /// <summary>
    /// Признак актуальности записи.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Возвращает строковое представление уровня адресного объекта.
    /// </summary>
    /// <returns>Строка в формате "Level {Level}: {Name} ({ShortName})".</returns>
    public override string ToString()
    {
        return $"Level {Level}: {Name} ({ShortName})";
    }
}