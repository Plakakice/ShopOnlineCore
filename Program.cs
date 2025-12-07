using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using ShopOnlineCore.Services;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;
using ShopOnlineCore.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ==================== DATABASE + IDENTITY ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký custom UserStore và RoleStore
builder.Services.AddScoped<ApplicationUserStore>();
builder.Services.AddScoped<ApplicationRoleStore>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Override UserStore và RoleStore bằng custom stores
builder.Services.AddScoped<IUserStore<ApplicationUser>>(provider =>
    provider.GetRequiredService<ApplicationUserStore>());

builder.Services.AddScoped<IRoleStore<IdentityRole>>(provider =>
    provider.GetRequiredService<ApplicationRoleStore>());

// ==================== MVC + SESSION + RAZOR ====================
builder.Services.AddControllersWithViews();

// Register Services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // Session timeout 2 tiếng
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 📨 Giả lập EmailSender để tránh lỗi khi đăng ký user
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 🛒 Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderService, OrderService>();

// 1) Thêm Authentication + Cookie
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies") // dùng cookie để lưu trạng thái đăng nhập
    // 2) Google
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google"; // mặc định là /signin-google, có thể đổi
    });

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
    var adminPassword = app.Configuration["AdminPassword"] ?? "Admin@123"; // Fallback chỉ cho môi trường dev nếu quên config

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

