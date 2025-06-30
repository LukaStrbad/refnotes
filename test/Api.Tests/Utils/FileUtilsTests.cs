using Api.Utils;

namespace Api.Tests.Utils;

public sealed class FileUtilsTests
{
    [Theory]
    [InlineData("/folder1", "/folder1/folder2", "..")]
    [InlineData("/", "/folder1/folder2", "../..")]
    [InlineData("/folder1", "/folder1/folder2/folder3", "../..")]
    [InlineData("/folder1/folder2/folder3", "/folder1/folder2", "./folder3")]
    [InlineData("/folder1/folder2/folder3", "/folder1/folder2", "folder3")]
    [InlineData("/folder3", "/folder1/folder2", "/folder3")] // This should ignore the root
    [InlineData("/folder1/folder2", "/folder1/folder2", ".")]
    [InlineData("/", "/", ".")]
    [InlineData("/folder1", "/folder1", "../folder1")]
    [InlineData("/folder1", "/", "./folder1")]
    [InlineData("/file1", "/", "file1")]
    public void ResolveRelativeFolderPath_ShouldReturnExpectedResult(string expected, string root, string relativePath)
    {
        // Act
        var result = FileUtils.ResolveRelativeFolderPath(root, relativePath);

        // Assert
        Assert.Equal(expected, result);
    }

}
