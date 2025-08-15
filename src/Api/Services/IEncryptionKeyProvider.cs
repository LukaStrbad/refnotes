namespace Api.Services;

public interface IEncryptionKeyProvider
{
    public byte[] AesKey { get; }
    public byte[] Sha256Key { get; }
}
