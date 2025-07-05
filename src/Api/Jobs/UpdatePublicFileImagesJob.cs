using Api.Services;
using Quartz;

namespace Api.Jobs;

public sealed class UpdatePublicFileImagesJob : IJob
{
    public const string Name = "UpdatePublicFileImagesJob";

    private readonly IPublicFileImageService _publicFileImageService;

    public UpdatePublicFileImagesJob(IPublicFileImageService publicFileImageService)
    {
        _publicFileImageService = publicFileImageService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var publicFileId = dataMap.GetIntValue("publicFileId");
        
        await _publicFileImageService.UpdateImagesForPublicFile(publicFileId);
    }
}
