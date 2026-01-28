using FiasReportGenerator.Models;

namespace FiasReportGenerator.Reports;

/// <summary>
/// Интерфейс для генерации отчетов.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Генерирует отчет на основе данных.
    /// </summary>
    /// <param name="data">Данные для отчета.</param>
    /// <param name="outputPath">Путь для сохранения отчета.</param>
    /// <param name="heading">Заголовок отчета.</param>
    /// <returns>Полный путь к созданному файлу отчета.</returns>
    string GenerateReport(object data, string outputPath, string heading);
}