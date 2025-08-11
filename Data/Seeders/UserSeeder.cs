using ShipmentFinishGood.Domain;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Utilities;

namespace ShipmentFinishGood.Data.Seeders;

public static class UserSeeder
{
    private static readonly (string Username,string Password,string Name,UserRole Role)[] DefaultUsers = new[]
    {
        ("nadia","Admin@123","Nadia",UserRole.Admin),
        ("ryan","Scanner@123","Ryan",UserRole.Scanner),
        ("bejo","Inputter@123","Bejo",UserRole.Inputer),
        ("ghaly","Management@123","Ghaly",UserRole.Manajemen)
    };

    public static async Task SeedAsync(AppDbContext ctx)
    {
        if (ctx.Users.Any()) return;
        foreach (var u in DefaultUsers)
            ctx.Users.Add(new User { Username=u.Username, Name=u.Name, Role=u.Role, Password=Utilities.PasswordHasher.Hash(u.Password) });
        await ctx.SaveChangesAsync();
    }
}
