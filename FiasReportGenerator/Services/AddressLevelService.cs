using System.Xml.Linq;
using FiasReportGenerator.Models;
using FiasReportGenerator.Utils;

namespace FiasReportGenerator.Services;

/// <summary>Загружает уровни адресных объектов из AS_OBJECT_LEVELS.</summary>
public class AddressLevelService
{
    /// <summary>
    /// Читает уровни адресных объектов из распакованных файлов ФИАС.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами ФИАС.</param>
    /// <returns>Словарь уровней адресных объектов с ключами по идентификаторам уровней.</returns>
    /// <exception cref="DirectoryNotFoundException">Если директория не найдена.</exception>
    /// <exception cref="FileNotFoundException">Если файл AS_OBJECT_LEVELS.xml не найден.</exception>
    /// <exception cref="InvalidOperationException">При ошибках парсинга XML.</exception>
    public Dictionary<int, AddressLevel> LoadAddressLevels(string extractedDirectory)
    {
        var levelsFilePath = FindObjectLevelsFile(extractedDirectory);

        try
        {
            var doc = XDocument.Load(levelsFilePath);
            var levels = new Dictionary<int, AddressLevel>();

            var objectLevels = doc.Descendants("OBJECTLEVEL");

            foreach (var levelElement in objectLevels)
            {
                var level = ParseAddressLevel(levelElement);
                levels[level.Level] = level;
            }

            return levels;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Ошибка чтение: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Находит файл AS_OBJECT_LEVELS.xml в директории с распакованными файлами.
    /// </summary>
    /// <param name="extractedDirectory">Директория с распакованными файлами ФИАС.</param>
    /// <returns>Полный путь к файлу AS_OBJECT_LEVELS.xml.</returns>
    /// <exception cref="DirectoryNotFoundException">Если директория не найдена.</exception>
    /// <exception cref="FileNotFoundException">Если файл AS_OBJECT_LEVELS.xml не найден.</exception>
    private static string FindObjectLevelsFile(string extractedDirectory)
    {
        var directory = new DirectoryInfo(extractedDirectory);
        if (!directory.Exists)
            throw new DirectoryNotFoundException($"Извлеченная директория не найдена: {extractedDirectory}");

        // Ищем файл AS_OBJECT_LEVELS*.xml в директории
        var levelsFile = directory.GetFiles("AS_OBJECT_LEVELS*.xml", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (levelsFile == null)
            throw new FileNotFoundException("AS_OBJECT_LEVELS.xml путь не найдет", Path.Combine(extractedDirectory, "AS_OBJECT_LEVELS.xml"));

        return levelsFile.FullName;
    }

    /// <summary>
    /// Парсит XML-элемент уровня адресного объекта в объект AddressLevel.
    /// </summary>
    /// <param name="levelElement">XML-элемент для парсинга.</param>
    /// <returns>Объект уровня адресного объекта.</returns>
    /// <exception cref="InvalidOperationException">При отсутствии обязательных атрибутов или ошибках парсинга.</exception>
    private static AddressLevel ParseAddressLevel(XElement levelElement)
    {
        try
        {
            var level = int.Parse(levelElement.Attribute("LEVEL")?.Value ?? throw new InvalidOperationException("LEVEL attribute missing"));
            var name = levelElement.Attribute("NAME")?.Value ?? throw new InvalidOperationException("NAME attribute missing");

            // SHORTNAME может отсутствовать, используем NAME
            var shortName = levelElement.Attribute("SHORTNAME")?.Value ?? name;

            var startDateStr = levelElement.Attribute("STARTDATE")?.Value ?? throw new InvalidOperationException("STARTDATE атрибут отсутствует");
            var startDate = DateTime.Parse(startDateStr);

            var endDateStr = levelElement.Attribute("ENDDATE")?.Value;
            DateTime? endDate = null;
            if (!string.IsNullOrWhiteSpace(endDateStr))
                endDate = DateTime.Parse(endDateStr);

            var updateDateStr = levelElement.Attribute("UPDATEDATE")?.Value ?? throw new InvalidOperationException("UPDATEDATE атрибут отсутствует");
            var updateDate = DateTime.Parse(updateDateStr);

            var isActiveStr = levelElement.Attribute("ISACTIVE")?.Value ?? "false";
            var isActive = isActiveStr.ToLower() == "true";

            return new AddressLevel
            {
                Level = level,
                Name = name,
                ShortName = shortName,
                StartDate = startDate,
                EndDate = endDate,
                UpdateDate = updateDate,
                IsActive = isActive
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Не удалось разобрать адрес: {ex.Message}", ex);
        }
    }
}