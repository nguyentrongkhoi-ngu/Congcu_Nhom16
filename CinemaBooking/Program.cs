using Microsoft.EntityFrameworkCore;
using CinemaBooking.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using CinemaBooking.Models;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.FileProviders;
using CinemaBooking.Models.Services;
using CinemaBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Thiết lập mã hóa UTF-8 cho console
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 2. Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Cấu hình kích thước tối đa cho upload file (100MB)
builder.Services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = 104857600; });
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
    options.ListenAnyIP(5153); // HTTP
    options.ListenAnyIP(7065, listenOptions => { listenOptions.UseHttps(); }); // HTTPS
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// 3. Đăng ký Database & Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "CinemaBookingAuth";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// 4. Đăng ký các Service tùy chỉnh
builder.Services.AddScoped<IPasswordHasher<NguoiDung>, PasswordHasher<NguoiDung>>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<MomoService>();
builder.Services.AddScoped<PaymentLogger>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddHostedService<LichChieuCleanupService>();

// 5. External Auth & Authorization
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOrUser", policy => policy.RequireRole("Admin", "User"));
});

// 6. Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 7. Configure Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Static files cho thư mục uploads
var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CinemaBooking.Middlewares.AdminRestrictionMiddleware>();
app.UseSession();

// 8. Routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<CinemaBooking.Hubs.BookingHub>("/bookingHub");

// 9. Startup & Seed Data (có try-catch để an toàn)
try 
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        Console.WriteLine("--- Initializing Data Seed ---");
        // Chạy Seed truyền thống cho Phim, Rạp...
        await SeedData.Initialize(context);
        // Chạy Seed cho Identity (User, Roles)
        await IdentitySeeder.SeedAsync(userManager, roleManager, context);
    }

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR ON STARTUP: {ex.Message}");
    if (ex.InnerException != null) 
    {
        Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
    }
    Console.WriteLine(ex.StackTrace);
    throw;
}