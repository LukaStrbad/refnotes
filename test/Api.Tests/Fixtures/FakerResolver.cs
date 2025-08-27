using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Fixtures;

public sealed class FakerResolver
{
    private readonly IServiceProvider _serviceProvider;

    public FakerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Faker<T> Get<T>() where T : class
        => _serviceProvider.GetRequiredService<Faker<T>>();
}
