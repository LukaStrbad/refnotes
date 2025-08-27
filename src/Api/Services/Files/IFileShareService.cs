using Data.Model;

namespace Api.Services.Files;

public interface IFileShareService
{
    Task<string> GenerateShareHash(int encryptedFileId);
    Task<SharedFile> GenerateSharedFileFromHash(string hash, int attachedDirectoryId);
    Task<User?> GetOwnerFromHash(string hash);
}
