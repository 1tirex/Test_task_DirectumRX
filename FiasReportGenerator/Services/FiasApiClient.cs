using FiasReportGenerator.Models;
using FiasReportGenerator.Validators;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Newtonsoft.Json;

namespace FiasReportGenerator.Services;

/// <summary>
/// HTTP-клиент для взаимодействия с ФИАС API.
/// </summary>
public class FiasApiClient : IFiasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FiasSettings _settings;
    private readonly ILogger<FiasApiClient> _logger;

    private readonly IValidator<DownloadFileInfo> _downloadFileInfoValidator;
    private readonly IValidator<string> _urlValidator;
    private readonly IValidator<string> _filePathValidator;
    private readonly IRateLimiter _rateLimiter;

    public FiasApiClient(
        HttpClient httpClient,
        FiasSettings settings,
        ILogger<FiasApiClient> logger,
        IValidator<DownloadFileInfo> downloadFileInfoValidator,
        IValidator<string> urlValidator,
        IValidator<string> filePathValidator,
        IRateLimiter rateLimiter)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _downloadFileInfoValidator = downloadFileInfoValidator ?? throw new ArgumentNullException(nameof(downloadFileInfoValidator));
        _urlValidator = urlValidator ?? throw new ArgumentNullException(nameof(urlValidator));
        _filePathValidator = filePathValidator ?? throw new ArgumentNullException(nameof(filePathValidator));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    /// <summary>
    /// Скачивает файл по указанному URL и сохраняет в заданную директорию.
    /// </summary>
    /// <param name="url">URL файла для скачивания.</param>
    /// <param name="destinationDirectory">Директория для сохранения файла.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Полный путь к скачанному файлу.</returns>
    /// <exception cref="ArgumentException">При некорректных параметрах.</exception>
    /// <exception cref="HttpRequestException">При ошибках HTTP-запроса.</exception>
    /// <exception cref="IOException">При ошибках файловой системы.</exception>
    /// <exception cref="OperationCanceledException">При отмене операции.</exception>
    public async Task<string> DownloadFileAsync(string url, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        // Проверяем входные параметры
        var urlValidation = await _urlValidator.ValidateAsync(url, cancellationToken);
        if (!urlValidation.IsValid)
        {
            var error = string.Join(", ", urlValidation.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException($"Неверный URL-адрес: {error}", nameof(url));
        }

        var pathValidation = await _filePathValidator.ValidateAsync(destinationDirectory, cancellationToken);
        if (!pathValidation.IsValid)
        {
            var error = string.Join(", ", pathValidation.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException($"Неверный каталог: {error}", nameof(destinationDirectory));
        }

        try
        {
            _logger.LogInformation("Начато скачивание файла ФИАС: {Url}", url);

            Directory.CreateDirectory(destinationDirectory);

            // Извлекаем имя файла из URL
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"fias_download_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            var filePath = Path.Combine(destinationDirectory, fileName);

            // Детальный путь сохраняем только в сообщении об успешном завершении
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

            await contentStream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Файл ФИАС успешно скачан: {FilePath}", filePath);
            return filePath;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка при скачивании файла ФИАС по адресу {Url}: {Message}", url, ex.Message);
            throw new HttpRequestException($"Failed to download file from {url}: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not HttpRequestException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Неожиданная ошибка при скачивании файла ФИАС по адресу {Url}: {Message}", url, ex.Message);
            throw new IOException($"Error downloading file from {url}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Получает информацию о последнем пакете обновлений ФИАС.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Информация о последнем пакете обновлений.</returns>
    /// <exception cref="HttpRequestException">При ошибках HTTP-запроса.</exception>
    /// <exception cref="InvalidOperationException">При ошибках парсинга или валидации данных.</exception>
    /// <exception cref="OperationCanceledException">При отмене операции.</exception>
    public async Task<DownloadFileInfo> GetLastDownloadFileInfoAsync(CancellationToken cancellationToken = default)
    {
        // Проверяем лимит запросов
        const string rateLimitKey = "fias_api";
        if (!await _rateLimiter.IsRequestAllowedAsync(rateLimitKey, cancellationToken))
        {
            var waitTime = await _rateLimiter.GetWaitTimeSecondsAsync(rateLimitKey, cancellationToken);
            _logger.LogWarning("Превышен лимит запросов к ФИАС API. Повторите попытку через {WaitTime} секунд", waitTime);
            throw new InvalidOperationException($"Превышен лимит запросов. Повторите попытку через {waitTime} секунд.");
        }

        try
        {
            var requestUrl = _settings.ApiBaseUrl + _settings.LastDownloadFileInfoPath;
            _logger.LogInformation("Запрос информации о последнем пакете ФИАС: {Url}", requestUrl);

            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return await ParseDownloadFileInfoAsync(jsonContent, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка HTTP при запросе информации о пакете ФИАС: {Message}", ex.Message);
            throw new HttpRequestException($"Ошибка при запросе информации о пакете ФИАС: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not HttpRequestException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ошибка обработки ответа ФИАС: {Message}", ex.Message);
            throw new InvalidOperationException($"Ошибка обработки ответа ФИАС: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Парсит JSON-ответ ФИАС API и преобразует его в объект DownloadFileInfo.
    /// </summary>
    /// <param name="jsonContent">JSON-строка для парсинга.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Информация о пакете ФИАС.</returns>
    /// <exception cref="InvalidOperationException">При ошибках парсинга или валидации данных.</exception>
    /// <exception cref="JsonReaderException">При ошибках разбора JSON.</exception>
    private async Task<DownloadFileInfo> ParseDownloadFileInfoAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            _logger.LogError("Пустой JSON-ответ от ФИАС API");
            throw new InvalidOperationException("Пустой JSON-ответ от ФИАС API");
        }

        try
        {
            var response = JsonConvert.DeserializeObject<DownloadFileInfoResponse>(jsonContent);
            if (response == null)
            {
                throw new InvalidOperationException("Некорректная структура JSON");
            }

            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(response.VersionId))
            {
                throw new InvalidOperationException("В ответе ФИАС отсутствует или пустое поле VersionId");
            }

            if (string.IsNullOrWhiteSpace(response.Date))
            {
                throw new InvalidOperationException("В ответе ФИАС отсутствует или пустое поле Date");
            }

            if (string.IsNullOrWhiteSpace(response.GarXMLDeltaURL))
            {
                throw new InvalidOperationException("В ответе ФИАС отсутствует или пустое поле GarXMLDeltaURL");
            }

            if (string.IsNullOrWhiteSpace(response.GarXMLFullURL))
            {
                throw new InvalidOperationException("В ответе ФИАС отсутствует или пустое поле GarXMLFullURL");
            }

            // Парсинг даты
            if (!DateTime.TryParseExact(response.Date, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                throw new InvalidOperationException($"Некорректный формат даты в ответе ФИАС: {response.Date}");
            }

            var result = new DownloadFileInfo
            {
                VersionId = response.VersionId,
                TextVersion = response.TextVersion,
                Date = date,
                GarXMLDeltaURL = response.GarXMLDeltaURL,
                GarXMLFullURL = response.GarXMLFullURL
            };

            // Проверяем результат парсинга
            var validation = await _downloadFileInfoValidator.ValidateAsync(result, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new InvalidOperationException($"Некорректные данные о пакете ФИАС: {errors}");
            }

            return result;
        }
        catch (JsonReaderException ex)
        {
            throw new InvalidOperationException($"Ошибка разбора JSON-ответа ФИАС: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not JsonReaderException)
        {
            throw new InvalidOperationException($"Неожиданная ошибка при обработке ответа ФИАС: {ex.Message}", ex);
        }
    }
}