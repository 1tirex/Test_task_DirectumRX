using FiasReportGenerator.Models;
using FiasReportGenerator.Reports;

namespace FiasReportGenerator.Reports;

/// <summary>Генерирует CSV-отчет по адресным объектам.</summary>
public class CsvReportGenerator : IReportGenerator
{
    /// <summary>
    /// Генерирует CSV-отчет на основе данных об адресных объектах.
    /// </summary>
    /// <param name="data">Данные в формате Dictionary&lt;AddressLevel, List&lt;AddressObject&gt;&gt;.</param>
    /// <param name="outputPath">Директория для сохранения отчета.</param>
    /// <param name="heading">Заголовок отчета.</param>
    /// <returns>Полный путь к созданному CSV-файлу.</returns>
    /// <exception cref="ArgumentException">Если данные имеют некорректный формат.</exception>
    public string GenerateReport(object data, string outputPath, string heading)
    {
        if (data is not Dictionary<AddressLevel, List<AddressObject>> dictionary)
            throw new ArgumentException("Данные не в словаре<AddressLevel, список<AddressObject>>", nameof(data));

        Directory.CreateDirectory(outputPath);

        var fileName = "Отчет.csv";
        var filePath = Path.Combine(outputPath, fileName);

        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        // Заголовок отчета
        writer.WriteLine($"\"{heading}\"");

        // Пустая строка
        writer.WriteLine();

        foreach (var (addressLevel, addresses) in dictionary)
        {
            // Заголовок уровня
            writer.WriteLine($"\"Таблица {addressLevel.Level}: {addressLevel.Name}\"");

            // Статистика по уровню
            writer.WriteLine($"\"Количество объектов: {addresses.Count}\"");

            // Заголовки колонок
            writer.WriteLine("\"Тип объекта\";\"Наименование объекта\"");

            // Данные объектов
            foreach (var address in addresses)
            {
                // Экранируем кавычки в данных
                var escapedType = address.TypeName.Replace("\"", "\"\"");
                var escapedName = address.Name.Replace("\"", "\"\"");

                writer.WriteLine($"\"{escapedType}\";\"{escapedName}\"");
            }

            // Пустая строка между таблицами
            writer.WriteLine();
        }

        return filePath;
    }
}