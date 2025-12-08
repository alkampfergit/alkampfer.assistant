using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Bookmarks;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Bookmarks;

public class ContentExtractionServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IFileStore> _fileStoreMock;
    private readonly Mock<ILogger<ContentExtractionService>> _loggerMock;

    public ContentExtractionServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _fileStoreMock = new Mock<IFileStore>();
        _loggerMock = new Mock<ILogger<ContentExtractionService>>();
    }

    [Fact]
    public async Task ExtractAsync_WithSimpleHtml_ShouldReturnMarkdown()
    {
        // Arrange
        var html = "<html><body><h1>Title</h1><p>Content</p></body></html>";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        // We assume ContentExtractionService takes HttpClient (or factory), IFileStore, Logger
        // Since the class doesn't exist yet, this test will fail to compile if I uncomment instantiation.
        // But I need to create the file.
        
        var sut = new ContentExtractionService(_httpClient, _fileStoreMock.Object, _loggerMock.Object);
        
        var result = await sut.ExtractAsync("https://example.com");
        Assert.Contains("# Title", result.Content);
        Assert.Contains("Content", result.Content);
    }
}
