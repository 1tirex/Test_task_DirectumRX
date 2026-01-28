using FiasReportGenerator.Filters;
using FiasReportGenerator.Models;

namespace FiasReportGenerator.Filters;

/// <summary>
/// Фильтр для отбора только активных адресных объектов.
/// </summary>
public class AddressFilter : IFilter<AddressObject>
{
    /// <summary>
    /// Фильтрует коллекцию адресных объектов, оставляя только активные.
    /// </summary>
    /// <param name="items">Коллекция адресных объектов для фильтрации.</param>
    /// <returns>Коллекция только активных адресных объектов.</returns>
    public IEnumerable<AddressObject> Filter(IEnumerable<AddressObject> items)
    {
        return items.Where(address => address.IsActive);
    }
}