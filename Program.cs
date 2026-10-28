using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projekt_cyber.Data;
using projekt_cyber.Models;
using ScottBrady91.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 15;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher<ApplicationUser>>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var settings = await db.PasswordPolicySettings.FindAsync(1);

    if (settings != null && settings.IdleTimeoutMinutes <= 0)
    {
        settings.IdleTimeoutMinutes = 1;
        await db.SaveChangesAsync();
    }
}

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

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated != true ||
        context.Request.Path == "/Account/Login")
    {
        await next();
        return;
    }

    var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

    var settings = await db.PasswordPolicySettings.FindAsync(1);
    var timeoutMinutes = settings?.IdleTimeoutMinutes ?? 30;
    var now = DateTime.UtcNow;

    var lastActivityTicks = context.Session.GetString("LastActivityTicks");

    if (long.TryParse(lastActivityTicks, out var ticks))
    {
        var lastActivity = new DateTime(ticks, DateTimeKind.Utc);

        if (now - lastActivity >= TimeSpan.FromMinutes(timeoutMinutes))
        {
            await signInManager.SignOutAsync();
            context.Session.Clear();
            context.Response.Redirect("/Account/Login?expired=true");
            return;
        }
    }

    context.Session.SetString("LastActivityTicks", now.Ticks.ToString());

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

app.Run();