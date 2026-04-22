using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Domain.Enums;

namespace ServiceCenter.Infrastructure.Seeds;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Engineer", "ServiceManager", "Client"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedAdminUser(userManager);
        await SeedWorkTypes(serviceProvider);
    }

    private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@servicecenter.com";
        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedWorkTypes(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<Data.ApplicationDbContext>();
        if (await db.WorkTypes.AnyAsync()) return;

        db.WorkTypes.AddRange(
            new WorkType { Name = "Diagnostics", DefaultPrice = 20, Description = "Initial device diagnostics" },
            new WorkType { Name = "Part Replacement", DefaultPrice = 0, Description = "Replace a hardware component" },
            new WorkType { Name = "Soldering", DefaultPrice = 15, Description = "PCB soldering work" },
            new WorkType { Name = "Reprogramming", DefaultPrice = 30, Description = "Firmware / software reset or flash" },
            new WorkType { Name = "Cleaning", DefaultPrice = 10, Description = "Internal cleaning" },
            new WorkType { Name = "Screen Repair", DefaultPrice = 50, Description = "Display replacement or repair" }
        );

        await db.SaveChangesAsync();
    }
}
