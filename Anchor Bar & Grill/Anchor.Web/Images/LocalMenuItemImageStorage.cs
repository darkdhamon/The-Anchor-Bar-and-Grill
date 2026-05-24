using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Anchor.Web.Images;

public sealed partial class LocalMenuItemImageStorage(
    IWebHostEnvironment environment,
    ILogger<LocalMenuItemImageStorage> logger) : IMenuItemImageStorage
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly int[] EdgeLimits = [2560, 1920, 1600, 1280, 1024];
    private static readonly int[] QualitySteps = [82, 72, 62, 52];

    public async Task<string> StageImageAsync(
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
            await ValidateImageDimensionsAsync(rawImage, cancellationToken);
            rawImage.Position = 0;
            using var image = await LoadImageAsync(rawImage, cancellationToken);
            var processedBytes = await ProcessImageAsync(image, cancellationToken);

            var fileName = BuildStoredFileName(originalFileName);
            var targetDirectory = ResolveTargetDirectory(isStaging: true);
            Directory.CreateDirectory(targetDirectory);
            var fullPath = Path.Combine(targetDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, processedBytes, cancellationToken);

            logger.LogInformation(
                "Staged menu item image {FileName} in {TargetDirectory} ({ContentType}, {ProcessedBytes} bytes).",
                fileName,
                targetDirectory,
                contentType ?? "unknown",
                processedBytes.Length);

            return $"{MenuItemImageStorageDefaults.StagingPublicFolderPath}/{fileName}";
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

    public async Task<string> CommitStagedImageAsync(
        string stagedImagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stagedImagePath))
        {
            throw new MenuItemImageUploadException("Choose an image before saving the menu item.");
        }

        var normalizedStagedPath = NormalizeManagedImagePath(stagedImagePath);
        if (!IsStagedImagePath(normalizedStagedPath))
        {
            throw new MenuItemImageUploadException("The selected upload is no longer available. Upload the image again before saving.");
        }

        var stagedFullPath = ResolveFullPath(normalizedStagedPath);
        if (!File.Exists(stagedFullPath))
        {
            throw new MenuItemImageUploadException("The selected upload is no longer available. Upload the image again before saving.");
        }

        var fileName = Path.GetFileName(stagedFullPath);
        var finalDirectory = ResolveTargetDirectory(isStaging: false);
        Directory.CreateDirectory(finalDirectory);
        var finalFullPath = Path.Combine(finalDirectory, fileName);

        await using (var source = File.Open(stagedFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var destination = File.Open(finalFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        logger.LogInformation(
            "Committed staged menu item image {FileName} from {StagingPath} to {FinalDirectory}.",
            fileName,
            stagedFullPath,
            finalDirectory);

        return $"{MenuItemImageStorageDefaults.PublicFolderPath}/{fileName}";
    }

    public Task DeleteImageAsync(
        string? imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Task.CompletedTask;
        }

        var normalizedPath = NormalizeManagedImagePath(imagePath);
        if (!IsManagedMenuItemImagePath(normalizedPath))
        {
            return Task.CompletedTask;
        }

        var fullPath = ResolveFullPath(normalizedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted menu item image {ImagePath}.", normalizedPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveTargetDirectory(bool isStaging)
    {
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            throw new InvalidOperationException("The web root path is not configured for menu image uploads.");
        }

        return isStaging
            ? Path.Combine(webRootPath, "images", "gallery", "menuitems", "_staged")
            : Path.Combine(webRootPath, "images", "gallery", "menuitems");
    }

    private static async Task ValidateImageDimensionsAsync(Stream source, CancellationToken cancellationToken)
    {
        try
        {
            var imageInfo = await Image.IdentifyAsync(source, cancellationToken);
            if (imageInfo is null)
            {
                throw new MenuItemImageUploadException("The selected file could not be read as an image.");
            }

            if (imageInfo.Width > MenuItemImageStorageDefaults.MaxDecodedEdgePixels
                || imageInfo.Height > MenuItemImageStorageDefaults.MaxDecodedEdgePixels)
            {
                throw new MenuItemImageUploadException($"Images must be {MenuItemImageStorageDefaults.MaxDecodedEdgePixels:N0} pixels or smaller on each side.");
            }

            var pixelCount = (long)imageInfo.Width * imageInfo.Height;
            if (pixelCount > MenuItemImageStorageDefaults.MaxDecodedPixelCount)
            {
                throw new MenuItemImageUploadException($"Images must be {MenuItemImageStorageDefaults.MaxDecodedPixelCount:N0} pixels or smaller in total.");
            }
        }
        catch (MenuItemImageUploadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new MenuItemImageUploadException("The selected file could not be read as an image.", exception);
        }
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

    private static async Task<byte[]> ProcessImageAsync(Image<Rgba32> image, CancellationToken cancellationToken)
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

    private static async Task<Image<Rgba32>> LoadImageAsync(Stream source, CancellationToken cancellationToken)
    {
        try
        {
            var image = await Image.LoadAsync<Rgba32>(source, cancellationToken);
            image.Mutate(context => context.AutoOrient());
            return image;
        }
        catch (Exception exception)
        {
            throw new MenuItemImageUploadException("The selected file could not be read as an image.", exception);
        }
    }

    private static async Task<byte[]> EncodeAttemptAsync(
        Image<Rgba32> originalImage,
        int edgeLimit,
        int quality,
        CancellationToken cancellationToken)
    {
        using var candidate = ResizeIfNeeded(originalImage, edgeLimit);
        await using var output = new MemoryStream();
        var encoder = new WebpEncoder
        {
            Quality = quality
        };

        await candidate.SaveAsync(output, encoder, cancellationToken);
        return output.ToArray();
    }

    private static Image<Rgba32> ResizeIfNeeded(Image<Rgba32> originalImage, int edgeLimit)
    {
        var scale = Math.Min(1f, Math.Min((float)edgeLimit / originalImage.Width, (float)edgeLimit / originalImage.Height));
        var width = Math.Max(1, (int)Math.Round(originalImage.Width * scale));
        var height = Math.Max(1, (int)Math.Round(originalImage.Height * scale));

        if (width == originalImage.Width && height == originalImage.Height)
        {
            return originalImage.Clone();
        }

        return originalImage.Clone(context => context.Resize(width, height));
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

    private static string BuildStoredFileName(string originalFileName)
    {
        var safeBaseName = BuildSafeBaseName(originalFileName);
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        return $"{safeBaseName}-{uniqueSuffix}.webp";
    }

    private string ResolveFullPath(string normalizedPublicPath)
    {
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            throw new InvalidOperationException("The web root path is not configured for menu image uploads.");
        }

        var relativePath = normalizedPublicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var allowedRoot = Path.GetFullPath(Path.Combine(webRootPath, "images", "gallery", "menuitems"));
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Menu item image paths must stay inside the managed gallery folder.");
        }

        return fullPath;
    }

    private static string NormalizeManagedImagePath(string imagePath)
    {
        var trimmed = imagePath.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal)
            ? trimmed
            : $"/{trimmed.TrimStart('/')}";
    }

    private static bool IsManagedMenuItemImagePath(string imagePath) =>
        imagePath.StartsWith(MenuItemImageStorageDefaults.PublicFolderPath, StringComparison.OrdinalIgnoreCase);

    private static bool IsStagedImagePath(string imagePath) =>
        imagePath.StartsWith(MenuItemImageStorageDefaults.StagingPublicFolderPath, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashRegex();
}
