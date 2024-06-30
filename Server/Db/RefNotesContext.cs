using Microsoft.EntityFrameworkCore;
using Server.Model;

namespace Server.Db;

public class RefNotesContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public string DbPath { get; }
    
    public RefNotesContext()
    {
        const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var refnotesPath = Path.Join(path, "RefNotes");
        Directory.CreateDirectory(refnotesPath);
        DbPath = Path.Join(refnotesPath, "refnotes.db");    
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}