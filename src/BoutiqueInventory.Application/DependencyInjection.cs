using System.Reflection;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Application.Mappings;
using BoutiqueInventory.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BoutiqueInventory.Application;

/// <summary>
/// Wires up the Application layer (services, AutoMapper profile,
/// FluentValidation validators) into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IExpirationMonitor, ExpirationMonitorService>();

        return services;
    }
}
