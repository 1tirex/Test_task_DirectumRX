namespace FiasReportGenerator.Models;

/// <summary>
/// Данные для генерации отчета об изменениях в адресах ФИАС.
/// </summary>
public class ReportData
{
    /// <summary>
    /// Дата изменений из пакета обновлений.
    /// </summary>
    public required DateTime ChangeDate { get; set; }

    /// <summary>
    /// Сгруппированные адресные объекты по уровням.
    /// </summary>
    public required Dictionary<int, List<AddressObject>> GroupedAddressObjects { get; set; }

    /// <summary>
    /// Уровни адресных объектов для отображения названий.
    /// </summary>
    public required Dictionary<int, AddressLevel> AddressLevels { get; set; }

    /// <summary>
    /// Получить количество объектов по уровням для статистики.
    /// </summary>
    public Dictionary<int, int> GetObjectsCountByLevel()
    {
        return GroupedAddressObjects.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count
        );
    }

    /// <summary>
    /// Получить общее количество объектов.
    /// </summary>
    public int GetTotalObjectsCount()
    {
        return GroupedAddressObjects.Sum(kvp => kvp.Value.Count);
    }
}