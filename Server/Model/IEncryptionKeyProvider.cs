namespace Server.Model;

public interface IEncryptionKeyProvider
{
    public byte[] Key { get; }
    public byte[] Iv { get; }
}