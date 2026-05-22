using BoutiqueInventory.Application.DTOs.Requests;
using FluentValidation;

namespace BoutiqueInventory.Application.Validators;

public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Location).MaximumLength(250);
    }
}

public sealed class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Location).MaximumLength(250);
    }
}

public sealed class WarehouseSectionRequestValidator : AbstractValidator<WarehouseSectionRequest>
{
    public WarehouseSectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}
