using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddSingletonSubstitute<T>(this IServiceCollection serviceCollection) where T : class
    {
        serviceCollection.AddSingleton(Substitute.For<T>());
    }

    public static void AddScopedSubstitute<T>(this IServiceCollection serviceCollection) where T : class
    {
        serviceCollection.AddScoped<T>(
            implementationFactory: static _ => Substitute.For<T>());
    }

    public static void AddTransientSubstitute<T>(this IServiceCollection serviceCollection) where T : class
    {
        serviceCollection.AddTransient<T>(
            implementationFactory: static _ => Substitute.For<T>());
    }
}
