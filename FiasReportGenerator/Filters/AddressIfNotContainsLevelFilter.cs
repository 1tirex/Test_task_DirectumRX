using FiasReportGenerator.Filters;
using FiasReportGenerator.Models;

namespace FiasReportGenerator.Filters;

/// <summary>
/// Фильтр для исключения адресных объектов из определенных уровней.
/// </summary>
public class AddressIfNotContainsLevelFilter(HashSet<int> excludedLevels) : IFilter<AddressObject>
{
    /// <summary>
    /// Фильтрует адресные объекты, исключая объекты из указанных уровней.
    /// </summary>
    /// <param name="addresses">Коллекция адресных объектов для фильтрации.</param>
    /// <returns>Коллекция адресных объектов без объектов из исключенных уровней.</returns>
    public IEnumerable<AddressObject> Filter(IEnumerable<AddressObject> addresses)
    {
        return addresses.Where(address => !excludedLevels.Contains(address.Level));
    }
}