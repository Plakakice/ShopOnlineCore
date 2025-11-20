using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;

namespace ShopOnlineCore.Models.Identity
{
    /// <summary>
    /// Custom RoleStore that prevents attempting to access IdentityRoleClaim which is not in the model
    /// </summary>
    public class ApplicationRoleStore : RoleStore<IdentityRole, ApplicationDbContext, string>
    {
        public ApplicationRoleStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {
        }

        /// <summary>
        /// Override to prevent attempting to access IdentityRoleClaim<string> from database
        /// </summary>
        public override Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            // Return empty list since we don't have role claims in our schema
            return Task.FromResult<IList<Claim>>(new List<Claim>());
        }

        /// <summary>
        /// Override to prevent attempting to add claims to the database
        /// </summary>
        public override Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            // No-op since we don't support role claims
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override to prevent attempting to remove claims from the database
        /// </summary>
        public override Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            // No-op since we don't support role claims
            return Task.CompletedTask;
        }
    }
}
