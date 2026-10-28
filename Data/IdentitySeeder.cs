using Microsoft.AspNetCore.Identity;
using projekt_cyber.Models;

namespace projekt_cyber.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Administrator", "Redaktor", "Czytelnik" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            var admin = await userManager.FindByNameAsync("ADMIN");

            if (admin == null)
            {
                var adminPassword = configuration["AdminInitialPassword"];
                if (string.IsNullOrWhiteSpace(adminPassword)) throw new InvalidOperationException("Setup AdminInitialPassword before running the application.");
                //dotnet user-secrets set "AdminInitialPassword" "Admin1234!Start"

                admin = new ApplicationUser
                {
                    UserName = "ADMIN",
                    Email = "admin@example.local",
                    EmailConfirmed = true,
                    MustChangePassword = false
                };

                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded) throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(admin, "Administrator"))
            {
                var roleResult = await userManager.AddToRoleAsync(admin, "Administrator");
                if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
