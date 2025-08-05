using Api.Services;
using Api.Model;

namespace Api.Tests.Mocks;

public class MockEncryptionKeyProvider : IEncryptionKeyProvider
{
    public byte[] AesKey => "1234567890123456"u8.ToArray();
    public byte[] Sha256Key { get; } = "1234567890123456"u8.ToArray();

    public byte[] Iv => "1234567890123456"u8.ToArray();
}
