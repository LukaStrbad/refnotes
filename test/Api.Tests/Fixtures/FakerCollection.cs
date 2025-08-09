using Api.Tests.Data.Faker;
using Data.Model;
using Bogus;
using Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Fixtures;

public static class FakerCollection
{
    public static IServiceCollection GetFakerCollection(RefNotesContext? context)
    {
        var services = new ServiceCollection();

        services.AddTransient<Faker<User>>(_ => GetUserFaker(context));
        services.AddTransient<Faker<EncryptedDirectory>>(sp => GetDirectoryFaker(context, sp));
        services.AddTransient<Faker<UserGroup>>(_ => GetUserGroupFaker(context));

        return services;
    }

    private static Faker<T> CreateBaseFaker<T>(RefNotesContext? context) where T : class
    {
        return context is null ? new Faker<T>() : new DatabaseFaker<T>(context);
    }

    private static Faker<User> GetUserFaker(RefNotesContext? context) =>
        CreateBaseFaker<User>(context)
            .CustomInstantiator(_ => new User("", "", "", ""))
            .StrictMode(true)
            .RuleFor(u => u.Id, f => 0)
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password())
            .RuleFor(u => u.Roles, _ => [])
            .RuleFor(u => u.EmailConfirmed, _ => true);

    private static Faker<EncryptedDirectory> GetDirectoryFaker(RefNotesContext? context, IServiceProvider sp)
    {
        var userFaker = sp.GetRequiredService<Faker<User>>();

        return CreateBaseFaker<EncryptedDirectory>(context)
            .StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Path, f => f.System.DirectoryPath())
            .RuleFor(g => g.PathHash, (_, dir) => dir.Path)
            .RuleFor(g => g.Files, _ => [])
            .RuleFor(g => g.Directories, _ => [])
            .RuleFor(g => g.Parent, _ => null)
            .RuleFor(g => g.Owner, _ => userFaker.Generate())
            .RuleFor(g => g.OwnerId, (_, dir) => dir.Owner?.Id)
            .RuleFor(g => g.Group, _ => null)
            .RuleFor(d => d.GroupId, (_, dir) => dir.Group?.Id);
    }

    private static Faker<UserGroup> GetUserGroupFaker(RefNotesContext? context) =>
        CreateBaseFaker<UserGroup>(context)
            .StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Name, f => f.Lorem.Sentence(5));
}
