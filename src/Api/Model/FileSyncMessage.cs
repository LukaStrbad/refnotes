namespace Api.Model;

public abstract class FileSyncMessage
{
    public abstract FileSyncMessageType MessageType { get; }
}

public class FileUpdatedMessage : FileSyncMessage
{
    public override FileSyncMessageType MessageType => FileSyncMessageType.UpdateTime;
    public DateTime Time { get; }

    public FileUpdatedMessage(DateTime time)
    {
        Time = time;
    }
}

public class UserIdMessage : FileSyncMessage
{
    public override FileSyncMessageType MessageType => FileSyncMessageType.UserId;
    public string UserId { get; }

    public UserIdMessage(string userId)
    {
        UserId = userId;
    }
}

public enum FileSyncMessageType
{
    UpdateTime,
    UserId
}
