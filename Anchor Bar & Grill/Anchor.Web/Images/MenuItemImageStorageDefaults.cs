namespace Anchor.Web.Images;

public static class MenuItemImageStorageDefaults
{
    public const long MaxRawUploadBytes = 50L * 1024L * 1024L;
    public const long MaxProcessedUploadBytes = 5L * 1024L * 1024L;
    public const string PublicFolderPath = "/images/gallery/menuitems";
}
