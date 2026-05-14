using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Anchor.Web.Data;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [PersonalData]
    [MaxLength(100)]
    public string? LastName { get; set; }

    public bool MustChangePassword { get; set; }

    public bool IsBootstrapAccount { get; set; }
}
