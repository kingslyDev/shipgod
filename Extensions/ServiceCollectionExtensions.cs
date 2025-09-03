using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Services;

namespace ShipmentFinishGood.Extensions;

public static class ServiceCollectionExtensions
{
    private const string CookieScheme = "app_cookie";

    public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddSignalR();
        return services;
    }

    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IModelConfigurationRepository, ModelConfigurationRepository>();
        
        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IExcelProcessingService, ExcelProcessingService>();
        services.AddScoped<IModelConfigurationService, ModelConfigurationService>();
        services.AddScoped<IFinalProcessingService, FinalProcessingService>();
        services.AddScoped<IQRManagementService, QRManagementService>();
        services.AddScoped<IScanningService, ScanningService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPdfGenerationService, PdfGenerationService>();
        
        // 🧠 SMART AUTO-CALCULATION ENGINE SERVICES
        services.AddScoped<ISmartCalculationEngine, SmartCalculationEngine>();
        services.AddScoped<ISmartBarcodeManager, SmartBarcodeManager>();
        services.AddScoped<IScanningProtectionService, ScanningProtectionService>();
        
        // 🔒 HIERARCHICAL LOCK VALIDATION SERVICES - Phase 2
        services.AddScoped<IPOValidationService, POValidationService>();
        
        return services;
    }    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"] ?? "dev-secret-key-change"; // TODO secure
        services.AddAuthentication(CookieScheme)
            .AddCookie(CookieScheme, opt =>
            {
                opt.LoginPath = "/Auth/Login";
                opt.AccessDeniedPath = "/Auth/Denied";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", p => p.RequireRole("admin"));
            options.AddPolicy("RequireScanner", p => p.RequireRole("scanner","admin","manajemen"));
            options.AddPolicy("RequireInputer", p => p.RequireRole("inputer","admin","manajemen"));
            options.AddPolicy("RequireManajemen", p => p.RequireRole("manajemen","admin"));
        });
        return services;
    }
}
