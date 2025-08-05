using System.Diagnostics;
using Api.Services;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Api;

public static class CommandLine
{
    public static async Task ParseAndExecute(string[] args, WebApplication app)
    {
        if (args.Length == 0)
            return;

        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "generate-hashes":
                await GenerateHashes(serviceProvider);
                break;
        }
        
        Environment.Exit(0);
    }

    private static async Task GenerateHashes(IServiceProvider serviceProvider)
    {
        var sw = Stopwatch.StartNew();
        var context = serviceProvider.GetRequiredService<RefNotesContext>();
        var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
        
        // Hash Directories
        Console.WriteLine("Generating hashes for directories");
        var idWithPaths = await context.Directories
            .Select(d => new { d.Id, d.Path })
            .ToListAsync();
        foreach (var idWithPath in idWithPaths)
        {
            var decryptedPath = encryptionService.DecryptAesStringBase64(idWithPath.Path);
            var pathHash = encryptionService.HashString(decryptedPath);
            await context.Directories
                .Where(d => d.Id == idWithPath.Id)
                .ExecuteUpdateAsync(d => d.SetProperty(dir => dir.PathHash, pathHash));
        }
        
        // Hash files
        Console.WriteLine("Generating hashes for files");
        var idWithFileNames = await context.Files
            .Select(f => new { f.Id, f.Name })
            .ToListAsync();
        foreach (var idWithFileName in idWithFileNames)
        {
            var decryptedName = encryptionService.DecryptAesStringBase64(idWithFileName.Name);
            var nameHash = encryptionService.HashString(decryptedName);
            await context.Files
                .Where(f => f.Id == idWithFileName.Id)
                .ExecuteUpdateAsync(f => f.SetProperty(file => file.NameHash, nameHash));
        }
        
        // Hash tags
        Console.WriteLine("Generating hashes for tags");
        var idWithTagNames = await context.FileTags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();
        foreach (var idWithTagName in idWithTagNames)
        {
            var decryptedName = encryptionService.DecryptAesStringBase64(idWithTagName.Name);
            var nameHash = encryptionService.HashString(decryptedName);
            await context.FileTags
                .Where(t => t.Id == idWithTagName.Id)
                .ExecuteUpdateAsync(t => t.SetProperty(tag => tag.NameHash, nameHash));
        }
        
        sw.Stop();
        Console.WriteLine($"Hashes generated in {sw.ElapsedMilliseconds} ms");
    }
}
