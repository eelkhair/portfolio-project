using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyApi.Tests.Helpers;

/// <summary>
/// Wraps FluentValidation validators with a ServiceProvider for resolving dependencies
/// in FastEndpoints validators that use Resolve&lt;T&gt;().
/// </summary>
public static class ValidatorTestHelper
{
    public static async Task<ValidationResult> ValidateWithServicesAsync<T>(
        IValidator<T> validator,
        T instance,
        IServiceProvider serviceProvider) where T : class
    {
        var context = new ValidationContext<T>(instance);
        context.RootContextData["__FE_ServiceProvider"] = serviceProvider;
        // FastEndpoints validators use Resolve<T>() which reads from context
        // For unit tests, we bypass that by running validation directly
        return await validator.ValidateAsync(context);
    }
}
