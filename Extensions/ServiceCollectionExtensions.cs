using Microsoft.AspNetCore.Identity;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Thêm custom Identity stores (ApplicationUserStore và ApplicationRoleStore)
        /// </summary>
        public static IServiceCollection AddCustomIdentityStores(this IServiceCollection services)
        {
            services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
            services.AddScoped<IRoleStore<IdentityRole>, ApplicationRoleStore>();
            return services;
        }
    }
}
