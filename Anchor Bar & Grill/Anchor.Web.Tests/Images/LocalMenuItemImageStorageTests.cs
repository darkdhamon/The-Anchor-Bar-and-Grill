using Anchor.Web.Images;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Anchor.Web.Tests.Images;

public sealed class LocalMenuItemImageStorageTests
{
    [Fact]
    public async Task SaveImageAsync_saves_processed_webp_under_menuitems_and_returns_public_path()
    {
        using var tempDirectory = new TemporaryDirectory();
        var webRoot = Path.Combine(tempDirectory.Path, "wwwroot");
        var service = new LocalMenuItemImageStorage(
            new TestWebHostEnvironment(webRoot),
            NullLogger<LocalMenuItemImageStorage>.Instance);

        var pngBytes = CreatePngBytes();
        await using var source = new MemoryStream(pngBytes);

        var savedPath = await service.SaveImageAsync(source, "Anchor Burger.png", "image/png", pngBytes.Length);

        Assert.StartsWith("/images/gallery/menuitems/", savedPath, StringComparison.Ordinal);
        Assert.EndsWith(".webp", savedPath, StringComparison.OrdinalIgnoreCase);

        var fileName = Path.GetFileName(savedPath);
        var savedFile = Path.Combine(webRoot, "images", "gallery", "menuitems", fileName);

        Assert.True(File.Exists(savedFile));
        Assert.InRange(new FileInfo(savedFile).Length, 1, MenuItemImageStorageDefaults.MaxProcessedUploadBytes);
    }

    [Fact]
    public async Task SaveImageAsync_accepts_jpeg_uploads_and_normalizes_them_to_webp()
    {
        using var tempDirectory = new TemporaryDirectory();
        var webRoot = Path.Combine(tempDirectory.Path, "wwwroot");
        var service = new LocalMenuItemImageStorage(
            new TestWebHostEnvironment(webRoot),
            NullLogger<LocalMenuItemImageStorage>.Instance);

        var jpegBytes = CreateJpegBytes();
        await using var source = new MemoryStream(jpegBytes);

        var savedPath = await service.SaveImageAsync(source, "phone-photo.jpg", "image/jpeg", jpegBytes.Length);

        Assert.StartsWith("/images/gallery/menuitems/", savedPath, StringComparison.Ordinal);
        Assert.EndsWith(".webp", savedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveImageAsync_rejects_unsupported_extensions()
    {
        using var tempDirectory = new TemporaryDirectory();
        var service = new LocalMenuItemImageStorage(
            new TestWebHostEnvironment(Path.Combine(tempDirectory.Path, "wwwroot")),
            NullLogger<LocalMenuItemImageStorage>.Instance);

        await using var source = new MemoryStream([0x01, 0x02, 0x03]);

        var exception = await Assert.ThrowsAsync<MenuItemImageUploadException>(() =>
            service.SaveImageAsync(source, "menu-item.gif", "image/gif", 3));

        Assert.Contains("JPG", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveImageAsync_rejects_empty_files()
    {
        using var tempDirectory = new TemporaryDirectory();
        var service = new LocalMenuItemImageStorage(
            new TestWebHostEnvironment(Path.Combine(tempDirectory.Path, "wwwroot")),
            NullLogger<LocalMenuItemImageStorage>.Instance);

        await using var source = new MemoryStream();

        var exception = await Assert.ThrowsAsync<MenuItemImageUploadException>(() =>
            service.SaveImageAsync(source, "empty.png", "image/png", 0));

        Assert.Contains("non-empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveImageAsync_rejects_declared_uploads_over_50_mb()
    {
        using var tempDirectory = new TemporaryDirectory();
        var service = new LocalMenuItemImageStorage(
            new TestWebHostEnvironment(Path.Combine(tempDirectory.Path, "wwwroot")),
            NullLogger<LocalMenuItemImageStorage>.Instance);

        await using var source = new MemoryStream([0x01]);

        var exception = await Assert.ThrowsAsync<MenuItemImageUploadException>(() =>
            service.SaveImageAsync(source, "oversized.png", "image/png", MenuItemImageStorageDefaults.MaxRawUploadBytes + 1));

        Assert.Contains("50 MB", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreatePngBytes()
    {
        using var image = CreateSampleImage();
        using var data = new MemoryStream();
        image.SaveAsPng(data);
        return data.ToArray();
    }

    private static byte[] CreateJpegBytes()
    {
        using var image = CreateSampleImage();
        using var data = new MemoryStream();
        image.SaveAsJpeg(data);
        return data.ToArray();
    }

    private static Image<Rgba32> CreateSampleImage()
    {
        var image = new Image<Rgba32>(1600, 1200);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                image[x, y] = new Rgba32((byte)(x % 255), (byte)(y % 255), (byte)((x + y) % 255));
            }
        }

        return image;
    }

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Anchor.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"anchor-web-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
