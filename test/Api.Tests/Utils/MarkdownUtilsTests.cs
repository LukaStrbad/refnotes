using System.Text;
using Api.Utils;

namespace Api.Tests.Utils;

public sealed class MarkdownUtilsTests
{
    [Theory]
    [ClassData(typeof(GetImagesTestData))]
    public async Task GetImagesAsync_ReturnsImages(string markdown, string[] expected)
    {
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

        var result = await MarkdownUtils.GetImagesAsync(memoryStream)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(expected, result);
    }

    private class GetImagesTestData : TheoryData<string, string[]>
    {
        public GetImagesTestData()
        {
            Add("![alt](image.png)", ["image.png"]);
            Add("![alt](subfolder/image.png)", ["subfolder/image.png"]);
            Add("![alt](./subfolder/image.png)", ["./subfolder/image.png"]);
            Add("![alt](/root/image.png)", ["/root/image.png"]);
            Add("""![alt](image.png "Title")""", ["image.png"]);
            Add("""
                ![alt](image.png "Title")
                ![alt2](image2.png "Title2")
                """, ["image.png", "image2.png"]);

            // Empty/null content
            Add("", []);
            Add("   ", []);
            Add("\n\n\n", []);

            // Malformed markdown images
            Add("![alt]()", []); // Empty URL
            Add("![](image.png)", ["image.png"]); // Empty alt text
            Add("![alt text without closing](image.png)", ["image.png"]); // Malformed alt
            Add("![alt](image.png", []); // Unclosed parenthesis
            Add("![alt]image.png)", []); // Missing opening parenthesis

            // Different URL schemes
            Add("![alt](https://example.com/image.png)", ["https://example.com/image.png"]);

            // URLs with query parameters and fragments
            Add("![alt](image.png?v=1&size=large)", ["image.png?v=1&size=large"]);
            Add("![alt](image.png#section)", ["image.png#section"]);

            // Images mixed with other markdown elements
            Add("""
                # Header
                ![alt](image1.png)
                Some text with **bold** and *italic*.
                ![alt](image2.png)
                - List item
                - Another item with ![inline](image3.png)
                """, ["image1.png", "image2.png", "image3.png"]);
        }
    }
}
