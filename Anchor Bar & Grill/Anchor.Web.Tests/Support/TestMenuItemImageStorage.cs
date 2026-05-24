using System.IO;
using Anchor.Web.Images;

namespace Anchor.Web.Tests.Support;

internal sealed class TestMenuItemImageStorage(string savedPath = "/images/gallery/menuitems/test-image.webp") : IMenuItemImageStorage
{
    public Task<string> StageImageAsync(
        Stream source,
        string originalFileName,
        string? contentType,
        long declaredLength,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"{MenuItemImageStorageDefaults.StagingPublicFolderPath}/{Path.GetFileName(savedPath)}");

    public Task<string> CommitStagedImageAsync(
        string stagedImagePath,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(savedPath);

    public Task DeleteImageAsync(
        string? imagePath,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
