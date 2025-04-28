using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Server.Controllers;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class SearchControllerTests
{
    private readonly SearchController _controller;
    private readonly ISearchService _searchService;

    public SearchControllerTests()
    {
        _searchService = Substitute.For<ISearchService>();
        _controller = new SearchController(_searchService);
    }

    [Fact]
    public async Task SearchFiles_ReturnsOk_WhenFilesSearched()
    {
        var searchOptions = new SearchOptionsDto("foo", 0, 100);
        
        var filesToReturn = new List<FileSearchResultDto>
        {
            new("foo", [], "foo", DateTime.Now),
            new("foo_bar", [], "bar", DateTime.Now)
        };
        _searchService.SearchFiles(searchOptions).Returns(filesToReturn.ToAsyncEnumerable());
        
        var response = await _controller.SearchFiles(searchOptions);

        var result = Assert.IsType<OkObjectResult>(response);
        var returnedFiles = Assert.IsType<IEnumerable<FileSearchResultDto>>(result.Value, exactMatch: false);
        
        Assert.Equal(filesToReturn, returnedFiles);
    }
}