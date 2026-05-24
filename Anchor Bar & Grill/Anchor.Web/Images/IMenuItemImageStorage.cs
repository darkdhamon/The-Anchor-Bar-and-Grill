namespace Anchor.Web.Images;

public interface IMenuItemImageStorage
{
    Task<string> StageImageAsync(
        Stream source,
        string originalFileName,
        string? contentType,
        long declaredLength,
        CancellationToken cancellationToken = default);

    Task<string> CommitStagedImageAsync(
        string stagedImagePath,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string? imagePath,
        CancellationToken cancellationToken = default);
}
