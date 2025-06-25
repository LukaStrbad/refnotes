using Api.Model;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpPost]
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