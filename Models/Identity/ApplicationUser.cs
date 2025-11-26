using Microsoft.AspNetCore.Identity;

namespace ShopOnlineCore.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
}
