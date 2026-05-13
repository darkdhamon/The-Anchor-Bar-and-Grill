using Microsoft.AspNetCore.Identity;

namespace Anchor.Web.Data;

public class ApplicationUser : IdentityUser
{
    public bool MustChangePassword { get; set; }

    public bool IsBootstrapAccount { get; set; }
}
