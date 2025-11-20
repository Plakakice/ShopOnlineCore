using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using ShopOnlineCore.Services;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;
using ShopOnlineCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ==================== DATABASE + IDENTITY ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Ghi đè custom UserStore và RoleStore bằng extension method
builder.Services.AddCustomIdentityStores();

// ==================== MVC + SESSION + RAZOR ====================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// 📨 Giả lập EmailSender để tránh lỗi khi đăng ký user
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 🛒 Register OrderRepository for Dependency Injection
builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();

// ==================== PIPELINE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

async Task CreateAdminRole(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Tạo role "Admin" nếu chưa có
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Email admin cố định
    var adminEmail = "admin@shop.com";
    var adminPassword = "Admin@123"; // bạn có thể đổi

    // Tạo tài khoản admin nếu chưa có
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, adminPassword);
    }

    // Gán quyền Admin cho tài khoản đó
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");
}

// Gọi hàm khởi tạo Admin khi khởi động ứng dụng
await CreateAdminRole(app);

app.Run();

