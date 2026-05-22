using AutoMapper;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Mappings;

/// <summary>
/// Central AutoMapper profile mapping <see cref="Domain.Entities"/>
/// onto outbound response DTOs. Inbound DTOs are turned into entities
/// by the services themselves (so we keep validation/business logic in
/// one place).
/// </summary>
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Warehouse, WarehouseSummaryResponse>()
            .ForMember(d => d.SectionCount, o => o.MapFrom(s => s.Sections.Count))
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Sections.Sum(sec => sec.Products.Count)));

        CreateMap<Warehouse, WarehouseDetailResponse>()
            .IncludeBase<Warehouse, WarehouseSummaryResponse>()
            .ForMember(d => d.Sections, o => o.MapFrom(s => s.Sections));

        CreateMap<WarehouseSection, WarehouseSectionResponse>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));

        CreateMap<Category, CategoryResponse>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));

        CreateMap<Product, ProductResponse>()
            .ForMember(d => d.Categories, o => o.MapFrom(s => s.Categories.Select(pc => pc.Category)))
            .ForMember(d => d.Location, o => o.MapFrom(s => new ProductLocationResponse
            {
                WarehouseId = s.WarehouseSection.WarehouseId,
                WarehouseName = s.WarehouseSection.Warehouse != null ? s.WarehouseSection.Warehouse.Name : string.Empty,
                WarehouseLocation = s.WarehouseSection.Warehouse != null ? s.WarehouseSection.Warehouse.Location : null,
                SectionId = s.WarehouseSectionId,
                SectionName = s.WarehouseSection.Name
            }));

        CreateMap<Product, ProductSummaryResponse>()
            .ForMember(d => d.WarehouseId, o => o.MapFrom(s => s.WarehouseSection.WarehouseId))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s =>
                s.WarehouseSection.Warehouse != null ? s.WarehouseSection.Warehouse.Name : string.Empty))
            .ForMember(d => d.SectionId, o => o.MapFrom(s => s.WarehouseSectionId))
            .ForMember(d => d.SectionName, o => o.MapFrom(s => s.WarehouseSection.Name))
            .ForMember(d => d.CategoryNames, o => o.MapFrom(s =>
                s.Categories.Count > 0
                    ? string.Join(", ", s.Categories
                        .Where(pc => pc.Category != null)
                        .Select(pc => pc.Category.Name))
                    : null));

        CreateMap<Product, ProductImageMatchResponse>()
            .IncludeBase<Product, ProductSummaryResponse>()
            .ForMember(d => d.HammingDistance, o => o.Ignore());

        CreateMap<ExpirationAlert, ExpirationAlertResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product != null ? s.Product.Sku : string.Empty))
            .ForMember(d => d.ExpirationDate, o => o.MapFrom(s => s.Product != null ? s.Product.ExpirationDate : null))
            .ForMember(d => d.AcknowledgedAt, o => o.MapFrom(s => s.AcknowledgedAt));
    }
}
