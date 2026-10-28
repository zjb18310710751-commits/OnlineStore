using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class ImageStorageServiceTests
{
    private sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _bytes;

        public FakeFormFile(string fileName, byte[] bytes, string contentType = "image/jpeg")
        {
            FileName = fileName;
            _bytes = bytes;
            ContentType = contentType;
        }

        public string ContentType { get; }
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _bytes.Length;
        public string Name => "ImageFile";
        public string FileName { get; }

        public Stream OpenReadStream() => new MemoryStream(_bytes);

        public void CopyTo(Stream target) => target.Write(_bytes, 0, _bytes.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => target.WriteAsync(_bytes, 0, _bytes.Length, cancellationToken);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string webRootPath) => WebRootPath = webRootPath;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
    }

    private static string CreateTempWebRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "onlinestore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static byte[] ValidJpeg() => new byte[]
    {
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01
    };

    private static byte[] ValidPng() => new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
    };

    [Fact]
    public async Task SaveAsync_SavesFile_AndReturnsRelativeUrl()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));

            var url = await service.SaveAsync(new FakeFormFile("photo.jpg", ValidJpeg()));

            Assert.StartsWith("/uploads/", url);
            Assert.EndsWith(".jpg", url);
            Assert.True(File.Exists(Path.Combine(webRoot, "uploads", Path.GetFileName(url))));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidExtension()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));
            var file = new FakeFormFile("malware.exe", new byte[] { 1, 2, 3, 4 }, "application/octet-stream");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(file));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));
            var file = new FakeFormFile("big.jpg", new byte[5 * 1024 * 1024 + 1]);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(file));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsNonImageContentType()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));
            var file = new FakeFormFile("fake.jpg", new byte[] { 1, 2, 3 }, "text/html");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(file));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsSpoofedImageContents()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));
            var file = new FakeFormFile("fake.jpg", "<html>not an image</html>"u8.ToArray(), "image/jpeg");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(file));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Delete_RemovesSavedFile()
    {
        var webRoot = CreateTempWebRoot();
        try
        {
            var service = new ImageStorageService(new FakeWebHostEnvironment(webRoot));
            var url = await service.SaveAsync(new FakeFormFile("photo.png", ValidPng(), "image/png"));

            service.Delete(url);

            Assert.False(File.Exists(Path.Combine(webRoot, "uploads", Path.GetFileName(url))));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }
}
