using System.Text;
using Api.Controllers;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ControllerTests;

public class FileControllerTests : BaseTests, IClassFixture<ControllerFixture<FileController>>
{
    private readonly FileController _controller;
    private readonly IFileService _fileService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly IPublicFileService _publicFileService;
    private readonly DefaultHttpContext _httpContext;

    public FileControllerTests(ControllerFixture<FileController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();

        _fileService = serviceProvider.GetRequiredService<IFileService>();
        _fileStorageService = serviceProvider.GetRequiredService<IFileStorageService>();
        _groupPermissionService = serviceProvider.GetRequiredService<IGroupPermissionService>();
        _publicFileService = serviceProvider.GetRequiredService<IPublicFileService>();
        _controller = serviceProvider.GetRequiredService<FileController>();

        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task AddFile_ReturnsOk_WhenFileAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var file = Substitute.For<IFormFile>();
        var fileStream = new MemoryStream("file content"u8.ToArray());
        file.OpenReadStream().Returns(fileStream);
        file.FileName.Returns(name);

        _fileService.AddFile(directoryPath, name, null).Returns(fileName);

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath, null);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, fileStream);
    }

    [Fact]
    public async Task AddFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var file = Substitute.For<IFormFile>();
        var fileStream = new MemoryStream("file content"u8.ToArray());
        file.OpenReadStream().Returns(fileStream);
        file.FileName.Returns(name);

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().AddFile(directoryPath, name, groupId);
        await _fileStorageService.DidNotReceiveWithAnyArgs().SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task AddTextFile_ReturnsOk_WhenFileAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        _fileService.AddFile(directoryPath, name, null).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.AddTextFile(directoryPath, name, null);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task AddTextFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.AddTextFile(directoryPath, name, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().AddFile(directoryPath, name, groupId);
        await _fileStorageService.DidNotReceiveWithAnyArgs().SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task MoveFile_ReturnsOk_WhenFileIsMoved()
    {
        const string oldName = "/dir/file.txt";
        const string newName = "/dir2/file2.txt";

        _fileService.MoveFile(oldName, newName, null).Returns(Task.CompletedTask);

        var result = await _controller.MoveFile(oldName, newName, null);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MoveFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string oldName = "/dir/file.txt";
        const string newName = "/dir2/file2.txt";

        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        var result = await _controller.MoveFile(oldName, newName, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().MoveFile(oldName, newName, groupId);
    }

    [Fact]
    public async Task GetFile_ReturnsOk_WhenFileExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var stream = Substitute.For<Stream>();

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns(fileName);
        _fileStorageService.GetFile(fileName).Returns(stream);

        var result = await _controller.GetFile(directoryPath, name, null);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns((string?)null);

        var result = await _controller.GetFile(directoryPath, name, null);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var stream = Substitute.For<Stream>();

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns(fileName);
        _fileStorageService.GetFile(fileName).Returns(stream);

        var result = await _controller.GetFile(directoryPath, name, groupId);

        Assert.IsType<ForbidResult>(result);
        _fileStorageService.DidNotReceiveWithAnyArgs().GetFile(fileName);
        await _fileService.DidNotReceive().GetFilesystemFilePath(directoryPath, name, groupId);
    }

    [Fact]
    public async Task GetPublicFile_ReturnsOk_WhenFileExists()
    {
        const string urlHash = "test_url_hash";
        var encryptedFile = new EncryptedFile("abcd.txt", "test")
        {
            Id = 123
        };
        var fileDto = new FileDto("test.txt", "/test.txt", [], 1024, DateTime.UtcNow, DateTime.UtcNow);
        var stream = Substitute.For<Stream>();

        _publicFileService.GetEncryptedFileAsync(urlHash).Returns(encryptedFile);
        _publicFileService.IsPublicFileActive(urlHash).Returns(true);
        _fileService.GetFileInfoAsync(encryptedFile.Id).Returns(fileDto);
        _fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(stream);

        var result = await _controller.GetPublicFile(urlHash);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetPublicFile_ReturnsNotFound_WhenPublicFileDoesntExist()
    {
        const string urlHash = "test_url_hash";
        _publicFileService.GetEncryptedFileAsync(urlHash).Returns((EncryptedFile?)null);

        var result = await _controller.GetPublicFile(urlHash);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetImage_ReturnsOk_WhenImageExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string imageName = "test_file_name.png";
        var stream = Substitute.For<Stream>();

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns(imageName);
        _fileStorageService.GetFile(imageName).Returns(stream);

        var result = await _controller.GetImage(directoryPath, name, null);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetImage_ReturnsEmptyStream_WhenImageDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name.png";
        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns((string?)null);

        var result = await _controller.GetImage(directoryPath, name, null);

        var okResult = Assert.IsType<FileContentResult>(result);
        Assert.Empty(okResult.FileContents);
    }

    [Fact]
    public async Task GetImage_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string imageName = "test_file_name.png";
        var stream = Substitute.For<Stream>();

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        var result = await _controller.GetImage(directoryPath, name, groupId);

        Assert.IsType<ForbidResult>(result);
        _fileStorageService.DidNotReceiveWithAnyArgs().GetFile(imageName);
        await _fileService.DidNotReceive().GetFilesystemFilePath(directoryPath, name, groupId);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_WhenFileDeleted()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.DeleteFile(directoryPath, name, null).Returns(Task.CompletedTask);

        var result = await _controller.DeleteFile(directoryPath, name, null);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns((string?)null);

        var result = await _controller.DeleteFile(directoryPath, name, null);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        var result = await _controller.DeleteFile(directoryPath, name, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().DeleteFile(directoryPath, name, groupId);
    }

    [Fact]
    public async Task SaveTextFile_ReturnsOk_WhenFileSaved()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.SaveTextFile(directoryPath, name, null);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
        await _fileService.Received(1).UpdateTimestamp(directoryPath, name, null);
    }

    [Fact]
    public async Task SaveTextFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.GetFilesystemFilePath(directoryPath, name, null).Returns((string?)null);

        var result = await _controller.SaveTextFile(directoryPath, name, null);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task SaveTextFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.SaveTextFile(directoryPath, name, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().UpdateTimestamp(directoryPath, name, groupId);
        await _fileStorageService.DidNotReceiveWithAnyArgs().SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task GetFileInfo_ReturnsFileInfo()
    {
        const string filePath = "/file.txt";

        _fileService.GetFileInfo(filePath, null).Returns(Task.FromResult(
            new FileDto("file.txt", filePath, ["tag1", "tag2"], 1024, DateTime.UtcNow, DateTime.UtcNow))
        );

        var result = await _controller.GetFileInfo(filePath, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var fileInfo = Assert.IsType<FileDto>(okResult.Value);
        Assert.Equal("file.txt", fileInfo.Name);
    }

    [Fact]
    public async Task GetFileInfo_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string filePath = "/file.txt";

        // Deny access to group
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);

        var result = await _controller.GetFileInfo(filePath, groupId);

        Assert.IsType<ForbidResult>(result);
        await _fileService.DidNotReceive().GetFileInfo(filePath, groupId);
    }

    [Fact]
    public async Task GetPublicFileInfo_ReturnsFileInfo()
    {
        const string urlHash = "test_url_hash";
        var encryptedFile = new EncryptedFile("abcd.txt", "test")
        {
            Id = 123
        };
        var fileDto = new FileDto("test.txt", "/test.txt", [], 1024, DateTime.UtcNow, DateTime.UtcNow);

        _publicFileService.GetEncryptedFileAsync(urlHash).Returns(encryptedFile);
        _publicFileService.IsPublicFileActive(urlHash).Returns(true);
        _fileService.GetFileInfoAsync(encryptedFile.Id).Returns(fileDto);

        var result = await _controller.GetPublicFileInfo(urlHash);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var fileInfo = Assert.IsType<FileDto>(okResult.Value);
        Assert.Equal("test.txt", fileInfo.Name);
    }

    [Fact]
    public async Task GetPublicFileInfo_ReturnsNotFound_WhenPublicFileDoesntExist()
    {
        const string urlHash = "test_url_hash";
        _publicFileService.GetEncryptedFileAsync(urlHash).Returns((EncryptedFile?)null);

        var result = await _controller.GetPublicFileInfo(urlHash);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }
}