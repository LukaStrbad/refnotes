namespace Api.Services.Schedulers;

public interface IPublicFileScheduler
{
    Task ScheduleImageRefreshForEncryptedFile(int encryptedFileId);
    Task ScheduleImageRefreshForPublicFile(int publicFileId);
}
