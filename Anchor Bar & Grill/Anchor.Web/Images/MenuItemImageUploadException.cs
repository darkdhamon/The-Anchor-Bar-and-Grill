namespace Anchor.Web.Images;

public sealed class MenuItemImageUploadException : Exception
{
    public MenuItemImageUploadException(string message)
        : base(message)
    {
    }

    public MenuItemImageUploadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
