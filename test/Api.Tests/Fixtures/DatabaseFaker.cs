using Bogus;
using Data;

namespace Api.Tests.Fixtures;

public class DatabaseFaker<T> : Faker<T> where T : class
{
    private readonly RefNotesContext _context;

    public DatabaseFaker(RefNotesContext context)
    {
        _context = context;
    }

    // Only one Generate override is needed, other methods call this one.
    public override T Generate(string? ruleSets = null)
    {
        var obj = base.Generate(ruleSets);
        _context.Add(obj);
        _context.SaveChanges();
        return obj;
    }
}
