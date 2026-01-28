using FiasReportGenerator.Models;
using FiasReportGenerator.Reports;

namespace FiasReportGenerator.Reports;

/// <summary>Генерирует TXT-отчет по адресным объектам.</summary>
public class TxtReportGenerator : IReportGenerator
{
    /// <summary>
    /// Генерирует текстовый отчет на основе данных об адресных объектах.
    /// </summary>
    /// <param name="data">Данные в формате Dictionary&lt;AddressLevel, List&lt;AddressObject&gt;&gt;.</param>
    /// <param name="outputPath">Директория для сохранения отчета.</param>
    /// <param name="heading">Заголовок отчета.</param>
    /// <returns>Полный путь к созданному текстовому файлу.</returns>
    /// <exception cref="ArgumentException">Если данные имеют некорректный формат.</exception>
    public string GenerateReport(object data, string outputPath, string heading)
    {
        if (data is not Dictionary<AddressLevel, List<AddressObject>> dictionary)
            throw new ArgumentException("Данные не в словаре <AddressLevel, список<AddressObject>>", nameof(data));

        Directory.CreateDirectory(outputPath);

        var fileName = "Отчет.txt";
        var filePath = Path.Combine(outputPath, fileName);

        using var writer = new StreamWriter(filePath);

        writer.WriteLine(heading);

        foreach (var (addressLevel, addresses) in dictionary)
        {
            writer.WriteLine($"   Address Level: {addressLevel.Name}");

            foreach (var address in addresses)
            {
                writer.WriteLine($"      ID: {address.Id}, Type: {address.TypeName}, Name: {address.Name}");
            }
        }

        return filePath;
    }
}