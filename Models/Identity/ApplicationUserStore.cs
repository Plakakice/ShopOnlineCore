using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ShopOnlineCore.Models.Identity
{
    /// <summary>
    /// Custom user store that doesn't use Claims, Logins, or Tokens
    /// </summary>
    public class ApplicationUserStore : UserStore<ApplicationUser, IdentityRole, ApplicationDbContext, string>
    {
        public ApplicationUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {
        }

        // Override methods that use Claims to prevent them from being called
        public override Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            // Return empty list instead of trying to fetch from DB
            return Task.FromResult<IList<Claim>>(new List<Claim>());
        }

        public override Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            // Do nothing - claims are not supported
            return Task.CompletedTask;
        }

        public override Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            // Do nothing - claims are not supported
            return Task.CompletedTask;
        }

        public override Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            // Do nothing - claims are not supported
            return Task.CompletedTask;
        }

        public override Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            // Return empty list - claims are not supported
            return Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
        }
    }
}
