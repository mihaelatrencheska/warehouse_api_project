using BoutiqueInventory.Application.DTOs.Requests;
using FluentValidation;

namespace BoutiqueInventory.Application.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Size).MaximumLength(40);
        RuleFor(x => x.Type).MaximumLength(80);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.WarehouseSectionId).NotEmpty();
        RuleForEach(x => x.CategoryIds).NotEmpty();
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Size).MaximumLength(40);
        RuleFor(x => x.Type).MaximumLength(80);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.WarehouseSectionId).NotEmpty();
        RuleForEach(x => x.CategoryIds).NotEmpty();
    }
}

public sealed class MoveProductRequestValidator : AbstractValidator<MoveProductRequest>
{
    public MoveProductRequestValidator()
    {
        RuleFor(x => x.WarehouseSectionId).NotEmpty();
    }
}

public sealed class UpdateProductCategoriesRequestValidator : AbstractValidator<UpdateProductCategoriesRequest>
{
    public UpdateProductCategoriesRequestValidator()
    {
        RuleForEach(x => x.CategoryIds).NotEmpty();
    }
}

public sealed class ProductSearchRequestValidator : AbstractValidator<ProductSearchRequest>
{
    public ProductSearchRequestValidator()
    {
        When(x => x.Page.HasValue, () =>
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        });

        When(x => x.PageSize.HasValue, () =>
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        });

        When(x => x.ExpiringWithinDays.HasValue, () =>
        {
            RuleFor(x => x.ExpiringWithinDays).GreaterThan(0);
        });
    }
}
