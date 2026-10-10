using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Data
{
    public static class IdentitySeeder
    {
        private const string DevelopmentPassword = "Library123!";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            RoleManager<IdentityRole> roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            UserManager<IdentityUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames =
            {
                RoleNames.Admin,
                RoleNames.Reception,
                RoleNames.Manager
            };

            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    IdentityResult roleResult =
                        await roleManager.CreateAsync(new IdentityRole(roleName));

                    if (!roleResult.Succeeded)
                    {
                        string errors = string.Join(
                            ", ",
                            roleResult.Errors.Select(error => error.Description)
                        );

                        throw new InvalidOperationException(
                            $"Could not create the {roleName} role: {errors}"
                        );
                    }
                }
            }

            await CreateUserAsync(
                userManager,
                "admin@librarydemo.com",
                RoleNames.Admin
            );

            await CreateUserAsync(
                userManager,
                "reception@librarydemo.com",
                RoleNames.Reception
            );

            await CreateUserAsync(
                userManager,
                "manager@librarydemo.com",
                RoleNames.Manager
            );
        }

        private static async Task CreateUserAsync(
            UserManager<IdentityUser> userManager,
            string email,
            string roleName
        )
        {
            IdentityUser? user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                IdentityResult userResult =
                    await userManager.CreateAsync(user, DevelopmentPassword);

                if (!userResult.Succeeded)
                {
                    string errors = string.Join(
                        ", ",
                        userResult.Errors.Select(error => error.Description)
                    );

                    throw new InvalidOperationException(
                        $"Could not create the user {email}: {errors}"
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                IdentityResult roleResult =
                    await userManager.AddToRoleAsync(user, roleName);

                if (!roleResult.Succeeded)
                {
                    string errors = string.Join(
                        ", ",
                        roleResult.Errors.Select(error => error.Description)
                    );

                    throw new InvalidOperationException(
                        $"Could not add {email} to the {roleName} role: {errors}"
                    );
                }
            }
        }
    }
}