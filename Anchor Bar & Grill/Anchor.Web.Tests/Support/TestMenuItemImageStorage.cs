using Anchor.Web.Images;

namespace Anchor.Web.Tests.Support;

internal sealed class TestMenuItemImageStorage(string savedPath = "/images/gallery/menuitems/test-image.webp") : IMenuItemImageStorage
{
    public Task<string> SaveImageAsync(
        Stream source,
        string originalFileName,
        string? contentType,
        long declaredLength,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(savedPath);
}
