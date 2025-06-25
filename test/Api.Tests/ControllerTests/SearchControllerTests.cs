using Api.Controllers;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ControllerTests;

public class SearchControllerTests : IClassFixture<ControllerFixture<SearchController>>
{
    private readonly SearchController _controller;
    private readonly ISearchService _searchService;

    public SearchControllerTests(ControllerFixture<SearchController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _searchService = serviceProvider.GetRequiredService<ISearchService>();
        _controller = serviceProvider.GetRequiredService<SearchController>();
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