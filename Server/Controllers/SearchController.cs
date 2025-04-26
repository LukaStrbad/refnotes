using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<FileSearchResultDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> SearchFiles(SearchOptionsDto options)
    {
        var files = await searchService.SearchFiles(options)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return Ok(files);
    }
}