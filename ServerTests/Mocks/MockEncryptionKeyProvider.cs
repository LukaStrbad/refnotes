using Server.Model;
using Server.Services;

namespace ServerTests.Mocks;

public class MockEncryptionKeyProvider : IEncryptionKeyProvider
{
    public byte[] Key => "1234567890123456"u8.ToArray();

    public byte[] Iv => "1234567890123456"u8.ToArray();
}