using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Db;
using Server.Db.Model;
using Server.Model;
using ServerTests.Fixtures;
using ServerTests.Mocks;
using Xunit.Sdk;
using Xunit.v3;

namespace ServerTests.Data;

public class AutoData : DataAttribute
{
    public override async ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod,
        DisposalTracker disposalTracker)
    {
        // This should have been initialized at this point
        var testDatabaseFixture = TestDatabaseFixture.Instance;

        var dataResolver = new AutoDataResolver(testMethod, testDatabaseFixture);
        disposalTracker.Add(dataResolver);
        await dataResolver.RegisterServicesAsync();

        var data = await dataResolver.ResolveTestParameters();

        if (!data.Any(value => value is AlternativeParameter)) return [ConvertDataRow(data)];

        // Duplicate values if there are alternative values for some parameters
        var normalValues =
            data.Select(v =>
                v is AlternativeParameter alternativeParameter ? alternativeParameter.Value : v).ToArray();

        var alternativeValues =
            data.Select(v =>
                v is AlternativeParameter alternativeParameter ? alternativeParameter.AlternativeValue : v).ToArray();

        return [ConvertDataRow(normalValues), ConvertDataRow(alternativeValues)];
    }

    public override bool SupportsDiscoveryEnumeration()
    {
        return false;
    }
}