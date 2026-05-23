namespace Anchor.Web.Images;

public interface IMenuItemImageStorage
{
    Task<string> SaveImageAsync(
        Stream source,
        string originalFileName,
        string? contentType,
        long declaredLength,
        CancellationToken cancellationToken = default);
}
