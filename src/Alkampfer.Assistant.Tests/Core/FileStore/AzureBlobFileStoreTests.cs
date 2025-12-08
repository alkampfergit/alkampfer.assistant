using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.FileStore;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.FileStore;

public class AzureBlobFileStoreTests
{
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly AzureBlobFileStore _sut;

    public AzureBlobFileStoreTests()
    {
        _containerClientMock = new Mock<BlobContainerClient>();
        _sut = new AzureBlobFileStore(_containerClientMock.Object);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldUploadBlob()
    {
        var blobClientMock = new Mock<BlobClient>();
        _containerClientMock.Setup(x => x.GetBlobClient("test.txt")).Returns(blobClientMock.Object);
        _containerClientMock.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), Mock.Of<Response>()));

        var content = new MemoryStream();
        await _sut.SaveFileAsync("test.txt", content);

        blobClientMock.Verify(x => x.UploadAsync(content, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnContent_WhenBlobExists()
    {
        var blobClientMock = new Mock<BlobClient>();
        _containerClientMock.Setup(x => x.GetBlobClient("test.txt")).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        
        var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")));
        blobClientMock.Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(downloadInfo, Mock.Of<Response>()));

        var result = await _sut.GetFileAsync("test.txt");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnNull_WhenBlobDoesNotExist()
    {
        var blobClientMock = new Mock<BlobClient>();
        _containerClientMock.Setup(x => x.GetBlobClient("test.txt")).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var result = await _sut.GetFileAsync("test.txt");

        Assert.Null(result);
    }
}
