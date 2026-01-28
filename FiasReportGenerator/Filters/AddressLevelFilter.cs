using FiasReportGenerator.Filters;
using FiasReportGenerator.Models;

namespace FiasReportGenerator.Filters;

/// <summary>
/// Фильтр для отбора уровней адресных объектов по именам.
/// </summary>
public class AddressLevelFilter(HashSet<string> excludedLevelNames) : IFilter<AddressLevel>
{
    /// <summary>
    /// Фильтрует уровни адресных объектов по именам, оставляя только указанные.
    /// </summary>
    /// <param name="items">Коллекция уровней адресных объектов для фильтрации.</param>
    /// <returns>Коллекция уровней, имена которых содержатся в списке исключенных имен.</returns>
    public IEnumerable<AddressLevel> Filter(IEnumerable<AddressLevel> items)
    {
        return items.Where(level => excludedLevelNames.Contains(level.Name));
    }
}