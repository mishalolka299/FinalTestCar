using FluentValidation;
using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Validation;

public class CreateCarValidator : AbstractValidator<CreateCarRequest>
{
    public CreateCarValidator(AppDbContext db)
    {
        RuleFor(x => x.VIN)
            .NotEmpty()
            .Length(17).WithMessage("VIN must be exactly 17 characters.")
            .MustAsync(async (vin, ct) => !await db.Cars.AnyAsync(c => c.VIN == vin, ct))
            .WithMessage("VIN must be unique.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"Year must be between 1900 and {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).WithMessage("Mileage must be non-negative.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);

        RuleFor(x => x.FuelType)
            .Must(ft => Enum.TryParse<FinalTest.Api.Domain.FuelType>(ft, out _))
            .WithMessage("FuelType must be Petrol, Diesel, Electric, or Hybrid.");
    }
}
