using FiasReportGenerator.Models;
using FluentValidation;
using System.Text.RegularExpressions;

namespace FiasReportGenerator.Validators;

/// <summary>Валидатор модели адресного объекта.</summary>
public class AddressObjectValidator : AbstractValidator<AddressObject>
{
    /// <summary>
    /// Инициализирует валидатор с правилами проверки для модели адресного объекта.
    /// </summary>
    public AddressObjectValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0");

        RuleFor(x => x.ObjectId)
            .NotEmpty()
            .WithMessage("ObjectId is required");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 90)
            .WithMessage("Level must be between 1 and 90");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(500)
            .WithMessage("Name must not exceed 500 characters")
            .Matches(@"^[^<>&""']*$", RegexOptions.Compiled)
            .WithMessage("Name contains invalid characters");

        RuleFor(x => x.TypeName)
            .NotEmpty()
            .WithMessage("TypeName is required")
            .MaximumLength(50)
            .WithMessage("TypeName must not exceed 50 characters");

        RuleFor(x => x.ObjectTypeName)
            .MaximumLength(50)
            .WithMessage("ObjectTypeName must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.ObjectTypeName));

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateTime.Now.AddDays(1))
            .WithMessage("StartDate cannot be in the future");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.UpdateDate)
            .LessThanOrEqualTo(DateTime.Now.AddHours(1))
            .WithMessage("UpdateDate cannot be significantly in the future");
    }
}
