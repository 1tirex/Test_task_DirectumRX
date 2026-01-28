using FiasReportGenerator.Models;
using FluentValidation;
using System.Text.RegularExpressions;

namespace FiasReportGenerator.Validators;

/// <summary>Валидатор ответа о пакете изменений ФИАС.</summary>
public class DownloadFileInfoValidator : AbstractValidator<DownloadFileInfo>
{
    /// <summary>
    /// Инициализирует валидатор с правилами проверки для информации о пакете ФИАС.
    /// </summary>
    public DownloadFileInfoValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty()
            .WithMessage("VersionId is required")
            .MaximumLength(50)
            .WithMessage("VersionId must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled)
            .WithMessage("VersionId must contain only alphanumeric characters, dots, underscores, and hyphens");

        RuleFor(x => x.GarXMLDeltaURL)
            .NotEmpty()
            .WithMessage("GarXMLDeltaURL is required")
            .Must(BeValidHttpsUrl)
            .WithMessage("GarXMLDeltaURL must be a valid HTTPS URL pointing to a ZIP file")
            .Must(BeValidFileUrl)
            .WithMessage("GarXMLDeltaURL must point to a ZIP file");

        RuleFor(x => x.GarXMLFullURL)
            .NotEmpty()
            .WithMessage("GarXMLFullURL is required")
            .Must(BeValidHttpsUrl)
            .WithMessage("GarXMLFullURL must be a valid HTTPS URL pointing to a ZIP file")
            .Must(BeValidFileUrl)
            .WithMessage("GarXMLFullURL must point to a ZIP file");

        RuleFor(x => x.Date)
            .NotNull()
            .WithMessage("Date is required")
            .LessThanOrEqualTo(DateTime.Now.AddDays(7))
            .WithMessage("Date cannot be more than 7 days in the future")
            .GreaterThan(DateTime.Now.AddYears(-10))
            .WithMessage("Date cannot be more than 10 years in the past");

        RuleFor(x => x.TextVersion)
            .MaximumLength(200)
            .WithMessage("TextVersion must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.TextVersion));

        RuleFor(x => x)
            .Must(HaveValidUrls)
            .WithMessage("Both URLs must be different if provided");
    }

    /// <summary>
    /// Проверяет, является ли URL корректным HTTPS URL.
    /// </summary>
    /// <param name="url">URL для проверки.</param>
    /// <returns>true если URL корректный HTTPS URL, иначе false.</returns>
    private bool BeValidHttpsUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               uri.Host.Contains('.');
    }

    /// <summary>
    /// Проверяет, указывает ли URL на ZIP-файл.
    /// </summary>
    /// <param name="url">URL для проверки.</param>
    /// <returns>true если URL указывает на ZIP-файл, иначе false.</returns>
    private bool BeValidFileUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.LocalPath.ToLowerInvariant();
        return path.EndsWith(".zip") || path.Contains(".zip?");
    }

    /// <summary>
    /// Проверяет, что оба URL (дельта и полный) различны.
    /// </summary>
    /// <param name="info">Информация о пакете ФИАС для проверки.</param>
    /// <returns>true если URL различны, иначе false.</returns>
    private bool HaveValidUrls(DownloadFileInfo info)
    {
        return info.GarXMLDeltaURL != info.GarXMLFullURL;
    }
}