namespace FiasReportGenerator.Filters;

/// <summary>
/// Интерфейс для фильтрации коллекций объектов.
/// </summary>
/// <typeparam name="T">Тип объектов для фильтрации.</typeparam>
public interface IFilter<T>
{
    /// <summary>
    /// Фильтрует коллекцию объектов.
    /// </summary>
    /// <param name="items">Коллекция объектов для фильтрации.</param>
    /// <returns>Отфильтрованная коллекция объектов.</returns>
    IEnumerable<T> Filter(IEnumerable<T> items);
}