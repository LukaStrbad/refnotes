using Bogus;

namespace Api.Tests.Data.Faker.Definition;

public abstract class FakerImplementationBase
{
    public abstract object CreateFakerObj();
}

public abstract class FakerImplementationBase<T> : FakerImplementationBase where T : class
{
    protected readonly Faker<T> BaseFaker;
    private readonly Dictionary<Type, FakerImplementationBase> _fakerImplementations;

    protected FakerImplementationBase(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<T> baseFaker)
    {
        _fakerImplementations = fakerImplementations;
        BaseFaker = baseFaker;
    }

    protected Faker<TModel> ResolveFaker<TModel>() where TModel : class
    {
        var type = typeof(TModel);
        if (!_fakerImplementations.TryGetValue(type, out var fakerImpl))
            throw new Exception($"No faker implementation registered for type {type.FullName}");

        if (fakerImpl.CreateFakerObj() is Faker<TModel> typedFaker) return typedFaker;
        throw new Exception($"Faker object is not of type {typeof(TModel).FullName}");
    }

    public abstract Faker<T> CreateFaker();

    public override object CreateFakerObj() => CreateFaker();
}
