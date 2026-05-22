using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Application.Services;
using BoutiqueInventory.Infrastructure.BackgroundJobs;
using BoutiqueInventory.Infrastructure.Data;
using BoutiqueInventory.Infrastructure.Data.Dapper;
using BoutiqueInventory.Infrastructure.Imaging;
using BoutiqueInventory.Infrastructure.Notifications;
using BoutiqueInventory.Infrastructure.Repositories;
using BoutiqueInventory.Infrastructure.Search;
using BoutiqueInventory.Infrastructure.Storage;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BoutiqueInventory.Infrastructure;

/// <summary>
/// Wires up the Infrastructure layer (DbContext, repositories,
/// unit-of-work, background jobs) into the DI container.
/// </summary>
public static class DependencyInjection
{
    private static int _dapperConfigured;

    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing from configuration.");

        services.AddScoped<ProductFtsSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, opt) =>
        {
            opt.UseSqlite(connectionString);
            opt.AddInterceptors(sp.GetRequiredService<ProductFtsSaveChangesInterceptor>());
        });
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IProductSearchIndex, ProductSearchIndex>();
        services.Configure<EmailNotificationOptions>(configuration.GetSection("Notifications:Email"));
        services.Configure<WebhookNotificationOptions>(configuration.GetSection("Notifications:Webhook"));
        services.AddHttpClient(nameof(WebhookExpirationNotifier));
        services.AddScoped<EmailExpirationNotifier>();
        services.AddScoped<WebhookExpirationNotifier>();

        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        services.AddSingleton<IImageHashService, ImageHashService>();
        services.AddSingleton<IProductImageStorage, WebRootProductImageStorage>();
        services.AddScoped<IProductImageService, ProductImageService>();

        services.AddHostedService<ExpirationCheckerJob>();

        ConfigureDapper();

        return services;
    }

    private static void ConfigureDapper()
    {
        if (Interlocked.CompareExchange(ref _dapperConfigured, 1, 0) != 0)
        {
            return;
        }

        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));

        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        SqlMapper.AddTypeHandler(new SqliteNullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new SqliteDateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new SqliteNullableDateTimeOffsetTypeHandler());
    }
}
