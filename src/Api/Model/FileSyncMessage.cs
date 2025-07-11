namespace Api.Model;

public abstract class FileSyncMessage
{
    public abstract FileSyncMessageType MessageType { get; }
}

public class FileUpdatedMessage : FileSyncMessage
{
    public override FileSyncMessageType MessageType => FileSyncMessageType.UpdateTime;
    public DateTime Time { get; }
    public string SenderClientId { get; }

    public FileUpdatedMessage(DateTime time, string senderClientId)
    {
        Time = time;
        SenderClientId = senderClientId;
    }
}

public class ClientIdMessage : FileSyncMessage
{
    public override FileSyncMessageType MessageType => FileSyncMessageType.ClientId;
    public string ClientId { get; }

    public ClientIdMessage(string clientId)
    {
        ClientId = clientId;
    }
}

public enum FileSyncMessageType
{
    UpdateTime,
    ClientId
}
