using FluentValidation;

namespace FiasReportGenerator.Validators;

/// <summary>Проверяет корректность путей к файлам и директориям.</summary>
public class FilePathValidator : AbstractValidator<string>
{
    /// <summary>
    /// Инициализирует валидатор с правилами проверки для путей к файлам и директориям.
    /// </summary>
    public FilePathValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("Path cannot be null or empty")
            .Must(NotContainInvalidCharacters)
            .WithMessage("Path contains invalid characters")
            .Must(NotBeTooLong)
            .WithMessage("Path is too long")
            .Must(NotContainTraversal)
            .WithMessage("Path contains directory traversal attempts");
    }

    /// <summary>
    /// Проверяет, что путь не содержит недопустимых символов.
    /// </summary>
    /// <param name="path">Путь для проверки.</param>
    /// <returns>true если путь не содержит недопустимых символов, иначе false.</returns>
    private bool NotContainInvalidCharacters(string path)
    {
        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Проверяет, что длина пути не превышает допустимый лимит.
    /// </summary>
    /// <param name="path">Путь для проверки.</param>
    /// <returns>true если длина пути допустима, иначе false.</returns>
    private bool NotBeTooLong(string path)
    {
        return path.Length <= 400;
    }

    /// <summary>
    /// Проверяет, что путь не содержит попыток обхода директорий.
    /// </summary>
    /// <param name="path">Путь для проверки.</param>
    /// <returns>true если путь не содержит обхода директорий, иначе false.</returns>
    private bool NotContainTraversal(string path)
    {
        var normalized = Path.GetFullPath(path);
        var parts = normalized.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var dotDotCount = parts.Count(p => p == "..");
        return dotDotCount <= 3; // Разрешаем небольшое количество .. но не слишком много
    }
}

/// <summary>Проверяет базовую корректность URL.</summary>
public class UrlValidator : AbstractValidator<string>
{
    /// <summary>
    /// Инициализирует валидатор с правилами проверки для URL.
    /// </summary>
    public UrlValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("URL cannot be null or empty")
            .Must(BeValidUrl)
            .WithMessage("URL must be a valid absolute URL")
            .Must(BeHttpsOrHttp)
            .WithMessage("URL must use HTTP or HTTPS protocol")
            .MaximumLength(2000)
            .WithMessage("URL is too long");
    }

    /// <summary>
    /// Проверяет, является ли строка корректным абсолютным URL.
    /// </summary>
    /// <param name="url">URL для проверки.</param>
    /// <returns>true если URL корректный, иначе false.</returns>
    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Проверяет, использует ли URL протокол HTTP или HTTPS.
    /// </summary>
    /// <param name="url">URL для проверки.</param>
    /// <returns>true если URL использует HTTP или HTTPS, иначе false.</returns>
    private bool BeHttpsOrHttp(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
