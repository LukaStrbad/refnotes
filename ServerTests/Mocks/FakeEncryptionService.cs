using System.Text;
using Server.Services;

namespace ServerTests.Mocks;

public class FakeEncryptionService : IEncryptionService
{
    public byte[] EncryptAes(byte[] bytes)
    {
        return bytes;
    }

    public byte[] EncryptAes(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    public async Task EncryptAesToStreamAsync(Stream inputStream, Stream outputStream)
    {
        await inputStream.CopyToAsync(outputStream);
    }

    public string EncryptAesStringBase64(string text)
    {
        return text;
    }

    public IEnumerable<byte> DecryptAes(byte[] encryptedBytes)
    {
        return encryptedBytes;
    }

    public void DecryptAesToStream(Stream encryptedInputStream, Stream decryptedOutputStream)
    {
        encryptedInputStream.CopyTo(decryptedOutputStream);
    }

    public string DecryptAesString(byte[] encryptedBytes)
    {
        return Encoding.UTF8.GetString(encryptedBytes);
    }

    public string DecryptAesStringBase64(string encryptedText)
    {
        return encryptedText;
    }
}