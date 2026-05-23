using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;

namespace Anchor.Web.Images;

public sealed partial class LocalMenuItemImageStorage(
    IWebHostEnvironment environment,
    ILogger<LocalMenuItemImageStorage> logger) : IMenuItemImageStorage
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly int[] EdgeLimits = [2560, 1920, 1600, 1280, 1024];
    private static readonly int[] QualitySteps = [82, 72, 62, 52];

    public async Task<string> SaveImageAsync(
        Stream source,
        string originalFileName,
        string? contentType,
        long declaredLength,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new MenuItemImageUploadException("Upload a JPG, JPEG, PNG, or WEBP image.");
        }

        if (declaredLength <= 0)
        {
            throw new MenuItemImageUploadException("Choose a non-empty image file before uploading.");
        }

        if (declaredLength > MenuItemImageStorageDefaults.MaxRawUploadBytes)
        {
            throw new MenuItemImageUploadException("Upload images must be 50 MB or smaller.");
        }

        await using var rawImage = new MemoryStream();
        await CopyWithLimitAsync(source, rawImage, MenuItemImageStorageDefaults.MaxRawUploadBytes, cancellationToken);
        if (rawImage.Length == 0)
        {
            throw new MenuItemImageUploadException("Choose a non-empty image file before uploading.");
        }

        rawImage.Position = 0;

        try
        {
            using var image = LoadBitmap(rawImage);
            var processedBytes = await ProcessImageAsync(image, cancellationToken);

            var targetDirectory = ResolveTargetDirectory();
            Directory.CreateDirectory(targetDirectory);

            var safeBaseName = BuildSafeBaseName(originalFileName);
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"{safeBaseName}-{uniqueSuffix}.webp";
            var fullPath = Path.Combine(targetDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, processedBytes, cancellationToken);

            logger.LogInformation(
                "Saved menu item image {FileName} to {TargetDirectory} ({ContentType}, {ProcessedBytes} bytes).",
                fileName,
                targetDirectory,
                contentType ?? "unknown",
                processedBytes.Length);

            return $"{MenuItemImageStorageDefaults.PublicFolderPath}/{fileName}";
        }
        catch (MenuItemImageUploadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save menu item image {OriginalFileName}.", originalFileName);
            throw new MenuItemImageUploadException("We couldn't finish processing that image. Try another file.");
        }
    }

    private string ResolveTargetDirectory()
    {
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            throw new InvalidOperationException("The web root path is not configured for menu image uploads.");
        }

        return Path.Combine(
            webRootPath,
            "images",
            "gallery",
            "menuitems");
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long totalBytes = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > maxBytes)
            {
                throw new MenuItemImageUploadException("Upload images must be 50 MB or smaller.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static async Task<byte[]> ProcessImageAsync(SKBitmap image, CancellationToken cancellationToken)
    {
        foreach (var edgeLimit in EdgeLimits)
        {
            foreach (var quality in QualitySteps)
            {
                var bytes = await EncodeAttemptAsync(image, edgeLimit, quality, cancellationToken);
                if (bytes.LongLength <= MenuItemImageStorageDefaults.MaxProcessedUploadBytes)
                {
                    return bytes;
                }
            }
        }

        throw new MenuItemImageUploadException("The image could not be compressed below 5 MB. Please choose a smaller source image.");
    }

    private static SKBitmap LoadBitmap(Stream source)
    {
        try
        {
            var bitmap = SKBitmap.Decode(source);
            return bitmap ?? throw new MenuItemImageUploadException("The selected file could not be read as an image.");
        }
        catch (Exception exception)
        {
            throw new MenuItemImageUploadException("The selected file could not be read as an image.", exception);
        }
    }

    private static async Task<byte[]> EncodeAttemptAsync(
        SKBitmap originalImage,
        int edgeLimit,
        int quality,
        CancellationToken cancellationToken)
    {
        using var candidate = ResizeIfNeeded(originalImage, edgeLimit);
        using var image = SKImage.FromBitmap(candidate);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        return data.ToArray();
    }

    private static SKBitmap ResizeIfNeeded(SKBitmap originalImage, int edgeLimit)
    {
        var scale = Math.Min(1f, Math.Min((float)edgeLimit / originalImage.Width, (float)edgeLimit / originalImage.Height));
        var width = Math.Max(1, (int)Math.Round(originalImage.Width * scale));
        var height = Math.Max(1, (int)Math.Round(originalImage.Height * scale));
        var resized = new SKBitmap(width, height, originalImage.ColorType, originalImage.AlphaType);

        using var canvas = new SKCanvas(resized);
        using var sourceImage = SKImage.FromBitmap(originalImage);
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(
            sourceImage,
            new SKRect(0, 0, originalImage.Width, originalImage.Height),
            new SKRect(0, 0, width, height),
            sampling);
        canvas.Flush();
        return resized;
    }

    private static string BuildSafeBaseName(string originalFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName).Trim();
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return "menu-item";
        }

        var builder = new StringBuilder(baseName.Length);
        bool previousWasDash = false;
        foreach (var character in baseName)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var sanitized = CollapseDashRegex().Replace(builder.ToString().Trim('-'), "-");
        return string.IsNullOrWhiteSpace(sanitized) ? "menu-item" : sanitized;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashRegex();
}
