namespace Api.Services;

public interface IPublicFileImageService
{
    Task ScheduleImageRefreshForPublicFile(int publicFileId);
    Task UpdateImagesForPublicFile(int publicFileId);
    Task RemoveImagesForPublicFile(int publicFileId);
}
