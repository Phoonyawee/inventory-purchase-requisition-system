using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockPilot.Models;

namespace StockPilot.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // ponytail: EnsureCreated keeps the demo portable; use migrations before production deployment.
        await db.Database.EnsureCreatedAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        foreach (var role in new[] { "Employee", "Inventory", "Manager", "Admin" })
            if (!await roles.RoleExistsAsync(role)) EnsureSucceeded(await roles.CreateAsync(new IdentityRole(role)));

        await AddUser(users, "employee@stockpilot.local", "Employee");
        await AddUser(users, "inventory@stockpilot.local", "Inventory");
        await AddUser(users, "manager@stockpilot.local", "Manager");
        await AddUser(users, "admin@stockpilot.local", "Admin");
        if (await db.Items.AnyAsync()) return;

        var employee = await users.FindByEmailAsync("employee@stockpilot.local") ?? throw new InvalidOperationException();
        var items = new[]
        {
            new Item { Sku = "MAT-1001", Name = "Industrial safety gloves", Category = "Safety", Unit = "pair", StockQuantity = 120, ReorderLevel = 50, UnitCost = 85m },
            new Item { Sku = "PKG-2001", Name = "Shipping label roll", Category = "Packaging", Unit = "roll", StockQuantity = 18, ReorderLevel = 30, UnitCost = 295m },
            new Item { Sku = "IT-3001", Name = "Cat6 Ethernet cable", Category = "IT supplies", Unit = "pcs", StockQuantity = 8, ReorderLevel = 10, UnitCost = 210m }
        };
        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        db.PurchaseRequests.Add(new PurchaseRequest { ItemId = items[1].Id, Quantity = 40, Department = "Warehouse", Reason = "Replenish labels before monthly dispatch cycle.", RequestedById = employee.Id, RequestedByName = "Demo Employee" });
        db.StockMovements.Add(new StockMovement { ItemId = items[0].Id, Type = StockMovementType.Receipt, Quantity = 120, Note = "Opening balance", CreatedBy = "System" });
        db.AuditLogs.Add(new AuditLog { Actor = "System", Action = "Seeded demo data", Record = "StockPilot" });
        await db.SaveChangesAsync();
    }

    private static async Task AddUser(UserManager<IdentityUser> users, string email, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            EnsureSucceeded(await users.CreateAsync(user, "Demo123!"));
        }
        if (!await users.IsInRoleAsync(user, role)) EnsureSucceeded(await users.AddToRoleAsync(user, role));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
