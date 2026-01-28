using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FiasReportGenerator.Extensions;

/// <summary>
/// Расширения для IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все валидаторы из указанной сборки.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="assembly">Сборка для сканирования валидаторов (определяется по типу T).</param>
    /// <returns>Коллекция сервисов с зарегистрированными валидаторами.</returns>
    public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;

        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var interfaces = validatorType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var validatorInterface in interfaces)
            {
                services.AddTransient(validatorInterface, validatorType);
            }
        }

        return services;
    }
}