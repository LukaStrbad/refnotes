using Microsoft.Extensions.DependencyInjection;
using Server.Controllers;
using ServerTests.Fixtures;

namespace ServerTests.ControllerTests;

public class PublicFileControllerTests : BaseTests, IClassFixture<ControllerFixture<PublicFileController>>
{
    private readonly PublicFileController _controller;
    
    public PublicFileControllerTests(ControllerFixture<PublicFileController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();

        _controller = serviceProvider.GetRequiredService<PublicFileController>();
    }
}