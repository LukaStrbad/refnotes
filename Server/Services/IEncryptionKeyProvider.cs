namespace Server.Services;

public interface IEncryptionKeyProvider
{
    public byte[] Key { get; }
    public byte[] Iv { get; }
}