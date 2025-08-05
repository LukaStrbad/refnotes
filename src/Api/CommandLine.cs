using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
            case "update-encrypted-fields":
                await UpdateEncryptedFields(serviceProvider);
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

    private static async Task UpdateEncryptedFields(IServiceProvider serviceProvider)
    {
        var sw = Stopwatch.StartNew();
        var context = serviceProvider.GetRequiredService<RefNotesContext>();
        var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
        var keyProvider = serviceProvider.GetRequiredService<IEncryptionKeyProvider>();
        var appSettings = serviceProvider.GetRequiredService<AppSettings>();
        
        using var aes = Aes.Create();
        aes.Key = keyProvider.AesKey;
        aes.IV = keyProvider.Iv;
        using var decryptor = aes.CreateDecryptor();
        
        async Task<string> UpdateEncryption(string input, ICryptoTransform decry)
        {
            var bytes = Convert.FromBase64String(input);
            using var ms = new MemoryStream();
            await using (var cs = new CryptoStream(ms, decry, CryptoStreamMode.Write))
            {
                cs.Write(bytes, 0, bytes.Length);
            }
        
            var decryptedString = Encoding.UTF8.GetString(ms.ToArray());
            return encryptionService.EncryptAesStringBase64(decryptedString);
        }
        
        // Update directories
        Console.WriteLine("Updating encrypted directory paths");
        var encryptedDirectories = await context.Directories
            .Select(d => new { d.Id, d.Path })
            .ToListAsync();
        foreach (var encryptedDirectory in encryptedDirectories)
        {
            var encryptedPath = await UpdateEncryption(encryptedDirectory.Path, decryptor);
            await context.Directories
                .Where(d => d.Id == encryptedDirectory.Id)
                .ExecuteUpdateAsync(d => d.SetProperty(dir => dir.Path, encryptedPath));
        }
        
        // Update files
        Console.WriteLine("Updating encrypted file paths");
        var encryptedFiles = await context.Files
            .Select(f => new { f.Id, f.Name, f.FilesystemName })
            .ToListAsync();
        foreach (var encryptedFile in encryptedFiles)
        {
            var encryptedName = await UpdateEncryption(encryptedFile.Name, decryptor);
            await context.Files
                .Where(f => f.Id == encryptedFile.Id)
                .ExecuteUpdateAsync(d => d.SetProperty(file => file.Name, encryptedName));
        
            // Update the file contents
            var filePath = Path.Combine(appSettings.DataDir, encryptedFile.FilesystemName);
            // Read the file
            var encryptedBytes = await File.ReadAllBytesAsync(filePath);
        
            using var ms = new MemoryStream();
            await using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        
            // Write the encrypted bytes to the file
            encryptedBytes = encryptionService.EncryptAes(ms.ToArray());
            await File.WriteAllBytesAsync(filePath, encryptedBytes);
        }
        
        // Update tags
        Console.WriteLine("Updating encrypted tag names");
        var fileTags = await context.FileTags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();
        foreach (var fileTag in fileTags)
        {
            var encryptedName = await UpdateEncryption(fileTag.Name, decryptor);
            await context.FileTags
                .Where(f => f.Id == fileTag.Id)
                .ExecuteUpdateAsync(t => t.SetProperty(tag => tag.Name, encryptedName));
        }
        
        // Update groups
        Console.WriteLine("Updating encrypted group names");
        var groups = await context.UserGroups
            .Select(g => new { g.Id, g.Name })
            .ToListAsync();
        foreach (var group in groups)
        {
            if (group.Name is null)
                continue;
            var encryptedName = await UpdateEncryption(group.Name, decryptor);
            await context.UserGroups
                .Where(g => g.Id == group.Id)
                .ExecuteUpdateAsync(t => t.SetProperty(grp => grp.Name, encryptedName));
        }
        
        sw.Stop();
        Console.WriteLine($"Encrypted fields updated in {sw.ElapsedMilliseconds} ms");
    }
}
