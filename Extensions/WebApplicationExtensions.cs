using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Data.Seeders;
using ShipmentFinishGood.Hubs;

namespace ShipmentFinishGood.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseAppRequestPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            
        app.MapHub<ProgressHub>("/progressHub");
        
        return app;
    }

    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DbInitializer.InitializeAsync(ctx).GetAwaiter().GetResult();
        return app;
    }
}
