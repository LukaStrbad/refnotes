using Api.Controllers;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ControllerTests;

public class PublicFileControllerTests : BaseTests, IClassFixture<ControllerFixture<PublicFileController>>
{
    private readonly PublicFileController _controller;
    
    public PublicFileControllerTests(ControllerFixture<PublicFileController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();

        _controller = serviceProvider.GetRequiredService<PublicFileController>();
    }
}