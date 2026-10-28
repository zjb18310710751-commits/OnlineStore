using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class ImageDeletionSafetyTests
{
    [Theory]
    [InlineData("/uploads/../keep.txt")]
    [InlineData("/uploads/..\\keep.txt")]
    public void Delete_CannotRemoveFilesOutsideUploads(string url)
    {
        var root = Path.Combine(Path.GetTempPath(), "image-safety-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "uploads"));
        var protectedFile = Path.Combine(root, "keep.txt");
        File.WriteAllText(protectedFile, "keep");
        try
        {
            new ImageStorageService(new Environment(root)).Delete(url);
            Assert.True(File.Exists(protectedFile));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class Environment(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = string.Empty;
    }
}
