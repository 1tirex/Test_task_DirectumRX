
using FiasReportGenerator.Models;
using FiasReportGenerator.Services;
using FiasReportGenerator.Utils;
using FiasReportGenerator.Reports;
using FiasReportGenerator.Validators;
using FiasReportGenerator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
 
using FluentValidation;

public class Program
{
    /// <summary>
    /// Точка входа в приложение. Настраивает хост и запускает основную логику приложения.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            await host.StartAsync();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Запуск");

            await RunApplicationAsync(host.Services, host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Крит ошибка: {Message}", ex.Message);
            Environment.ExitCode = 1;
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    /// Выполняет основную логику приложения: скачивание, распаковка и обработка данных ФИАС.
    /// </summary>
    /// <param name="services">Провайдер сервисов для получения зависимостей.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private static async Task RunApplicationAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var fileManager = services.GetRequiredService<IFileManager>();
        var fiasClient = services.GetRequiredService<IFiasApiClient>();

        try
        {
            var projectRoot = fileManager.GetProjectRoot();
            fileManager.EnsureDataDirectoriesCreated(projectRoot);

            var downloadInfo = await fiasClient.GetLastDownloadFileInfoAsync();

            logger.LogInformation("ФИАС: версия {VersionId}, дата {Date}", 
                downloadInfo.VersionId, downloadInfo.Date.ToString("yyyy-MM-dd"));

            // Проверяем актуальность локальных данных ФИАС
            var extractedDir = fileManager.GetExtractedDirectory(projectRoot);
            var currentVersionDate = await fileManager.GetCurrentVersionDateAsync(projectRoot);
            DateTime? reportVersionDate = currentVersionDate; // Дата для отчета обновляется после скачивания

            if (currentVersionDate == null || currentVersionDate < downloadInfo.Date)
            {
                if (currentVersionDate == null)
                {
                    logger.LogInformation("Локальные данные ФИАС отсутствуют");
                }
                else
                {
                    logger.LogInformation("Локальные данные ФИАС устарели. Текущая версия: {CurrentDate}, доступна: {LatestDate}",
                        currentVersionDate.Value.ToString("yyyy-MM-dd"), downloadInfo.Date.ToString("yyyy-MM-dd"));
                }

                logger.LogInformation("Скачивание пакета ФИАС: {Url}", downloadInfo.GarXMLDeltaURL);

                var downloadedFilePath = await fiasClient.DownloadFileAsync(
                        downloadInfo.GarXMLDeltaURL,
                        fileManager.GetDownloadsDirectory(projectRoot)
                    );

                logger.LogInformation("Пакет скачан: {FilePath}", downloadedFilePath);

                logger.LogInformation("Распаковка в {Destination}", extractedDir);

                await fileManager.ExtractZipFileAsync(
                        downloadedFilePath,
                        extractedDir
                    );

                logger.LogInformation("Данные успешно распакованы");

                // обновиляем версию
                reportVersionDate = await fileManager.GetCurrentVersionDateAsync(projectRoot);
            }
            else
            {
                logger.LogInformation("Локальные данные ФИАС актуальны (версия от {Date})",
                    currentVersionDate.Value.ToString("yyyy-MM-dd"));
            }

            logger.LogInformation("Загрузка объектов из AS_OBJECT_LEVELS.xml");

            var cacheService = services.GetRequiredService<ICacheService>();
            var addressLevels = await cacheService.GetAddressLevelsAsync(extractedDir, cancellationToken);

            logger.LogInformation("Загружено объектов: {Count}", addressLevels.Count);

            logger.LogInformation("Загрузка и фильтрация объектов из файлов");

            var addressObjectService = services.GetRequiredService<AddressObjectService>();
            var filteredAddressObjects = await addressObjectService.LoadAndFilterAddressObjectsAsync(
                extractedDir,
                addressLevels,
                cancellationToken);

            logger.LogInformation("Фильтрация адресных объектов завершена");

            var totalObjects = filteredAddressObjects.Sum(g => g.Value.Count);
            logger.LogInformation("Отфильтрованных объектов: {Count}", totalObjects);

            // Генерация отчетов
            logger.LogInformation("Формирование отчетов");

            // Определяем дату для заголовка отчета из файла версии или из API
            var reportDate = reportVersionDate ?? downloadInfo.Date;
            var heading = $"Отчет по добавленным объектам за {reportDate:dd.MM.yyyy}";

            // Генерация TXT отчета
            var txtReportGenerator = services.GetRequiredService<TxtReportGenerator>();
            var txtReportFilePath = txtReportGenerator.GenerateReport(
                filteredAddressObjects,
                fileManager.GetReportsDirectory(projectRoot),
                heading);
            logger.LogInformation("TXT-отчет сохранен: {FilePath}", txtReportFilePath);

            // Генерация CSV отчета для возможных дальнейших работ
            var csvReportGenerator = services.GetRequiredService<CsvReportGenerator>();
            var csvReportFilePath = csvReportGenerator.GenerateReport(
                filteredAddressObjects,
                fileManager.GetReportsDirectory(projectRoot),
                heading);
            logger.LogInformation("CSV-отчет сохранен: {FilePath}", csvReportFilePath);

            logger.LogInformation("Обработка изменений ФИАС успешно завершена");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Операция была отменена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Необработанная ошибка приложения: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Создает и настраивает хост приложения с зависимостями и конфигурацией.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Настроенный хост приложения.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Регистрация конфигурации
                services.Configure<FiasSettings>(context.Configuration.GetSection("Fias"));
                services.Configure<DataSettings>(context.Configuration.GetSection("Data"));
                services.Configure<CacheSettings>(context.Configuration.GetSection("Cache"));

                // Регистрация опций как сервисов для DI
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FiasSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>().Value);

                // Регистрация валидаторов
                services.AddValidatorsFromAssemblyContaining<DownloadFileInfoValidator>();

                // Регистрация дополнительных валидаторов
                services.AddScoped<IValidator<string>, UrlValidator>();
                services.AddScoped<IValidator<string>, FilePathValidator>(sp => new FilePathValidator());

                // Регистрация HttpClient
                services.AddHttpClient<IFiasApiClient, FiasApiClient>((serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FiasSettings>>().Value;
                    client.BaseAddress = new Uri(settings.ApiBaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(settings.HttpTimeoutMinutes);
                });

                // Регистрация сервисов
                services.AddSingleton<IFileManager, FileManager>();
                services.AddScoped<AddressLevelService>();
                services.AddScoped<IAddressObjectParser, StreamingAddressObjectParser>();
                services.AddScoped<ICacheService, MemoryCacheService>();
                services.AddScoped<AddressObjectService>();

                // Регистрация IRateLimiter
                services.AddSingleton<IRateLimiter>(sp =>
                {
                    var cache = sp.GetRequiredService<IMemoryCache>();
                    var logger = sp.GetRequiredService<ILogger<InMemoryRateLimiter>>();
                    return new InMemoryRateLimiter(cache, logger, maxRequestsPerMinute: 30);
                });

                // Регистрация генераторов отчетов
                services.AddScoped<TxtReportGenerator>();
                services.AddScoped<CsvReportGenerator>();

                // Регистрация кэширования
                services.AddMemoryCache();
            });
}